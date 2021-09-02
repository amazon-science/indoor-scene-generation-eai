#!/usr/bin/env python
# coding: utf-8

# In[ ]:


import matplotlib.pyplot as plt
from tqdm import tqdm


# In[ ]:


import os, sys, argparse, json, copy, logging

import torch
import torch.multiprocessing as mp


os.environ['ALFRED_ROOT'] = os.getcwd()
sys.path.append(os.path.join(os.environ['ALFRED_ROOT']))

from data.dataset import AlfredDataset
from models.config.configs import Config
from models.model.mmt import MultiModalTransformer
from models.nn.mrcnn import MaskRCNNDetector
from models.eval.eval_task import EvalTaskMMT
from models.eval.eval_subgoals import EvalSubgoalsMMT


# In[ ]:


model_path = "/home/ubuntu/research3/ET/logs/pretrained/et_human_synth_pretrained.pth"
build_path="valid/lumi_unseen_no_box.x86_64"
trial_path = "valid/lumi_unseen/"


# In[ ]:


from models.utils.vocab import Vocab

vocab = Vocab()
vocab.load("data/full_2.1.0_pp/vocab")


# In[ ]:


import revtok
from hitut_train.custom_eval import get_goal_conditions_meet, remove_spaces_and_lower, setup_scene, load_all_trial_paths


# In[ ]:


core_mask_op = "taskset -pc %s %d" %('0-40', os.getpid())
os.system(core_mask_op)

# multiprocessing settings
mp.set_start_method('spawn')
manager = mp.Manager()

# parser
parser = argparse.ArgumentParser()
Config(parser)

# eval settings
parser.add_argument('--reward_config', default='models/config/rewards.json')
parser.add_argument('--eval_split', type=str, default='valid_seen', choices=['valid_seen', 'valid_unseen'])
parser.add_argument('--eval_path', type=str, default="exp/something")
parser.add_argument('--ckpt_name', type=str, default="model_best_seen.pth")
parser.add_argument('--num_core_per_proc', type=int, default=5, help='cpu cores used per process')
# parser.add_argument('--model', type=str, default='models.model.seq2seq_im_mask')
parser.add_argument('--subgoals', type=str, help="subgoals to evaluate independently, eg:all or GotoLocation,PickupObject...", default="")
parser.add_argument('--smooth_nav', dest='smooth_nav', action='store_true', help='smooth nav actions (might be required based on training data)')
parser.add_argument('--max_high_steps', type=int, default=20, help='max steps before a high-level episode termination')
parser.add_argument('--max_high_fails', type=int, default=5, help='max failing times to try high-level proposals')
parser.add_argument('--max_fails', type=int, default=999, help='max failing times in ALFRED benchmark')
parser.add_argument('--max_low_steps', type=int, default=50, help='max steps before a low-level episode termination')
parser.add_argument('--only_eval_mask', dest='only_eval_mask', action='store_true')
parser.add_argument('--use_gt_navigation', dest='use_gt_navigation', action='store_true')
parser.add_argument('--use_gt_high_action', dest='use_gt_high_action', action='store_true')
parser.add_argument('--use_gt_mask', dest='use_gt_mask', action='store_true')
parser.add_argument('--save_video', action='store_true')

parser.add_argument('--eval_disable_feat_lang_high', help='do not use language features as high input', action='store_true')
parser.add_argument('--eval_disable_feat_lang_navi', help='do not use language features as low-navi input', action='store_true')
parser.add_argument('--eval_disable_feat_lang_mani', help='do not use language features as low-mani input', action='store_true')
parser.add_argument('--eval_disable_feat_vis', help='do not use visual features as input', action='store_true')
parser.add_argument('--eval_disable_feat_action_his', help='do not use action history features as input', action='store_true')
parser.add_argument('--eval_enable_feat_vis_his', help='use additional history visual features as input', action='store_true')
parser.add_argument('--eval_enable_feat_posture', help='use additional agent posture features as input', action='store_true')

# Luminous
parser.add_argument('--model_path', type=str, default="exp/pretrained/pretrained.pth")
parser.add_argument('--build_path', type=str, default="valid/lumi_unseen_no_box.x86_64", required=True)
parser.add_argument('--trial_path', type=str, default="valid/lumi_unseen/", required=True)

# parse arguments
args = parser.parse_args()


# In[ ]:


model_path = args.model_path
build_path= args.build_path
trial_path = args.trial_path


# In[ ]:


args.eval_path = model_path #"exp/Jan27-roberta-mix/noskip_lr_mix_all_E-xavier768d_L12_H768_det-sep_dp0.1_di0.1_step_lr5e-05_0.999_type_sd999"
args.ckpt = "model_best_seen.pth"
args.gpu = True
args.max_high_fails = 9
args.max_fails = 10
args.eval_split = "valid_unseen"
args.eval_enable_feat_posture = True
args.num_threads = 0
args.name_temp = "eval_valid_unseen"


# --eval_path exp/Jan27-roberta-mix/noskip_lr_mix_all_E-xavier768d_L12_H768_det-sep_dp0.1_di0.1_step_lr5e-05_0.999_type_sd999 --ckpt model_best_seen.pth --gpu --max_high_fails 9 --max_fails 10 --eval_split valid_seen --eval_enable_feat_posture --num_threads 4 --name_temp eval_valid_seen

# In[ ]:


args_model = argparse.Namespace(**json.load(open(os.path.join(args.eval_path, 'config.json'), 'r')))
args.use_bert = args_model.use_bert
args.bert_model = args_model.bert_model
# args.inner_dim = 1024


# In[ ]:


# load alfred data and build pytorch data sets and loaders
alfred_data = AlfredDataset(args)


# In[ ]:


# setup model
device = torch.device('cuda') if args.gpu else torch.device('cpu')
ckpt_path = os.path.join(args.eval_path, args.ckpt_name)
ckpt = torch.load(ckpt_path, map_location=device)
model = MultiModalTransformer(args_model, alfred_data)
model.load_state_dict(ckpt, strict=False)   #strict=False
model.to(model.device)
models = model


# In[ ]:


# log dir
eval_type = 'task' if not args.subgoals else 'subgoal'
gt_navi = '' if not args.use_gt_navigation else '_gtnavi'
gt_sg = '' if not args.use_gt_high_action else '_gtsg'
input_str = ''
if args.eval_disable_feat_lang_high:
    input_str += 'nolanghigh_'
if args.eval_disable_feat_lang_mani:
    input_str += 'nolangmani_'
if args.eval_disable_feat_lang_navi:
    input_str += 'nolangnavi_'
if args.eval_disable_feat_vis:
    input_str += 'novis_'
if args.eval_disable_feat_action_his:
    input_str += 'noah_'
if args.eval_enable_feat_vis_his:
    input_str += 'hasvh_'
if args.eval_enable_feat_posture:
    input_str += 'haspos_'
log_name = '%s_%s_%s_maxfail%d_%s%s%s.log'%(args.name_temp, eval_type, args.eval_split, args.max_high_fails,
    input_str, gt_navi, gt_sg)
if args.debug:
    log_name = log_name.replace('.log', '_debug.log')
if isinstance(models, dict):
    log_name = log_name.replace('.log', '_sep.log')
args.log_dir = os.path.join(args.eval_path, log_name)


# In[ ]:


log_level = logging.DEBUG if args.debug else logging.INFO
log_handlers = [logging.StreamHandler(), logging.FileHandler(args.log_dir)]
logging.basicConfig(handlers=log_handlers, level=log_level,
    format='%(asctime)s %(message)s', datefmt='%Y-%m-%d %H:%M:%S')


# In[ ]:


args.log_dir


# In[ ]:


from models.eval.eval_mmt import check_input


# In[ ]:


logging.info('model: %s'%ckpt_path)
check_input(model, args)


# In[ ]:


args.subgoals


# In[ ]:


# setup object detector
detector = MaskRCNNDetector(args, detectors=[args.detector_type])

# eval mode
if args.subgoals:
    eval_master = EvalSubgoalsMMT(args, alfred_data, models, detector, manager)
else:
    eval_master = EvalTaskMMT(args, alfred_data, models, detector, manager)


# In[ ]:


# eval_master


# In[ ]:


import os, time, sys, logging, copy, shutil, glob
import json
import cv2
import numpy as np
from PIL import Image
from datetime import datetime

import torch

sys.path.append(os.path.join(os.environ['ALFRED_ROOT']))

from gen.constants import *
from gen.utils.image_util import decompress_mask
from gen.utils.video_util import VideoSaver, save_frame_failure
from env.thor_env import ThorEnv
from models.eval.eval import Eval, EvalMMT


# In[ ]:


# start THOR
env = ThorEnv(build_path=build_path)


# In[ ]:


plt.imshow(env.last_event.frame)
plt.show()


# In[ ]:


#env.reset(scene_name_or_num="v308_1")


# In[ ]:


#env


# In[ ]:


# set logger
log_level = logging.DEBUG if args.debug else logging.INFO
log_handlers = [logging.StreamHandler(), logging.FileHandler(args.log_dir)]
logging.basicConfig(handlers=log_handlers, level=log_level,
   format='%(asctime)s %(message)s', datefmt='%Y-%m-%d %H:%M:%S')


# # Main

# In[ ]:


success_record = []


# In[ ]:


all_trial_paths = load_all_trial_paths(trial_path)


# In[ ]:


len(all_trial_paths)


# In[ ]:


idx = 0


# In[ ]:


all_trial_paths[idx]


# In[ ]:


all_trial_paths[0].split("/")[4].split("]")[1]


# In[ ]:


for idx in tqdm(range(len(all_trial_paths))):
    try:
        with open(all_trial_paths[idx], 'r') as f:
            traj_raw = json.load(f)

        traj_data = traj_raw

        prtfunc = print #logging.debug
        prtfunc('-'*50)
        prtfunc(traj_data['task_desc'])

        traj_data['task_desc']

        #scene_name = traj_data['task_desc'].split("/")[0].split('-')[-1]
        #scene_name = "FloorPlan" + scene_name + "_physics"
        scene_name = all_trial_paths[idx].split("/")[4].split("]")[1]
        print(scene_name)
        env.reset(scene_name)

        scene_name

        model_navi = model_mani = model

        setup_scene(traj_data, env)

        plt.imshow(env.last_event.frame)
        plt.show()

        traj_data['raw'] = traj_raw

        traj_raw['task_id'] = "??"

        try:
            horizon = traj_data['raw']['scene']['init_action']['horizon']
            rotation = traj_data['raw']['scene']['init_action']['rotation']
        except:
            horizon = rotation = 0

        all_done, success = False, False
        high_idx, high_fails, high_steps, api_fails = 0, 0, 0, 0
        high_history = ['[SOS]', 'None']
        high_history_before_last_navi = copy.deepcopy(high_history)
        low_history_col = []
        reward = 0
        t = 0
        stop_cond = ''
        failed_events= ''
        terminate = False

        path_name = traj_data['raw']['task_id'] + '_ridx' + str(0)
        visualize_info = {'save_path': os.path.join(args.log_dir.replace('.log', ''), path_name)}

        t_start_total = time.time()

        # while not all_done

        while not all_done:
            high_steps += 1
            # break if max_steps reached
            if high_fails >= args.max_high_fails:
                stop_cond += 'Terminate due to reach maximum high repetition failures'
                break

            if high_idx >= args.max_high_steps:
                stop_cond += 'Terminate due to reach maximum high steps'
                break

            # traj_data

            ex = traj_data

            # goal instruction
            task_desc = ex['turk_annotations']['anns'][0]['task_desc']

            # step-by-step instructions
            high_descs = ex['turk_annotations']['anns'][0]['high_descs']

            # tokenize language
            traj_data['lang'] = {
                'repeat_idx': 0,
                'goal_tokenize': ['[SEP]'] + revtok.tokenize(remove_spaces_and_lower(task_desc)) + ['[SEP]'],
                'instr_tokenize': [['[SEP]'] + revtok.tokenize(remove_spaces_and_lower(x)) + ['[SEP]'] for x in high_descs]
            }

            traj = traj_data

            traj['lang']['goal'] = vocab.seq_encode(traj['lang']['goal_tokenize'])
            traj['lang']['instr'] = [vocab.seq_encode(x) for x in traj['lang']['instr_tokenize']]

            with torch.no_grad():
                curr_frame = env.last_event.frame
                masks, boxes, classes, scores = detector.get_preds_step(curr_frame)
                # prtfunc('MaskRCNN Top8: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(classes[:8], scores[:8])]))

                observations = {}
                observations['lang'] = traj_data['lang']['goal'] if not args.eval_disable_feat_lang_high else None
                observations['vis'] = [boxes, classes, scores] if not args.eval_disable_feat_vis else None
                observations['act_his'] = high_history if not args.eval_disable_feat_action_his else None
                observations['vis_his'] = None
                observations['pos'] = None

                # model predicts topk proposals
                preds, probs  = model_mani.step(observations, 'high', topk=5)
                high_actions, high_args = preds['type'], preds['arg']

            # Simply use the top1 high prediction instead of using multiple high proposals
            high_action = high_actions[0]
            high_arg = high_args[0]
            visualize_info['high_idx'] = high_idx
            visualize_info['high_action'] = high_action
            visualize_info['high_arg'] = high_arg

        #     prtfunc('-'*50)
        #     prtfunc('Task goal: ' + ''.join(traj_data['lang']['goal_tokenize']).replace('  ', ' '))
        #     prtfunc('High proposals:')
        #     prtfunc('action: ' + ''.join(['(%s, %.3f)'%(high_abbr[i], j) for i, j in zip(high_actions, probs['type'])]))
        #     prtfunc('arg: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(high_args, probs['arg'])]))

            if high_action == 'GotoLocation':
                high_history_before_last_navi = copy.deepcopy(high_history)


            if high_action == 'NoOp':
                all_done = True
                stop_cond += 'Predictes a high-level NoOp to terminate!'
                break

            # print action
        #     prtfunc('high history' + str(high_history))
        #     prtfunc('high pred: %s(%s)'%(high_action, high_arg))
        #     prtfunc('high idx: %d'%high_idx)
        #     prtfunc('high fails: %d'%high_fails)

            # go into the low-level action prediction loop
            subgoal_done, prev_t_success = False, False
            low_idx = 0
            low_history = [high_action, high_arg]
            low_vis_history = []
            while not subgoal_done:
                # break if max_steps reached
                if low_idx >= args.max_low_steps:
                    failed_events += 'SG %s(%s) not done in %d steps |'%(high_action, high_arg, args.max_low_steps)
                    # prtfunc("Reach maximum low step limitation. Subgoal '%s(%s)' failed" %(high_action, high_arg))
                    break

        #         prtfunc('-'*50)
        #         prtfunc('Completing subgoal: %s(%s)'%(high_action, high_arg))

                if args.use_gt_navigation and high_action == 'GotoLocation':
                    try:
                        proposals = [(vocabs['out_vocab_low_type'].id2w(traj_data['low']['dec_out_low_actions'][high_idx][low_idx]), None, None)]
                        # prtfunc('Use ground truth navigation actions')
                    except:
                        stop_cond += 'Terminate due to do not find proper ground truth navigation actions! '
                        all_done = True
                        break

                else:
                    with torch.no_grad():
                        task_type = 'low_navi' if high_action == 'GotoLocation' else 'low_mani'
                        model = model_navi if task_type == 'low_navi' else model_mani
                        # visual observation
                        curr_frame = env.last_event.frame
                        masks, boxes, classes, scores = detector.get_preds_step(curr_frame)
                        # prtfunc('MaskRCNN Top8: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(classes[:8], scores[:8])]))

                        # disable language directives when retry the navigation subgoal
                        global_disable = args.eval_disable_feat_lang_navi if 'navi' in task_type else args.eval_disable_feat_lang_mani
                        use_lang = not global_disable and high_idx == len(low_history_col) and high_idx<len(traj_data['lang']['instr'])
                        if use_lang:
                            pass
                            # prtfunc('Instruction: ' + ''.join(traj_data['lang']['instr_tokenize'][high_idx]).replace('  ', ' '))
                        use_vis_his = args.eval_enable_feat_vis_his and 'navi' in task_type
                        use_pos = args.eval_enable_feat_posture and 'navi' in task_type


                        observations = {}
                        observations['lang'] = traj_data['lang']['instr'][high_idx] if use_lang else None
                        observations['vis'] = [boxes, classes, scores] if not args.eval_disable_feat_vis else None
                        observations['act_his'] = low_history if not args.eval_disable_feat_action_his else None
                        observations['vis_his'] = low_vis_history if use_vis_his else None
                        observations['pos'] = {'rotation': int((rotation%360)/90),
                                                            'horizon': int(horizon/15)%12} if use_pos else None

                        # model predicts topk proposals
                        preds, probs = model.step(observations, task_type, topk=13)
                        low_actions, low_args, mask_ids = preds['type'], preds['arg'], preds['mask']

                        if task_type == 'low_navi':
                            # proposals = [(low_actions[i], None, None) for i in range(5) if probs['type'][i]]
                            proposals = [(low_actions[0], None, None)]
                            # Obstruction Detection technique from MOCA
                            if low_actions[0] == 'MoveAhead':
                                cands = ['RotateLeft', 'RotateRight', 'NoOp']
                                cand_rank= [low_actions.index(i) for i in cands]
                                probs['type'][cand_rank[2]] += 0.05 # encourage stopping
                                cand_probs = [probs['type'][i] for i in cand_rank]
                                cand_idx = cand_probs.index(max(cand_probs))
                                proposals.append((cands[cand_idx], None, None))
                            # prtfunc('Low proposal: '+ str([i for i,j,k in proposals]))
                        else:
                            # proposals = [(low_actions[0], low_args[i], mask_ids[i]) for i in range(3)]
                            proposals = []
                            for act_idx, act_prob in enumerate(probs['type']):
                                for arg_idx, arg_prob in enumerate(probs['type']):
                                    if act_prob > 0.1 and arg_prob > 0.1:
                                        proposals.append((low_actions[act_idx], low_args[arg_idx], mask_ids[arg_idx]))
                                        #prtfunc('Low proposal: %s(%.2f) %s(%.2f)'%(low_actions[act_idx], act_prob,low_args[arg_idx], arg_prob))

                # prtfunc('Low proposals:' + str(proposals))
                # prtfunc('action: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(low_actions, probs['type'])]))
                # prtfunc('arg: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(low_args, probs['arg'])]))
                # prtfunc('mask: ' + ''.join(['(%s, %.3f)'%(i, j) for i, j in zip(mask_ids, probs['mask'])]))

                t_success = False
                for action, low_arg, mask_id in proposals:
                    if action == 'NoOp' and (high_action=='GotoLocation' or prev_t_success):
                        low_history += [action, low_arg]
                        low_vis_history += [observations['vis']] if args.eval_enable_feat_vis_his else []
                        subgoal_done = True
                        # prtfunc("Subgoal '%s(%s)' is done! low steps: %d"%(high_action, high_arg, low_idx+1))
                        break

                    # disable masks/arguments for non-ineractive actions
                    if action in NON_INTERACT_ACTIONS:
                        low_arg = 'None'
                        mask = None
                    elif args.use_gt_mask:
                        mask = decompress_mask(traj_data['low']['mask'][high_idx][low_idx])
                    else:
                        try:
                            mask = masks[mask_id - 1]
                            mask_cls_pred = classes[mask_id-1]
                            if not similar(mask_cls_pred, low_arg):
                                failed_events += 'bad mask: %s -> %s |'%(mask_cls_pred, low_arg)
                                # prtfunc('Agent: no correct mask grounding!')
                                mask = None
                        except:
                            # if low_arg in classes:
                            #     mask = masks[classes.index(low_arg)]
                            if mask_id == 0:
                                failed_events += 'pred mask: 0 |'
                                #prtfunc('Agent: no available mask')
                                mask = None
                            else:
                                failed_events += 'Invaild mask id: %s |'%str(mask_id)
                                #prtfunc('Invaild mask id: %s'%str(mask_id))
                                mask = None

                    if args.save_video:
                        visualize_info['low_idx'] = low_idx
                        visualize_info['low_action'] = action
                        visualize_info['low_arg'] = low_arg
                        visualize_info['global_step'] = t
                        visualize_info['mask'] = mask
                        visualize_info['bbox'] = boxes[mask_id - 1] if mask is not None else None
                        visualize_info['class'] = mask_cls_pred  if mask is not None else None
                        visualize_info_ = visualize_info
                    else:
                        visualize_info_ = None

                    if action not in NON_INTERACT_ACTIONS and mask is None:
                        if args.save_video:
                            save_frame_failure(env.last_event.frame[:, :, ::-1], visualize_info_)
                        prev_t_success = False
                        continue

                    # use action and predicted mask (if available) to interact with the env
                    t_success, _, _, err, _ = env.va_interact(action, interact_mask=mask,
                        smooth_nav=args.smooth_nav, debug=args.debug, visualize_info=visualize_info_)
                    t += 1
                    prev_t_success = t_success

                    if t_success:
                        low_history += [action, low_arg]
                        low_vis_history += [observations['vis']] if args.eval_enable_feat_vis_his else []
                        rotation += 90 if action == 'RotateRight' else -90 if action == 'RotateLeft' else 0
                        horizon += 15 if action == 'LookUp' else -15 if action == 'LookDown' else 0
                        #prtfunc('low pred: %s(%s)'%(action, low_arg))
                        #prtfunc('Agent posture: rotation: %d horizon: %d'%(rotation, horizon))
                        #prtfunc('high idx: %d (fails: %s)  low idx: %d'%(high_idx, high_fails, low_idx))
                        #prtfunc('Successfully executed!')
                        #prtfunc('Low history: '+' '.join(['%s'%i for i in low_history[::2]]))
                        # t_reward, t_done = env.get_transition_reward()
                        # reward += t_reward
                        low_idx += 1
                        break
                    else:
                        api_fails += 1
                        if api_fails >= args.max_fails:
                            stop_cond = 'Reach 10 Api fails'
                            terminate = True
                        failed_events += 'bad action: %s(%s) api fail: %d |'%(action, low_arg, api_fails)
                        #prtfunc('Low pred: %s(%s)'%(action, low_arg))
                        #prtfunc('Low action failed! Try another low proposal')

                if terminate:
                    break

                if not prev_t_success:   # fails in all the proposals, get stuck
                    failed_events += 'SG %s(%s) fail(%d): no valid proposal |'%(high_action, high_arg, high_fails)
                    #prtfunc("Failed in all low proposals. Subgoal '%s(%s)' failed" %(high_action, high_arg))
                    break

            # out of the low loop and return to the high loop
            if terminate:
                break

            if subgoal_done:
                if high_idx == len(low_history_col):
                    # a new subgoal is completed
                    low_history_col.append(low_history)
                    # high_fails = 0
                else:
                    # a previously failed subgoal is completed
                    low_history_col[high_idx].extend(low_history[2:])
                high_history += [high_action, high_arg]
                high_idx += 1
            else:
                high_fails += 1
                if high_action == 'GotoLocation':
                    # if a navigation subgoal fails, simply retry it
                    high_history = high_history
                    high_idx = high_idx
                    failed_events += 'Navi failed (step: %d) |'%high_steps
                    #prtfunc("Navigation failed. Try again!")
                else:
                    # if a manipulative subgoal fails, retry the navigation subgoal before that
                    high_history = copy.deepcopy(high_history_before_last_navi)
                    high_idx = int(len(high_history)/2 - 1)
                    failed_events += 'SG %s(%s) failed: go back to hidx: %d |'%(high_action, high_arg, high_idx)
                    #prtfunc("Subgoal '%s(%s)' failed. Retry the navigation before that!" %(high_action, high_arg))


        task_nname =  traj_data['task_desc'].split("-")[0]
        s, ts = get_goal_conditions_meet(task_nname, traj_data['pddl_params'], env.last_event, env)
        success_record.append([traj_data['task_desc'], s, ts])

        print(task_nname, s, ts)
    except Exception as e:
        print("wrong task: ", all_trial_paths[idx], e)


# In[ ]:


print(success_record)


# In[ ]:




