from math import e
import os
import json
from tqdm.auto import tqdm
from vocab import Vocab
import torch

from et_train.language import generate_language_from_high_pddl, generate_task_desc_from_task_name, generate_natural_language_from_plan

from alfred.nn.enc_visual import FeatureExtractor
from alfred.utils.model_util import load_model
from alfred.utils import data_util

import argparse



LANGUAGE_CHOICE =  "Mix" # "Template" # "Natural"
DATASET_ROOT = "valid/validation_test/"

if __name__ == "__main__":
    
    # parser
    parser = argparse.ArgumentParser()

    parser.add_argument('--language_choice', type=str, default="Mix")
    parser.add_argument('--dataset_root', type=str, default="valid/validation_test/")

    # parse arguments
    args = parser.parse_args()
    
    LANGUAGE_CHOICE = args.language_choice
    DATASET_ROOT = args.dataset_root
    
    
    #train_root = "data/lumi_light/train/"s
    train_root = DATASET_ROOT

    vocab = data_util.load_vocab("lmdb_human")
    vocab_in = Vocab(vocab['action_low']._index2word + ["h_" + word for word in vocab['action_high']._index2word])
    vocab_in.name = "lmdb_human_speaker"
    vocab_out = vocab['word']

    extractor = None
    speaker = None
    if LANGUAGE_CHOICE != "Template":
        extractor = FeatureExtractor(
            archi='fasterrcnn',
            device=torch.device("cuda"),
            checkpoint="logs/pretrained/fasterrcnn_model.pth",
            compress_type='4x'
            )
        speaker, _ = load_model('logs/pretrained/speaker_816_best.pth', torch.device("cuda:0"))

    for train_task in tqdm(os.listdir(train_root)):
        trial_folder = os.path.join(train_root, train_task)

        if len(os.listdir(trial_folder)) == 0:
            os.rmdir(trial_folder)
            continue

        for trial in os.listdir(trial_folder):
            json_path = os.path.join(trial_folder, trial, "traj_data.json")
            print(json_path)
            cdf_json = json.load(open(json_path))
            cdf_json['task_path'] = os.path.join(trial_folder, trial)

            ann = {
                    "assignment_id": "",
                    "high_descs": [],
                    "task_desc": "",
                    "votes": [1,1,1]
                }
            
            if LANGUAGE_CHOICE == "Template":
                # templated language generation
                ann["high_descs"] = generate_language_from_high_pddl(cdf_json["plan"]['high_pddl'])
            else:
                # natural language generation
                is_mix = LANGUAGE_CHOICE == "Mix"
                ann["high_descs"] = generate_natural_language_from_plan(cdf_json, extractor, speaker, vocab_in, vocab_out, is_mix)

            ann["task_desc"] = generate_task_desc_from_task_name(json_path.split("/")[-3])
            
            cdf_json["turk_annotations"] = {"anns":[ann]}
            # rename old and write new json
            os.rename(json_path, os.path.join(trial_folder, trial, "~traj_data.json"))
            #write_json_path = json_path
            write_json_path = os.path.join(trial_folder, trial, "traj_data.json")
            
            json.dump(cdf_json, open(write_json_path,"w"), sort_keys=True, indent=4)
            # print(write_json_path)

            # break
        # break
        