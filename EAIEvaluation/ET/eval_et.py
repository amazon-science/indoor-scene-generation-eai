#!/usr/bin/env python
# coding: utf-8

# In[ ]:


import os
from tqdm.auto import tqdm
import json
import numpy as np
import torch


# In[ ]:


import ai2thor
print(ai2thor.__version__)


# In[ ]:


# model_path = "/home/ubuntu/research3/ET/logs/pretrained/et_human_synth_pretrained.pth"
# build_path="valid/lumi_unseen_no_box.x86_64"
# trial_path = "valid/lumi_unseen/"


# In[ ]:


os.environ['ET_ROOT'] =  '/home/ubuntu/research3/ET'
os.environ['ET_DATA'] = '/home/ubuntu/research3/ET/data/'
os.environ['ET_LOGS'] = '/home/ubuntu/research3/ET/logs/'


# In[ ]:


import matplotlib.pyplot as plt
from alfred.gen.utils import game_util


# In[ ]:


#from sacred import Experiment

from alfred.config import exp_ingredient, eval_ingredient
from alfred.eval.eval_master import EvalMaster
from alfred.eval.eval_task import evaluate_task, process_eval_task
from alfred.eval.eval_subgoals import evaluate_subgoals, process_eval_subgoals
from alfred.gen import constants
from alfred.utils import eval_util, helper_util


# In[ ]:


from et_train.custom_eval import *


# In[ ]:


args = helper_util.AttrDict(
{'exp': 'pretrained', 'checkpoint': 'et_human_pretrained.pth', 'split': 'valid_seen', 'shuffle': False, 'max_steps': 1000, 'max_fails': 10, 'subgoals': '', 'smooth_nav': False, 'no_model_unroll': False, 'no_teacher_force': False, 'debug': False, 'x_display': '0', 'eval_range': None, 'object_predictor': '/home/ubuntu/research3/ET/logs/pretrained/maskrcnn_model.pth', 'name': 'default', 'model': 'transformer', 'device': 'cuda', 'num_workers': 5, 'pretrained_path': None, 'fast_epoch': False, 'data': {'train': None, 'valid': 'lmdb_human', 'length': 30000, 'ann_type': 'lang'}, 'dout': '/home/ubuntu/research3/ET/logs/pretrained'}
)


# In[1]:


import argparse
# parser
added_parser = argparse.ArgumentParser()

# Luminous
added_parser.add_argument('--model_path', type=str, default="exp/pretrained/pretrained.pth", required=True)
added_parser.add_argument('--build_path', type=str, default="valid/lumi_unseen_no_box.x86_64", required=True)
added_parser.add_argument('--trial_path', type=str, default="valid/lumi_unseen/", required=True)

# parse arguments
added_args = parser.parse_args()


# In[ ]:


model_path = added_args.model_path
build_path=added_args.build_path
trial_path = added_args.build_path


# In[ ]:





# In[ ]:


args.split = "valid_unseen"


# In[ ]:


print(args)


# In[ ]:


model_paths = eval_util.get_model_paths(args)


# In[ ]:


master = EvalMaster(args, model_paths[0])
#trial_queue, log_queue = master.create_queues(model_paths)


# In[ ]:


evaluate_function = evaluate_subgoals if args.subgoals else evaluate_task


# In[ ]:


from alfred.env.thor_env import ThorEnv


# In[ ]:


env = ThorEnv(
      x_display=args.x_display,
      player_screen_width=300,
      player_screen_height=300,
      build_path=build_path)
#env = ThorEnv(args.x_display)


# In[ ]:


from alfred.utils.eval_util import load_agent, load_object_predictor


# In[ ]:


object_predictor = load_object_predictor(args)


# In[ ]:


dataset = master.dataset


# In[ ]:


master.dataset.dataset_info


# In[ ]:


model, extractor = load_agent(
        model_path,
        master.dataset.dataset_info,
        args.device
)


# In[ ]:


dataset.vocab_translate = model.vocab_out


# In[ ]:


model_path_loaded = model_path


# In[ ]:


vocab = {'word': dataset.vocab_in, 'action_low': model.vocab_out}
vocab['word'].name = "lmdb_human"


# In[ ]:


success_record = []
goal_conditions_record = []


# In[ ]:


def load_all_trial_paths(train_root):
    all_trial_paths = []
    for train_task in tqdm(os.listdir(train_root)):
        trial_folder = os.path.join(train_root, train_task)

        if len(os.listdir(trial_folder)) == 0:
            os.rmdir(trial_folder)
            continue

        for trial in os.listdir(trial_folder):
            json_path = os.path.join(trial_folder, trial, "traj_data.json")
            all_trial_paths.append(json_path)
    return all_trial_paths


# In[ ]:


all_trial_paths = load_all_trial_paths(trial_path)


# In[ ]:


len(all_trial_paths)


# In[ ]:


for idx in tqdm(range(len(all_trial_paths))):
    if "heat" not in all_trial_paths[idx]:
        continue
    #print(all_trial_paths[idx])
    #break
    try:
        print(all_trial_paths[idx])
        traj_data = json.load(open(all_trial_paths[idx]))
        model.reset()
        scene_name = all_trial_paths[idx].split("/")[2].split("]")[1]
        #scene_name = all_trial_paths[idx].split("/")[2].split('_')[-2].split('-')[-1]
        #scene_name = "FloorPlan" + scene_name + "_physics"
        print("Floorplan", scene_name)
        # set up scene
        env.reset(scene_name)
        setup_scene(traj_data, env)

        plt.imshow(env.last_event.frame)
        plt.show()

        # Main
        # print("main")

        input_dict = {}

        #vocab['word'].word2index(get_lang_instr(traj_data))

        lang_tensor = vocab['word'].word2index(get_lang_instr(traj_data))
        input_dict['lang'] = torch.tensor([lang_tensor]).to("cuda")

        input_dict['frames'] = eval_util.get_observation(env.last_event, extractor)

        #input_dict['lang'].shape

        input_dict['lengths_lang'] = torch.tensor([input_dict['lang'].size(1)]).to(torch.device("cuda:0"))
        #input_dict['lengths_frames'] = torch.tensor([input_dict['frames'].size(1)]).to(torch.device("cuda:0"))
        input_dict['length_lang_max'] = max(input_dict['lengths_lang'])
        #input_dict['length_frames_max'] = max(input_dict['lengths_frames'])

        #input_dict['frames'].shape

        prev_action = None
        t, num_fails, reward = 0, 0, 0

        episode_end, prev_action, num_fails, _, _ = eval_util.agent_step(model, input_dict, vocab, prev_action, env, args, num_fails, object_predictor)

        while t < args.max_steps:
            # print(t, prev_action)
            # plt.imshow(env.last_event.frame)
            # plt.show()
            # get an observation and do an agent step
            input_dict['frames'] = eval_util.get_observation(env.last_event, extractor)
            episode_end, prev_action, num_fails, _, _ = eval_util.agent_step(
                model, input_dict, vocab, prev_action, env, args, num_fails, object_predictor)
            # get rewards
            # reward += env.get_transition_reward()[0]
            t += 1
            # break if stop is predicted or args.max_fails is reached
            if episode_end:
                break
                
        task_nname = traj_data['task_desc'].split("-")[0]
        s, ts = get_goal_conditions_meet(task_nname, traj_data['pddl_params'], env.last_event, env)
        success_record.append([traj_data['task_desc'], s, ts])
        print(s, ts)
    except Exception as e:
        print("wrong task: ", all_trial_paths[idx], e)


# In[ ]:


print(success_record)


# In[ ]:




