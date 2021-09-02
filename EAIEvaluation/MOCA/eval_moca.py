#!/usr/bin/env python
# coding: utf-8

# In[ ]:


import matplotlib.pyplot as plt
import json
from tqdm import tqdm


# In[ ]:


import revtok
from moca_train.custom_eval import get_goal_conditions_meet, remove_spaces_and_lower, setup_scene, load_all_trial_paths


# In[ ]:


import os
import sys

os.environ['ALFRED_ROOT'] = os.getcwd()

sys.path.append(os.path.join(os.environ['ALFRED_ROOT']))
sys.path.append(os.path.join(os.environ['ALFRED_ROOT'], 'gen'))
sys.path.append(os.path.join(os.environ['ALFRED_ROOT'], 'models'))

import argparse
import torch.multiprocessing as mp
#from eval_task import EvalTask
#from eval_subgoals import EvalSubgoals


# In[ ]:



# parser
parser = argparse.ArgumentParser()

# settings
parser.add_argument('--splits', type=str, default="data/splits/oct21.json")
parser.add_argument('--data', type=str, default="data/json_feat_2.1.0")
parser.add_argument('--reward_config', default='models/config/rewards.json')
parser.add_argument('--eval_split', type=str, choices=['train', 'valid_seen', 'valid_unseen'])
#parser.add_argument('--model_path', type=str, default="exp/pretrained/pretrained.pth")
parser.add_argument('--model', type=str, default='models.model.seq2seq_im_mask')
parser.add_argument('--preprocess', dest='preprocess', action='store_true')
parser.add_argument('--shuffle', dest='shuffle', action='store_true')
parser.add_argument('--gpu', dest='gpu', action='store_true')
parser.add_argument('--num_threads', type=int, default=1)

# eval params
parser.add_argument('--max_steps', type=int, default=400, help='max steps before episode termination')
parser.add_argument('--max_fails', type=int, default=10, help='max API execution failures before episode termination')

# eval settings
parser.add_argument('--subgoals', type=str, help="subgoals to evaluate independently, eg:all or GotoLocation,PickupObject...", default="")
parser.add_argument('--smooth_nav', dest='smooth_nav', action='store_true', help='smooth nav actions (might be required based on training data)')
parser.add_argument('--skip_model_unroll_with_expert', action='store_true', help='forward model with expert actions')
parser.add_argument('--no_teacher_force_unroll_with_expert', action='store_true', help='no teacher forcing with expert')

# debug
parser.add_argument('--debug', dest='debug', action='store_true')
parser.add_argument('--fast_epoch', dest='fast_epoch', action='store_true')

# Luminous
parser.add_argument('--model_path', type=str, default="exp/pretrained/pretrained.pth")
parser.add_argument('--build_path', type=str, default="valid/lumi_unseen_no_box.x86_64", required=True)
parser.add_argument('--trial_path', type=str, default="valid/lumi_unseen/", required=True)


# parse arguments
args = parser.parse_args()


# In[ ]:



# parser
parser = argparse.ArgumentParser()

# settings
parser.add_argument('--splits', type=str, default="data/splits/oct21.json")
parser.add_argument('--data', type=str, default="data/json_feat_2.1.0")
parser.add_argument('--reward_config', default='models/config/rewards.json')
parser.add_argument('--eval_split', type=str, choices=['train', 'valid_seen', 'valid_unseen'])
parser.add_argument('--model_path', type=str, default="exp/pretrained/pretrained.pth")
parser.add_argument('--model', type=str, default='models.model.seq2seq_im_mask')
parser.add_argument('--preprocess', dest='preprocess', action='store_true')
parser.add_argument('--shuffle', dest='shuffle', action='store_true')
parser.add_argument('--gpu', dest='gpu', action='store_true')
parser.add_argument('--num_threads', type=int, default=1)

