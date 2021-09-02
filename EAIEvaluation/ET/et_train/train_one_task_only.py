from logging import debug
import os

os.environ['ET_ROOT'] =  '/home/ubuntu/research3/ET'
os.environ['ET_DATA'] = '/home/ubuntu/research3/ET/data/'
os.environ['ET_LOGS'] = '/home/ubuntu/research3/ET/logs/'

import json
import pickle
import torch
import numpy as np
import torch
from torch.functional import split
from torch.utils.data.dataset import Dataset

from alfred.utils import data_util, model_util, helper_util
from tqdm import tqdm

from alfred.gen import constants
from alfred.utils.data_util import load_vocab

import random

from torch.utils.tensorboard import SummaryWriter
from alfred.model.train import prepare, create_model, load_data, wrap_datasets, process_vocabs


class OneTaskAlfredDataset(Dataset):
    def __init__(self, root_folder:str, args, split="train", task_name = "look_at_obj_in_light") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False
        self.max_subgoals = constants.MAX_SUBGOALS
        self.task_name = task_name

        # load the vocabulary for object classes
        self.vocab_obj = torch.load(os.path.join(
            constants.ET_ROOT, constants.OBJ_CLS_VOCAB))
        # load vocabularies for input language and output actions
        vocab = data_util.load_vocab("lmdb_human")
        self.vocab_in = vocab['word']
        out_type = 'action_low' 
        self.vocab_out = vocab[out_type]

        self.json_folder = os.path.join(self.root_folder, "jsons")
        self.feat_folder = os.path.join(self.root_folder, "feats")
        self.mask_folder = os.path.join(self.root_folder, "masks")
        
        self.json_trajs = []
        self.mask_paths = []
        self.feat_paths = []

        self.load_json_mask_feat_paths()
    
    def __len__(self):
        return len(self.json_trajs)
    
    def load_json_mask_feat_paths(self):
        for json_file in tqdm(os.listdir(self.json_folder)):
            if json_file.startswith(self.split):
                
                if self.task_name not in json_file:
                    continue

                json_array = pickle.load(open(os.path.join(self.json_folder,json_file), "rb"))
                for json_item in json_array:
                    #self.json_paths.append(os.path.join(self.json_folder,json_file))
                    self.json_trajs.append(json_item)
                    
                    feat_file = json_file.split(".")[0] + ".pt"
                    self.feat_paths.append(os.path.join(self.feat_folder,feat_file))
                    
                    mask_file = json_file.split(".")[0] + ".pkl"
                    self.mask_paths.append(os.path.join(self.mask_folder,mask_file))

            #train:look_at_obj_in_light-AlarmClock-None-DeskLamp-301:trial_T20190s

    def __getitem__(self, index):
        task_json = self.json_trajs[index]

        # dataset name
        task_json["dataset_name"] = self.name

        traj_feat = torch.load(self.feat_paths[index])
        #traj_mask = torch.load(self.mask_paths[index])

        feat_dict = self.load_features(task_json)
        feat_dict["frames"] = traj_feat

        return task_json, feat_dict

    def load_features(self, task_json):
        '''s
        load features from task_json
        '''
        feat = dict()
        # language inputs
        feat['lang'] = self.load_lang(task_json)

        # action outputs
        if not self.test_mode:
            # low-level action
            feat['action'] = self.load_action(
                task_json, self.vocab_out)
            # low-level valid interact
            feat['action_valid_interact'] = [
                a['valid_interact'] for a in sum(
                    task_json['num']['action_low'], [])]
            feat['object'] = self.load_object_classes(
                task_json, self.vocab_obj)
            assert len(feat['object']) == sum(feat['action_valid_interact'])

        # auxillary outputs
        if not self.test_mode:
            # subgoal completion supervision
            if self.args.subgoal_aux_loss_wt > 0:
                feat['subgoals_completed'] = np.array(
                    task_json['num']['low_to_high_idx']) / self.max_subgoals
            # progress monitor supervision
            if self.args.progress_aux_loss_wt > 0:
                num_actions = len(task_json['num']['low_to_high_idx'])
                goal_progress = [(i + 1) / float(num_actions)
                                 for i in range(num_actions)]
                feat['goal_progress'] = goal_progress
        return feat
        
    def load_lang(self, task_json):
        '''
        load numericalized language from task_json
        '''
        lang_num_goal = task_json['num']['lang_goal']
        lang_num_instr = sum(task_json['num']['lang_instr'], [])
        lang_num = lang_num_goal + lang_num_instr
        return lang_num

    def load_action(self, task_json, vocab_orig, vocab_translate=None, action_type='action_low'):
        '''
        load action as a list of tokens from task_json
        '''
        if action_type == 'action_low':
            # load low actions
            lang_action = [
                [a['action'] for a in a_list]
                for a_list in task_json['num']['action_low']]
        elif action_type == 'action_high':
            # load high actions
            lang_action = [
                [a['action']] + a['action_high_args']
                for a in task_json['num']['action_high']]
        else:
            raise NotImplementedError('Unknown action_type {}'.format(action_type))
        lang_action = sum(lang_action, [])
        # translate actions to the provided vocab if needed
        if vocab_translate and not vocab_orig.contains_same_content(
                vocab_translate):
            lang_action = model_util.translate_to_vocab(
                lang_action, vocab_orig, vocab_translate)
        return lang_action

    def load_object_classes(self, task_json, vocab=None):
        '''
        load object classes for interactive actions
        '''
        object_classes = []
        for action in task_json['plan']['low_actions']:
            if model_util.has_interaction(action['api_action']['action']):
                obj_key = ('receptacleObjectId'
                           if 'receptacleObjectId' in action['api_action']
                           else 'objectId')
                object_class = action['api_action'][obj_key].split('|')[0]
                object_classes.append(
                    object_class if vocab is None
                    else vocab.word2index(object_class))
        return object_classes


if __name__ == "__main__":
    args = helper_util.AttrDict(
    {'seed': 2, 'resume': True, 'profile': False, 'batch': 8, 'epochs': 20, 
    'optimizer': 'adamw', 'weight_decay': 0.33, 'lr': {'init': 0.0001, 
    'profile': 'linear', 'decay_epoch': 10, 'decay_scale': 0.1, 
    'final': 1e-05, 'cycle_epoch_up': 0, 'cycle_epoch_down': 0, 'warmup_epoch': 0, 
    'warmup_scale': 1}, 'action_loss_wt': 1.0, 'object_loss_wt': 1.0, 
    'subgoal_aux_loss_wt': 0.1, 'progress_aux_loss_wt': 0.1, 
    'entropy_wt': 0.0, 'demb': 768, 'encoder_heads': 12, 
    'encoder_layers': 2, 'num_input_actions': 1, 
    'encoder_lang': {'shared': True, 'layers': 2, 'pos_enc': True, 'instr_enc': False}, 
    'decoder_lang': {'layers': 2, 'heads': 12, 'demb': 768, 'dropout': 0.1, 'pos_enc': True}, 
    'detach_lang_emb': False, 'dropout': {'lang': 0.0, 'vis': 0.3, 'emb': 0.0, 
    'transformer': {'encoder': 0.1, 'action': 0.0}}, 
    'enc': {'pos': True, 'pos_learn': False, 'token': False, 'dataset': False}, 
    'name': 'pretrained', 'model': 'transformer', 'device': 'cuda', 'num_workers': 12, 
    'pretrained_path': None, 'fast_epoch': False, 'data': {'train': ['lmdb_human'], 
    'valid': [], 'length': 30000, 'ann_type': ['lang']}, 'dout': os.environ['ET_LOGS'] + '/pretrained'}
    )

    # warining: resume has to be true, otherwise all record shall be deleted

    print(args)

    # Writer will output to ./runs/ directory by default
    writer = SummaryWriter()

    batch_size = 8
    dataset = OneTaskAlfredDataset("data/lmdb_i/", args, split="train", task_name="look_at_obj_in_light")
    dataset.name = "lmdb_human"

    loader_args = {
            'num_workers': 5,
            'drop_last': (torch.cuda.device_count() > 1),
            'collate_fn': helper_util.identity}

    weights = [1 / len(dataset)] * len(dataset)
    num_samples = 30000
    sampler = torch.utils.data.WeightedRandomSampler(
        weights, num_samples=num_samples, replacement=True)

    loader = torch.utils.data.DataLoader(dataset, batch_size = batch_size, sampler=sampler, **loader_args)

    # assign vocabs to datasets and check their sizes for nn.Embeding inits
    embs_ann, vocab_out = process_vocabs([dataset], args)
    print(embs_ann)
    vocabs = [dataset.vocab_in]
    # create the model
    model, optimizer, prev_train_info = create_model(args, embs_ann, vocab_out)
    # optimizer
    optimizer, schedulers = model_util.create_optimizer_and_schedulers(0, model.args, model.parameters(), optimizer)

    # load validation dataset
    valid_dataset = OneTaskAlfredDataset("data/lmdb_i/", args, split="valid", task_name="look_at_obj_in_light")
    valid_dataset.name = "lmdb_human"
    valid_loader = torch.utils.data.DataLoader(valid_dataset, batch_size = batch_size,  **loader_args)
    valid_best_loss = 1e6

    print("Training start")
    print(model.args.dout)
    # save the checkpoint
    print('Saving models...')
    model_util.save_model(
        model, 'one_task_810_{}.pth'.format("save_test"), {}, optimizer=optimizer)
    #model_util.save_model(self, 'latest.pth', stats, symlink=True)


    # train on additional data
    total_step = 0
    for ep in range(args.epochs + 1):
        model.train()
        batch_mix_train_loss = []
        for batches in tqdm(zip(loader), total=num_samples // batch_size):
            losses_train_batches = 0
            total_step += 1
            for c, batch in enumerate(batches):
                traj_data, input_dict, gt_dict = data_util.tensorize_and_pad(
                        batch, model.args.device, model.pad)
                
                model_out = model.model.forward(
                    vocab=vocabs[c],
                    action=gt_dict['action'], **input_dict)

                losses_train = model.model.compute_batch_loss(model_out, gt_dict)
                losses_train_batches += sum([v for v in losses_train.values()])
            
            # do the gradient step
            optimizer.zero_grad()
            losses_train_batches.backward()
            optimizer.step()
            
            batch_mix_train_loss.append(losses_train_batches.item())

            if total_step % 200 == 0:
                writer.add_scalar("train loss", np.mean(batch_mix_train_loss), total_step)

        model_util.adjust_lr(optimizer, model.args, ep, schedulers)

        model.eval()
        losses_valid_list = []
        for batch in tqdm(valid_loader):
            traj_data, input_dict, gt_dict = data_util.tensorize_and_pad(
                        batch, model.args.device, model.pad)
                
            # clip token
            # input_dict['lang'] = torch.clip(input_dict['lang'], max=len(dataset.vocab_in) - 1)

            model_out = model.model.forward(
                dataset.vocab_in,
                action=gt_dict['action'], **input_dict)

            losses_valid = model.model.compute_batch_loss(model_out, gt_dict)
            losses_valid_batch = sum([v for v in losses_valid.values()])

            losses_valid_list.append(losses_valid_batch.item())

        # record validation loss
        valid_loss_mean = np.mean(losses_valid_list)
        writer.add_scalar("valid loss", valid_loss_mean, ep)

        if valid_loss_mean < valid_best_loss:
            valid_best_loss = valid_loss_mean

            if not os.path.exists(model.args.dout):
                os.mkdir(model.args.dout)

            # save the checkpoint
            print('Saving models...')
            model_util.save_model(model, 'one_task_810_{}.pth'.format("best"), {}, optimizer=optimizer)
            #model_util.save_model(self, 'latest.pth', stats, symlink=True)

        if ep % 10 == 0:
            if not os.path.exists(model.args.dout):
                os.mkdir(model.args.dout)

            print(model.args.dout)
            # save the checkpoint
            print('Saving models...')
            model_util.save_model(
                model, 'one_task_810_{:02d}.pth'.format(ep), {}, optimizer=optimizer)
            #model_util.save_model(self, 'latest.pth', stats, symlink=True)

    writer.close()