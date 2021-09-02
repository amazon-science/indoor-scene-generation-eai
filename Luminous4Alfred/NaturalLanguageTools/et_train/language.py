# generate nlp from high-level desc
import random
import os
import torch
from alfred.utils.model_util import load_model
from et_train.custom_dataset import CustomNaturalLanguageDataset
from PIL import Image

action2language = {
    "GotoLocation": ["go to", "find", "walk to"],
    "PickupObject": ["pick up", "take", "carry"],
    "PutObject": ["put", "place"],
    "SliceObject": ["slice", "cut"],
    "CoolObject":["chill", "cool"],
    "HeatObject":["heat", "cook"],
    "CleanObject":["clean","wash","rinse"],
    "ToggleObject":["turn on", "toggle on"]
}

sop2name = {
    "sinkbasin": "sink",
    "stoveburner": "stove",
    "soapbar": "soap bar",
    "desklamp": "desk lamp",
    "sidetable": "side table",
    "tissuebox": "tissue box",
    "coffeetable": "coffee table",
    "butterknife": "butter knife",
    "garbagecan": "garbage can",
    "spraybottle": "spray bottle",
    "floorlamp": "floor lamp",
    "coffeemachine": "coffee machine",
    "bathtubbasin": "bathtub",
    "dishsponge": "dish sponge",
    "alarmclock": "alarm clock",
    "toiletpaper": "toilet paper",
    "toiletpaperhanger": "paper hanger",
    "handtowelholder": "towel holder",
    "handtowel": "towel",
    "wateringcan": "watering can",
    "tennisracket": "tennis racket",
    "glassbottle": "glass bottle",
    "winebottle": "wine bottle",
    "potatosliced": "sliced potato",
    "tomatosliced": "sliced tomato",
    "lettucesliced": "sliced lettuce",
    "applesliced": "sliced apple",
    "breadsliced": "sliced bread",
    "baseballbat": "baseball bat",
    "creditcard": "credit card",
    "remotecontrol":"remote control",
    "keychain": "key chain",
    "diningtable": "dining table",
    "sidetable": "side table",
}

def generate_language_from_high_pddl(high_pddl:list):
    '''
    Generate natural language from high pddl according to rules
    '''
    language_lines = []
    for plan in high_pddl:
        action = plan["planner_action"]["action"]
        
        #modify action name
        if action == "ToggleObjectOn":
            action =  "ToggleObject"

        # last action
        if action == "End":
            break

        if action not in action2language:
            print("new action found: ", action)
            continue
        action_language = random.choice(action2language[action])
        target_object = plan["discrete_action"]["args"][0]

        # rename
        if target_object in sop2name:
            target_object = sop2name[target_object]

        if action in ["PutObject","HeatObject","CoolObject","CleanObject","SliceObject", "ToggleObject"]:
            det_prefix = "the"
        else:
            det_prefix = random.choice(["a", "the"])

        # natural language desc
        lan_line = [action_language, det_prefix, target_object]

        if action == "PutObject":
            media_object = plan["discrete_action"]["args"][1]

            if media_object in sop2name:
                media_object = sop2name[media_object]

            if media_object in ["sink", "fridge", "cabinet"]:
                deter = "in"
            else:
                deter = "on"
            lan_line += [deter,det_prefix,media_object]
        elif action == "CoolObject":
            lan_line += ["in", "the", "fridge"] + ["then", "take", "it", "back", "out"]
        elif action == "HeatObject":
            lan_line += ["in", "the", "microwave"] + ["then", "take", "it", "back", "out"]
        elif action == "GotoLocation":
            if target_object in ["cabinet","fridge"]:
                lan_line += [random.choice(["and open it","and open the {}".format(target_object), 
                "and then, open the {} door".format(target_object)])]
        elif action == "CleanObject":
            lan_line += ["in the sink"]

        language_lines.append(" ".join(lan_line) + ".")

    return language_lines
            

def generate_task_desc_from_task_name(task_name:str):
    '''
    Generate task description from task name
    '''
    task_type, main_obj, media_obj, target_obj, room_num = task_name.lower().split("-")
    
    if main_obj in sop2name:
        main_obj = sop2name[main_obj]
    if media_obj in sop2name:
        media_obj = sop2name[media_obj]
    if target_obj in sop2name:
        target_obj = sop2name[target_obj]

    task_desc = ""
    if task_type == "pick_and_place_simple":
        task_desc += "pick up the {} and place it on the {}".format(main_obj, target_obj)
    elif task_type == "pick_and_place_with_movable_recep":
        task_desc += "Place the {} with {} in it on the {}".format(media_obj, main_obj, target_obj)
    elif task_type == "pick_two_obj_and_place":
        task_desc += "Put two {} on the {}".format(main_obj, target_obj)
    elif task_type == "look_at_obj_in_light":
        task_desc += "Look at {} under the light".format(main_obj)
    elif task_type == "pick_cool_then_place_in_recep":
         task_desc += "Cool the {} and place it on the {}".format(main_obj, target_obj)
    elif task_type == "pick_clean_then_place_in_recep":
         task_desc += "Clean the {} and place it on the {}".format(main_obj, target_obj)
    elif task_type == "pick_heat_then_place_in_recep":
         task_desc += "Heat the {} and place it on the {}".format(main_obj, target_obj)
    
    return task_desc


def generate_natural_language_from_plan(traj_json, extractor, speaker, vocab_in, vocab_out, mix_templete=False):
    '''
    Generate natural language from high pddl according by model
    '''
    # basic property
    task_path = traj_json['task_path']

    # careful, for luminous, images num must equal to low_action num
    assert(len(traj_json['images']) == len(traj_json['plan']['low_actions']))

    # record
    language_lines = []
    low_action_seq = []
    image_seq = []
    high_instr = None
    current_high_idx = 0
    for i in range(len(traj_json['plan']['low_actions']) + 1):
        if i < len(traj_json['plan']['low_actions']):
            low_action = traj_json['plan']['low_actions'][i]
            high_action_idx = low_action['high_idx']
            image = Image.open(os.path.join(task_path + "/raw_images/", traj_json['images'][i]['image_name'])) 

        if high_action_idx > current_high_idx or i == len(traj_json['plan']['low_actions']):
        # if high_action_idx > current_high_idx:
            current_high_idx = high_action_idx

            # high instruction
            high_action = traj_json['plan']['high_pddl'][high_action_idx]['discrete_action']
            prev_high_action = traj_json['plan']['high_pddl'][high_action_idx - 1]['discrete_action']
            # print(high_action)
            high_instr = [high_action['action']] + high_action['args']
            
            # fix high instr errors
            high_instr = [word if word != "ToggleObjectOn" else "ToggleObject" for word in high_instr]
            
            # encode high instr
            high_instr = [vocab_in.word2index("h_" + word) for word in high_instr]

            # low instruction
            low_instr = [vocab_in.word2index(word) for word in low_action_seq]
            
            action_instr_piece =  high_instr + low_instr
            action_instr_tensor = torch.tensor([action_instr_piece]).to(torch.device("cuda"))
            
            frames = extractor.featurize(image_seq, batch=4)
            
            input_dict = {}
            input_dict['lang'] = action_instr_tensor
            input_dict['frames'] = frames.unsqueeze(0).to(torch.device("cuda"))
            input_dict['lengths_lang'] = torch.tensor([input_dict['lang'].size(1)]).to(torch.device("cuda:0"))
            input_dict['lengths_frames'] = torch.tensor([input_dict['frames'].size(1)]).to(torch.device("cuda:0"))
            input_dict['length_lang_max'] = max(input_dict['lengths_lang'])
            input_dict['length_frames_max'] = max(input_dict['lengths_frames'])
            
            predict_nl = speaker.model.translate(vocab_in, **input_dict)
            predict_words = vocab_out.index2word(predict_nl[0])
            
            if mix_templete:
                if prev_high_action['action'] != "GotoLocation":
                    line = generate_language_from_high_pddl([traj_json['plan']['high_pddl'][high_action_idx - 1]])[0] 
                else:
                    line = " ".join(predict_words[:-1])
            else:
                line = " ".join(predict_words[:-1])
            
            print(prev_high_action['action'], line)
            language_lines.append(line)
            
            low_action_seq.clear()
            image_seq.clear()
            
        if i < len(traj_json['plan']['low_actions']):
            low_action_seq.append(low_action["discrete_action"]["action"])
            image_seq.append(image)

    return language_lines
