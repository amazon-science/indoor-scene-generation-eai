from logging import debug
import os
import json
import pickle
import torch
import numpy as np
import torch
from torch.functional import split
from torch.utils.data.dataset import Dataset


from io import BytesIO
from alfred.utils import data_util, model_util
from tqdm import tqdm

from alfred.gen import constants
from alfred.utils.data_util import load_vocab
from vocab import Vocab

import random

class CustomAlfredDataset(Dataset):
    def __init__(self, root_folder:str, args, split="train") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False
        self.max_subgoals = constants.MAX_SUBGOALS

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

class CustomSpeakerDataset(Dataset):
    def __init__(self, root_folder:str, args, split="train") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False

        self.ann_type = args.data['ann_type']

        # load vocabularies for input language and output actions
        vocab = data_util.load_vocab("lmdb_human")
        self.vocab_in = vocab['word']
        out_type = 'action_high'
        self.vocab_out = vocab[out_type]

        self.json_folder = os.path.join(self.root_folder, "jsons")
        self.feat_folder = os.path.join(self.root_folder, "feats")
        self.mask_folder = os.path.join(self.root_folder, "masks")
        
        self.json_paths = []
        self.mask_paths = []
        self.feat_paths = []

        self.load_json_mask_feat_paths()
    
    def __len__(self):
        return len(self.json_paths)
    
    def load_json_mask_feat_paths(self):
        for json_file in tqdm(os.listdir(self.json_folder)):
            if json_file.startswith(self.split):
                self.json_paths.append(os.path.join(self.json_folder,json_file))

                feat_file = json_file.split(".")[0] + ".pt"
                self.feat_paths.append(os.path.join(self.feat_folder,feat_file))
                
                mask_file = json_file.split(".")[0] + ".pkl"
                self.mask_paths.append(os.path.join(self.mask_folder,mask_file))

            #train:look_at_obj_in_light-AlarmClock-None-DeskLamp-301:trial_T20190s

    def __getitem__(self, index):
        task_json = random.choice(pickle.load(open(self.json_paths[index], "rb")))

        # dataset name
        task_json["dataset_name"] = self.name

        #traj_mask = torch.load(self.mask_paths[index])

        feat_dict = {}
        feat_dict['lang'] = self.load_lang(task_json)
        if 'frames' in self.ann_type:
            traj_feat = torch.load(self.feat_paths[index])
            feat_dict['frames'] = traj_feat

        feat_dict['action'] = self.load_action(
                task_json, self.vocab_out, None, 'action_high')
        
        # remove all the lang key/value pairs if only frames are used as input
        if self.ann_type == 'frames':
            keys_lang = [key for key in feat_dict if key.startswith('lang')]
            for key in keys_lang:
                feat_dict.pop(key)

        # wrong fix
        self.wrong_fix(feat_dict)

        return task_json, feat_dict
        
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

    def wrong_fix(self, feat):
        feat_action = feat['action']

        # fix sink -> sinkbasin
        feat['action'] = [a if a != 94 else 24 for a in feat_action]

class CustomInstructionDataset():
    def __init__(self, root_folder:str, args, split="train") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False
        self.max_subgoals = constants.MAX_SUBGOALS
        self.start_token_index = 1

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
        
        self.high_pddls = []
        self.lang_descs = []

        self.load_json_mask_feat_paths()
    
    def __len__(self):
        return len(self.lang_descs)
    
    def load_json_mask_feat_paths(self):
        debug_max_load = 0
        for json_file in tqdm(os.listdir(self.json_folder)):
            debug_max_load += 1
            # if debug_max_load > 100:
            #     break
            if json_file.startswith(self.split):
                json_array = pickle.load(open(os.path.join(self.json_folder,json_file), "rb"))
                for json_item in json_array:
                    for i in range(len(json_item['num']['lang_instr']) - 1):
                        lang = [self.start_token_index] + json_item['num']['lang_instr'][i]
                        action_num_high = json_item['num']['action_high'][i]
                        high_pddl = [action_num_high['action']] +  action_num_high['action_high_args']

                        self.lang_descs.append(lang) 
                        self.high_pddls.append(high_pddl)

            #train:look_at_obj_in_light-AlarmClock-None-DeskLamp-301:trial_T20190s

    def __getitem__(self, index):
        return torch.tensor(self.high_pddls[index]), torch.tensor(self.lang_descs[index])


