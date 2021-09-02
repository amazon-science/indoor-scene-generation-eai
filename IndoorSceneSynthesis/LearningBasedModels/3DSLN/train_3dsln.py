from options.options import Options
import os
import torch
from build_dataset_model import build_loaders, build_model
from utils import get_model_attr, calculate_model_losses, tensor_aug
from collections import defaultdict
import math

from torch.utils.tensorboard import SummaryWriter
from tqdm.auto import tqdm
import numpy as np

from new.CustomVAE import *
from new.utils import resolve_relative_positions
from utils import calculate_model_losses
from data.suncg_dataset import g_add_in_room_relation, g_use_heuristic_relation_matrix, \
    g_prepend_room, g_add_random_parent_link, g_shuffle_subject_object

# decoder option
g_decoder_option = "original" #"rgcn"
g_relative_location = "False" # FIX FLASE CURRENTLY
g_parent_link_index = 16

args = Options().parse()
if (args.output_dir is not None) and (not os.path.isdir(args.output_dir)):
    os.mkdir(args.output_dir)
if (args.test_dir is not None) and (not os.path.isdir(args.test_dir)):
    os.mkdir(args.test_dir)

# train path
# train path
args.suncg_train_dir = '/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/all_room_info_a_filtered.json'
args.valid_types_dir = '/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/valid_types.json'
args.suncg_val_dir = '/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/all_room_info_b_filtered.json'


# has KL divergence loss
args.KL_loss_weight = 0.001
args.use_AE = False

# tensorboard
writer = SummaryWriter()
writer.add_text("args", str(args))

writer.add_hparams({
    "experiment type": "Original VAE for 3d-front: Original Encoder + Original Decoder",
    "decoder type": g_decoder_option,
    "use relative location": g_relative_location,
    "Add 'in_room' relation": g_add_in_room_relation,
    "Use heuristic relation matrix": g_use_heuristic_relation_matrix,
    "prepend/append room info": g_prepend_room,
    "add random parent link": g_add_random_parent_link,
    "shuffle object/subject when loading data": g_shuffle_subject_object,
}, {"NA": 0})

# load data
vocab, train_loader, val_loader = build_loaders(args)

# load model
model, model_kwargs = build_model(args, vocab)
print(model)
model.float().cuda()
optimizer = torch.optim.Adam(model.parameters(), lr=args.learning_rate)

t = 0 # total steps
total_epochs = 5

for epoch in range(total_epochs):
    print("Training epoch {}".format(epoch))
    
    # training
    model.train()
    for batch in tqdm(train_loader):
        t += 1
        ids, objs, boxes, triples, angles, attributes, obj_to_img, triple_to_img = tensor_aug(batch)

        model_out = model(objs, triples, boxes, angles, attributes, obj_to_img)
        mu, logvar, boxes_pred, angles_pred = model_out
  
        if args.KL_linear_decay:
            KL_weight = 10 ** (t // 1e5 - 6)
        else:
            KL_weight = args.KL_loss_weight
        total_loss, losses = calculate_model_losses(args, None, boxes, boxes_pred, angles, angles_pred, mu=mu, logvar=logvar, KL_weight=KL_weight)
        losses['total_loss'] = total_loss.item()
        
        if not math.isfinite(losses['total_loss']):
            print('WARNING: Got loss = NaN, not backpropping')
            continue
        
        optimizer.zero_grad()
        total_loss.backward()
        optimizer.step()

        if t % args.print_every == 0:
            print("On batch {} in epoch {}".format(t, epoch))
            for name, val in losses.items():
                print(' [%s]: %.4f' % (name, val))
                writer.add_scalar('Loss/'+ name, val, t)

    # validation
    model.eval()
    print("Validation epoch {}".format(epoch))
    valid_loss_list = {"bbox_pred":[], "angle_pred": [],"total_loss":[], 'KLD_Gauss':[]}
    for batch in tqdm(val_loader):       
        ids, objs, boxes, triples, angles, attributes, obj_to_img, triple_to_img = tensor_aug(batch)

        model_out = model(objs, triples, boxes, angles, attributes, obj_to_img)
        mu, logvar, boxes_pred, angles_pred = model_out
  
        if args.KL_linear_decay:
            KL_weight = 10 ** (t // 1e5 - 6)
        else:
            KL_weight = args.KL_loss_weight
        total_loss, losses = calculate_model_losses(args, None, boxes, boxes_pred, angles, angles_pred, mu=mu, logvar=logvar, KL_weight=KL_weight)
        losses['total_loss'] = total_loss.item()
       
        
        for name, val in losses.items():
            valid_loss_list[name].append(val)

        #valid_loss_list['total_loss'].append(total_loss.item())
        
    for name, val_list in valid_loss_list.items():
        writer.add_scalar('Loss/Validation_'+ name, np.mean(val_list), epoch)
        print("Validation loss", name, np.mean(val_list))
  