# eval params
parser.add_argument('--max_steps', type=int, default=400, help='max steps before episode termination')
parser.add_argument('--max_fails', type=int, default=10, help='max API execution failures before episode termination')

# eval settings
parser.add_argument('--subgoals', type=str, help="subgoals to evaluate independently, eg:all or GotoLocation,PickupObject...", default="")
parser.add_argument('--smooth_nav', dest='smooth_nav', action='store_true', help='smooth nav actions (might be required based on training data)')
parser.add_argument('--skip_model_unroll_with_expert', action='store_true', help='forward model with expert actions')
parser.add_argument('--no_teacher_force_unroll_with_expert', action='store_true', help='no teacher forcing with expert')

# debug
parser.add_argument('--debug', dest='debug', action='store_true')
parser.add_argument('--fast_epoch', dest='fast_epoch', action='store_true')

# parse arguments
args = parser.parse_args("")


# In[ ]:


args


# In[ ]:


model_path = args.model_path
build_path= args.build_path
trial_path = args.trial_path


# In[ ]:


# multiprocessing settings
mp.set_start_method('spawn')
manager = mp.Manager()


# In[ ]:


from models.eval.eval_task import EvalTask


# In[ ]:


eval_master = EvalTask(args, manager)


# In[ ]:


import os
import json
import numpy as np
from PIL import Image
from datetime import datetime
# from .eval import Eval
from env.thor_env import ThorEnv

import torch
import constants
import torch.nn.functional as F
from torchvision.utils import save_image
from torchvision.transforms.functional import to_tensor
from torchvision.models.detection import maskrcnn_resnet50_fpn
import matplotlib.pyplot as plt
import random


# In[ ]:


env = ThorEnv(build_path=build_path)


# In[ ]:


plt.imshow(env.last_event.frame)

eval_master.args.eval_split = "valid_unseen"


# In[ ]:


r_idx = 0


# In[ ]:


model = eval_master.model
resnet = eval_master.resnet


# In[ ]:


all_trial_paths = load_all_trial_paths(trial_path)


# In[ ]:


model = model.cuda()
model.reset()


# In[ ]:


maskrcnn = maskrcnn_resnet50_fpn(num_classes=119)
maskrcnn.eval()
maskrcnn.load_state_dict(torch.load('weight_maskrcnn.pt'))
maskrcnn = maskrcnn.cuda()


# In[ ]:


success_record = []


# In[ ]:


for idx in tqdm(range(len(all_trial_paths))):
    try:
        traj = json.load(open(all_trial_paths[idx]))
        traj_data = traj

        # scene_name = all_trial_paths[idx].split("/")[2].split('_')[-2].split('-')[-1]
        # scene_name = "FloorPlan" + scene_name + "_physics"
        scene_name = all_trial_paths[idx].split("/")[2].split("]")[1]
        print(scene_name)
        # set up scene
        env.reset(scene_name)

        # break

         # setup scene
        setup_scene(traj, env)

        plt.imshow(env.last_event.frame)
        plt.show()

        ex = traj

        # tokenize language
        traj['ann'] = {
            'goal': revtok.tokenize(remove_spaces_and_lower(ex['turk_annotations']['anns'][r_idx]['task_desc'])) + ['<<goal>>'],
            'instr': [revtok.tokenize(remove_spaces_and_lower(x)) for x in ex['turk_annotations']['anns'][r_idx]['high_descs']] + [['<<stop>>']],
            'repeat_idx': r_idx
        }

        # numericalize language
        traj['num'] = {}
        vocab = model.vocab['word']
        traj['num']['lang_goal'] = vocab.word2index([w.strip().lower() for w in traj['ann']['goal']], train=False)
        traj['num']['lang_instr'] = [vocab.word2index([w.strip().lower() for w in x], train=False) for x in traj['ann']['instr']]

        # extract language features
        feat = model.featurize([(traj_data, False)], load_mask=False)

        # goal instr
        goal_instr = traj_data['turk_annotations']['anns'][r_idx]['task_desc']

        prev_image = None
        prev_action = None
        nav_actions = ['MoveAhead_25', 'RotateLeft_90', 'RotateRight_90', 'LookDown_15', 'LookUp_15']

        prev_class = 0
        prev_center = torch.zeros(2)

        # fix traj
        traj["task_id"] = "???"

        done, success = False, False
        fails = 0
        t = 0
        reward = 0
        while not done:
            # break if max_steps reached
            if t >= args.max_steps:
                break

            # extract visual features
            curr_image = Image.fromarray(np.uint8(env.last_event.frame))
            feat['frames'] = resnet.featurize([curr_image], batch=1).unsqueeze(0).to("cuda")

            # forward model
            m_out = model.step(feat)
            m_pred = model.extract_preds(m_out, [(traj_data, False)], feat, clean_special_tokens=False)
            m_pred = list(m_pred.values())[0]

            # action prediction
            action = m_pred['action_low']
            if prev_image == curr_image and prev_action == action and prev_action in nav_actions and action in nav_actions and action == 'MoveAhead_25':
                dist_action = m_out['out_action_low'][0][0].detach().cpu()
                idx_rotateR = model.vocab['action_low'].word2index('RotateRight_90')
                idx_rotateL = model.vocab['action_low'].word2index('RotateLeft_90')
                action = 'RotateLeft_90' if dist_action[idx_rotateL] > dist_action[idx_rotateR] else 'RotateRight_90'

            if action == '<<stop>>':
                print("\tpredicted STOP")
                break

            # mask prediction
            mask = None
            if model.has_interaction(action):
                class_dist = m_pred['action_low_mask'][0]
                pred_class = np.argmax(class_dist)

                # mask generation
                with torch.no_grad():
                    out = maskrcnn([to_tensor(curr_image).cuda()])[0]
                    for k in out:
                        out[k] = out[k].detach().cpu()

                if sum(out['labels'] == pred_class) == 0:
                    mask = np.zeros((constants.SCREEN_WIDTH, constants.SCREEN_HEIGHT))
                else:
                    masks = out['masks'][out['labels'] == pred_class].detach().cpu()
                    scores = out['scores'][out['labels'] == pred_class].detach().cpu()

                    # Instance selection based on the minimum distance between the prev. and cur. instance of a same class.
                    if prev_class != pred_class:
                        scores, indices = scores.sort(descending=True)
                        masks = masks[indices]
                        prev_class = pred_class
                        prev_center = masks[0].squeeze(dim=0).nonzero().double().mean(dim=0)
                    else:
                        cur_centers = torch.stack([m.nonzero().double().mean(dim=0) for m in masks.squeeze(dim=1)])
                        distances = ((cur_centers - prev_center)**2).sum(dim=1)
                        distances, indices = distances.sort()
                        masks = masks[indices]
                        prev_center = cur_centers[0]

                    mask = np.squeeze(masks[0].numpy(), axis=0)

            # print action
            if args.debug:
                print(action)

            # use predicted action and mask (if available) to interact with the env
            t_success, _, _, err, _ = env.va_interact(action, interact_mask=mask, smooth_nav=args.smooth_nav, debug=args.debug)

            if not t_success:
                fails += 1
                if fails >= args.max_fails:
                    print("Interact API failed %d times" % fails + "; latest error '%s'" % err)
                    break

            # next time-step
            # t_reward, t_done = env.get_transition_reward()
            # reward += t_reward
            t += 1

            prev_image = curr_image
            prev_action = action


        task_nname = traj_data['task_desc'].split("-")[0]
        s, ts = get_goal_conditions_meet(task_nname, traj_data['pddl_params'], env.last_event, env)
        success_record.append([traj_data['task_desc'], s, ts])
        print([task_nname, s, ts])
    except Exception as e:
        print("wrong task: ", all_trial_paths[idx], e)


# In[ ]:


print(success_record)


# In[ ]:




