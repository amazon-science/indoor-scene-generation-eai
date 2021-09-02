import os

os.environ['ET_ROOT'] =  '/home/ubuntu/research3/ET'
os.environ['ET_DATA'] = '/home/ubuntu/research3/ET/data/'
os.environ['ET_LOGS'] = '/home/ubuntu/research3/ET/logs/'

import gtimer as gt
import collections

import torch
import random
import shutil
import pprint
import numpy as np
#from sacred import Experiment

from alfred.config import exp_ingredient, train_ingredient
# from alfred.data import AlfredDataset, SpeakerDataset
from alfred.gen import constants
from alfred.model.learned import LearnedModel
from alfred.utils import data_util, helper_util, model_util

from alfred.model.train import prepare, create_model, load_data, wrap_datasets, process_vocabs

from alfred.utils.data_util import tensorize_and_pad
from alfred.utils import helper_util

from et_train.custom_dataset import *

from torch.utils.tensorboard import SummaryWriter

args = helper_util.AttrDict(
{'seed': 1, 'resume': True, 'profile': False, 'batch': 8, 'epochs': 20, 'optimizer': 'adamw', 'weight_decay': 0.33, 'lr': {'init': 0.0001, 'profile': 'linear', 'decay_epoch': 10, 'decay_scale': 0.1, 'final': 1e-05, 'cycle_epoch_up': 0, 'cycle_epoch_down': 0, 'warmup_epoch': 0, 'warmup_scale': 1}, 'action_loss_wt': 1.0, 'object_loss_wt': 1.0, 'subgoal_aux_loss_wt': 0.1, 'progress_aux_loss_wt': 0.1, 'entropy_wt': 0.0, 'demb': 768, 'encoder_heads': 12, 'encoder_layers': 2, 'num_input_actions': 1, 'encoder_lang': {'shared': True, 'layers': 2, 'pos_enc': True, 'instr_enc': False}, 'decoder_lang': {'layers': 2, 'heads': 12, 'demb': 768, 'dropout': 0.1, 'pos_enc': True}, 'detach_lang_emb': False, 'dropout': {'lang': 0.0, 'vis': 0.3, 'emb': 0.0, 'transformer': {'encoder': 0.1, 'action': 0.0}}, 'enc': {'pos': True, 'pos_learn': False, 'token': False, 'dataset': False}, 'name': 'pretrained', 'model': 'transformer', 'device': 'cuda', 'num_workers': 12, 'pretrained_path': None, 'fast_epoch': False, 'data': {'train': ['lmdb_human'], 'valid': [], 'length': 30000, 'ann_type': ['lang']}, 'dout': os.environ['ET_LOGS'] + '/pretrained'}
)

# warining: resume has to be true, otherwise all record shall be deleted

print(args)

# Writer will output to ./runs/ directory by default
writer = SummaryWriter()

dataset = CustomAlfredDataset("data/lmdb_i/", args)
dataset.name = "lmdb_human"

loader_args = {
        'num_workers': 5,
        'drop_last': (torch.cuda.device_count() > 1),
        'collate_fn': helper_util.identity}

weights = [1 / len(dataset)] * len(dataset)
num_samples = 30000 
batch_size = 8
sampler = torch.utils.data.WeightedRandomSampler(
    weights, num_samples=num_samples, replacement=True)

loader = torch.utils.data.DataLoader(dataset, batch_size = batch_size, sampler=sampler, **loader_args)

#additional_dataset = CustomAlfredDataset("data/lmdb_new_i/", args)
additional_dataset = CustomSynthDataset("data/lmdb_synth_45k_i/", args)
additional_dataset.name = "lmdb_synth_45k_i"

additional_weights = [1 / len(additional_dataset)] * len(additional_dataset)
additional_num_samples = 30000 // 2
additional_sampler = torch.utils.data.WeightedRandomSampler(
    additional_weights, num_samples=additional_num_samples, replacement=True)

additional_loader = torch.utils.data.DataLoader(additional_dataset, batch_size = batch_size, 
    sampler=additional_sampler, **loader_args)

# assign vocabs to datasets and check their sizes for nn.Embeding inits
embs_ann, vocab_out = process_vocabs([dataset], args)
print(embs_ann)
vocabs = [dataset.vocab_in, additional_dataset.vocab_in]
# create the model
model, optimizer, prev_train_info = create_model(args, embs_ann, vocab_out)
# optimizer
optimizer, schedulers = model_util.create_optimizer_and_schedulers(0, model.args, model.parameters(), optimizer)



# load validation dataset
valid_dataset = CustomAlfredDataset("data/lmdb_i/", args, split="valid")
valid_dataset.name = "lmdb_human"
valid_loader = torch.utils.data.DataLoader(valid_dataset, batch_size = batch_size,  **loader_args)
valid_best_loss = 1e6

# args.pretrained_path = "/home/ubuntu/research2/ET/logs/pretrained/speaker_k_best.pth"
# pretrained_model = torch.load(args.pretrained_path, map_location=torch.device(args.device))
# model_dict = model.model.state_dict()

# # load pretrained weights
# loaded_keys = set(model.state_dict().keys()).intersection(set(pretrained_model['model'].keys()))

# print("loaded_keys:", loaded_keys)

# pretrained_dict = {k: v for k, v in pretrained_model['model'].items() if k in loaded_keys}
# model_dict.update(pretrained_dict) 
# model.load_state_dict(model_dict, strict=False)

print("Training start")
print(model.args.dout)
# save the checkpoint
print('Saving models...')
model_util.save_model(
    model, 'final_k_{}.pth'.format("save_test"), {}, optimizer=optimizer)
#model_util.save_model(self, 'latest.pth', stats, symlink=True)

# train on additional data
total_step = 0
for ep in range(21):
    model.train()
    batch_mix_train_loss = []
    for batches in tqdm(zip(loader, additional_loader), total=num_samples // batch_size):
        losses_train_batches = 0
        total_step += 1
        for c, batch in enumerate(batches):
            traj_data, input_dict, gt_dict = tensorize_and_pad(
                    batch, model.args.device, model.pad)
            
            # clip token
            input_dict['lang'] = torch.clip(input_dict['lang'], max=len(dataset.vocab_in) - 1)

            model_out = model.model.forward(
                vocabs[c],
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
        traj_data, input_dict, gt_dict = tensorize_and_pad(
                    batch, model.args.device, model.pad)
            
        # clip token
        input_dict['lang'] = torch.clip(input_dict['lang'], max=len(dataset.vocab_in) - 1)

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
        model_util.save_model(model, 'final_k_{}.pth'.format("best"), {}, optimizer=optimizer)
        #model_util.save_model(self, 'latest.pth', stats, symlink=True)

    if ep % 10 == 0:
        if not os.path.exists(model.args.dout):
            os.mkdir(model.args.dout)

        print(model.args.dout)
        # save the checkpoint
        print('Saving models...')
        model_util.save_model(
            model, 'final_k_{:02d}.pth'.format(ep), {}, optimizer=optimizer)
        #model_util.save_model(self, 'latest.pth', stats, symlink=True)

writer.close()