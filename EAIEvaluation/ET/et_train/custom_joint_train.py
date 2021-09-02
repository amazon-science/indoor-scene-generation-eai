import os

os.environ['ET_ROOT'] =  '/home/ubuntu/research2/ET'
os.environ['ET_DATA'] = '/home/ubuntu/research2/ET/data/'
os.environ['ET_LOGS'] = '/home/ubuntu/research2/ET/logs/'

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

dataset = CustomAlfredDataset("data/lmdb_i/")
dataset.name = "lmdb_human"

additionl_dataset = CustomAlfredDataset("data/lmdb_new_i/")
additionl_dataset.name = "lmdb_human"

loader_args = {
        'num_workers': 5,
        'drop_last': (torch.cuda.device_count() > 1),
        'collate_fn': helper_util.identity}

loader = torch.utils.data.DataLoader(dataset, batch_size = 8, **loader_args)
additionl_dataset = torch.utils.data.DataLoader(additionl_dataset, batch_size = 8, **loader_args)

args = helper_util.AttrDict(
{'seed': 1, 'resume': True, 'profile': False, 'batch': 8, 'epochs': 20, 'optimizer': 'adamw', 'weight_decay': 0.33, 'lr': {'init': 0.0001, 'profile': 'linear', 'decay_epoch': 10, 'decay_scale': 0.1, 'final': 1e-05, 'cycle_epoch_up': 0, 'cycle_epoch_down': 0, 'warmup_epoch': 0, 'warmup_scale': 1}, 'action_loss_wt': 1.0, 'object_loss_wt': 1.0, 'subgoal_aux_loss_wt': 0, 'progress_aux_loss_wt': 0, 'entropy_wt': 0.0, 'demb': 768, 'encoder_heads': 12, 'encoder_layers': 2, 'num_input_actions': 1, 'encoder_lang': {'shared': True, 'layers': 2, 'pos_enc': True, 'instr_enc': False}, 'decoder_lang': {'layers': 2, 'heads': 12, 'demb': 768, 'dropout': 0.1, 'pos_enc': True}, 'detach_lang_emb': False, 'dropout': {'lang': 0.0, 'vis': 0.3, 'emb': 0.0, 'transformer': {'encoder': 0.1, 'action': 0.0}}, 'enc': {'pos': True, 'pos_learn': False, 'token': False, 'dataset': False}, 'name': 'pretrained', 'model': 'transformer', 'device': 'cuda', 'num_workers': 12, 'pretrained_path': None, 'fast_epoch': False, 'data': {'train': ['lmdb_human'], 'valid': [], 'length': 30000, 'ann_type': ['lang']}, 'dout': '/home/ubuntu/research2/ET/logs/pretrained'}
)

# assign vocabs to datasets and check their sizes for nn.Embeding inits
embs_ann, vocab_out = process_vocabs([dataset], args)
# create the model
model, optimizer, prev_train_info = create_model(args, embs_ann, vocab_out)
# optimizer
optimizer, schedulers = model_util.create_optimizer_and_schedulers(
prev_train_info['progress'], model.args, model.parameters(), optimizer)

# fix vocab
# dataset.vocab_in.name = list(model.model.embs_ann.keys())[0]

print("Training start")
print(model.args.dout)
# save the checkpoint
print('Saving models...')
model_util.save_model(
    model, 'model_j_{:02d}.pth'.format(99), {}, optimizer=optimizer)
#model_util.save_model(self, 'latest.pth', stats, symlink=True)

# train on additional data
for ep in range(101):
    model.train()
    losses_train_list = []
    for batches in tqdm(loader):
        traj_data, input_dict, gt_dict = tensorize_and_pad(
                batches, model.args.device, model.pad)

        model_out = model.model.forward(
            dataset.vocab_in,
            action=gt_dict['action'], **input_dict)

        losses_train = model.model.compute_batch_loss(model_out, gt_dict)

        # do the gradient step
        optimizer.zero_grad()
        sum_loss = sum([v for v in losses_train.values()])
        sum_loss.backward()
        
        losses_train_list.append(sum_loss.item())
        
        optimizer.step()
    
    additional_losses_train_list = []
    for batches in tqdm(additionl_dataset):
        traj_data, input_dict, gt_dict = tensorize_and_pad(
                batches, model.args.device, model.pad)

        # clip token
        input_dict['lang'] = torch.clip(input_dict['lang'], max=len(dataset.vocab_in) - 1)

        model_out = model.model.forward(
            dataset.vocab_in,
            action=gt_dict['action'], **input_dict)

        losses_train = model.model.compute_batch_loss(model_out, gt_dict)

        # do the gradient step
        optimizer.zero_grad()
        sum_loss = sum([v for v in losses_train.values()])
        sum_loss.backward()
        
        additional_losses_train_list.append(sum_loss.item())
        
        optimizer.step()

    print("epoch: {} train loss {:.3f}".format(ep, np.mean(losses_train_list)))
    print("epoch: {} additional train loss {:.3f}".format(ep, np.mean(additional_losses_train_list)))

    if ep % 10 == 0:
        if not os.path.exists(model.args.dout):
            os.mkdir(model.args.dout)

        print(model.args.dout)
        # save the checkpoint
        print('Saving models...')
        model_util.save_model(
            model, 'model_j_{:02d}.pth'.format(ep), {}, optimizer=optimizer)
        #model_util.save_model(self, 'latest.pth', stats, symlink=True)