class CustomSynthDataset(Dataset):
    def __init__(self, root_folder:str, args, split="train", lang_fix_style="ET") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False
        self.max_subgoals = constants.MAX_SUBGOALS
        self.lang_fix_style=lang_fix_style

        # load the vocabulary for object classes
        self.vocab_obj = torch.load(os.path.join(
            constants.ET_ROOT, constants.OBJ_CLS_VOCAB))
        # load vocabularies for input language and output actions
        vocab = data_util.load_vocab("lmdb_human")

        # modify vocab
        self.vocab_in = vocab['action_high']
        self.vocab_in._index2word[3] = '<<instr>>'
        del self.vocab_in._word2index['<<mask>>']
        self.vocab_in._word2index['<<instr>>'] = 3

        out_type = 'action_low' 
        self.vocab_out = vocab[out_type]

        self.json_folder = os.path.join(self.root_folder, "jsons")
        self.feat_folder = os.path.join(self.root_folder, "feats")
        self.mask_folder = os.path.join(self.root_folder, "masks")
        
        self.json_trajs = []
        self.mask_paths = []
        self.feat_paths = []

        # some images feat gets lost during processing
        self.all_features_list = os.listdir(self.feat_folder)

        self.load_json_mask_feat_paths()

    def __len__(self):
        return len(self.json_trajs)
    
    def load_json_mask_feat_paths(self):
        debug_length = 0
        for json_file in tqdm(os.listdir(self.json_folder)):
            debug_length += 1
            if debug_length > 2000:
                break
            if json_file.startswith(self.split):
                json_array = pickle.load(open(os.path.join(self.json_folder,json_file), "rb"))
                for json_item in json_array:
                    feat_file = json_file.split(".")[0] + ".pt"

                    # task filtration
                    if feat_file not in self.all_features_list:
                        continue
                    
                    if self.split == "train" and ("slice" in json_file.lower() and self.lang_fix_style == "Luminous"):
                        continue

                    if self.split == "train" and ("heat" in json_file.lower() or "cool" in json_file.lower()):
                        #if np.random.rand() < 0.8:
                        continue

                    # if "lumi_processed_train" in self.name and "look_at_obj_in_light" in json_file.lower():
                    #     # round one lumious data set: light bug
                    #     continue



                    self.feat_paths.append(os.path.join(self.feat_folder,feat_file))
                    
                    #self.json_paths.append(os.path.join(self.json_folder,json_file))
                    self.json_trajs.append(json_item)

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
        '''
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
        high_lang_full = []# [2] # <<goal>> token
        action_num_high = task_json['num']['action_high']
        for action_h in action_num_high:
            high_pddl = [action_h['action']] +  action_h['action_high_args']
            high_lang_full += high_pddl #+ [3] #<<instr>> token

        # wrong fix: don't know why
        if self.lang_fix_style == "ET":
            wrong_reindex = {94:24, 95:46, 96:69} # sink->sinkbasin, tvstand->sidetable, bathtub ->bathtubasin
            high_lang_full = [a if a not in wrong_reindex else wrong_reindex[a] for a in high_lang_full]
        elif self.lang_fix_style == "Luminous":
            wrong_reindex = {94:53} # ? -> ToggleObject
            high_lang_full = [a if a not in wrong_reindex else wrong_reindex[a] for a in high_lang_full]
        # lang_num_goal = task_json['num']['lang_goal']
        # lang_num_instr = sum(task_json['num']['lang_instr'], [])
        # lang_num = lang_num_goal + lang_num_instr
        return high_lang_full

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


        #wrong fix: moveahead name
        wrong_reindex = {17:6} # sink->sinkbasin, tvstand->sidetable, bathtub ->bathtubasin
        lang_action = [a if a not in wrong_reindex else wrong_reindex[a] for a in lang_action]

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



class CustomNaturalLanguageDataset(Dataset):
    def __init__(self, root_folder:str, args, split="train", subtask="GotoLocation") -> None:
        super().__init__()
        self.args = args
        self.root_folder = root_folder
        self.name = self.root_folder
        self.split = split
        self.test_mode = False
        self.max_subgoals = constants.MAX_SUBGOALS
        self.subtask = subtask

        # CONSTANTS
        self.low_action_vocab_size = 17
        self.high_action_vocab_size = 94

        # load the vocabulary for object classes
        self.vocab_obj = torch.load(os.path.join(
            constants.ET_ROOT, constants.OBJ_CLS_VOCAB))
        # load vocabularies for input language and output actions
        vocab = data_util.load_vocab("lmdb_human")
        self.vocab_in = Vocab(vocab['action_low']._index2word + ["h_" + word for word in vocab['action_high']._index2word])
        self.vocab_in.name = "lmdb_human_speaker"
        out_type = 'word' 
        self.vocab_out = vocab[out_type]

        self.json_folder = os.path.join(self.root_folder, "jsons")
        self.feat_folder = os.path.join(self.root_folder, "feats")
        self.mask_folder = os.path.join(self.root_folder, "masks")
        
        self.json_trajs = []
        self.mask_paths = []
        self.feat_paths = []

        self.load_json_mask_feat_paths()

        self.feature_pieces = []
        self.lang_target_pieces = []
        self.action_instr_pieces = []

        self.prepare_data_instances()
    
    def __len__(self):
        return len(self.lang_target_pieces)
    
    def load_json_mask_feat_paths(self):
        debug_idx = 0
        all_folders = os.listdir(self.json_folder)
        random.shuffle(all_folders)
        for json_file in tqdm(all_folders):
            debug_idx += 1
            if debug_idx > 2000:
                break
            if json_file.startswith(self.split):
                json_array = pickle.load(open(os.path.join(self.json_folder,json_file), "rb"))
                for json_item in json_array:
                    #self.json_paths.append(os.path.join(self.json_folder,json_file))
                    self.json_trajs.append(json_item)
                    
                    feat_file = json_file.split(".")[0] + ".pt"
                    self.feat_paths.append(os.path.join(self.feat_folder,feat_file))
                    
                    mask_file = json_file.split(".")[0] + ".pkl"
                    self.mask_paths.append(os.path.join(self.mask_folder,mask_file))

            #train:look_at_obj_in_light-AlarmClock-None-DeskLamp-301:trial_T20190s

    def prepare_data_instances(self):
        print("Preparing data instances")
        for index in tqdm(range(len(self.json_trajs))):
            task_json = self.json_trajs[index]
            traj_feat = torch.load(self.feat_paths[index])

            image_feat_start_idx = 0
            for i in range(len(task_json['num']['lang_instr'])):
                # target language
                lang_instr = task_json['num']['lang_instr'][i]
                self.lang_target_pieces.append(lang_instr)

                # low action seq
                low_instr = [a['action'] for a in task_json['num']['action_low'][i]]
                
                # high instr
                high_action = task_json['num']['action_high'][i]
                high_instr = [high_action['action']] + high_action['action_high_args']
                high_instr = [instr + self.low_action_vocab_size for instr in high_instr] # offset

                self.action_instr_pieces.append(high_instr + low_instr)
                
                # image features
                self.feature_pieces.append(traj_feat[image_feat_start_idx: image_feat_start_idx + len(low_instr)])
                image_feat_start_idx += len(low_instr)


    def __getitem__(self, index):
        # task_json = self.json_trajs[index]

        # # dataset name
        # task_json["dataset_name"] = self.name

        # traj_feat = torch.load(self.feat_paths[index])
        # #traj_mask = torch.load(self.mask_paths[index])

        # feat_dict = self.load_features(task_json)
        # feat_dict["frames"] = traj_feat

        feat_dict = {
            'lang': self.action_instr_pieces[index],
            'frames':self.feature_pieces[index],
            'action':self.lang_target_pieces[index],
        }

        return {"dataset_name": self.name}, feat_dict

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
