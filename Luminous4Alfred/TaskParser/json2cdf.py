#!/usr/bin/env python
# coding: utf-8

# In[ ]:


import os
import json
from pprint import pprint
import urllib.request
from tqdm.auto import tqdm


# In[ ]:


from cdf_gene.params import sim_obj_types, sim_obj_name2type
from cdf_gene.utils import get_task_goals_and_script, modify
from cdf_gene.utils import get_cdfs_from_floorplan, merge_cdfs, generate_cdf_from_task, generate_cdf_from_task_by_gt


# In[ ]:


import argparse

# parser
parser = argparse.ArgumentParser()

parser.add_argument('--train_save_path', type=str, default="")
parser.add_argument('--valid_save_path', type=str, default="")
parser.add_argument('--test_save_path', type=str, default="")

# parse arguments
args = parser.parse_args("")


# In[ ]:


object_vocab = []
cdf_vocab = []


# In[ ]:


# load data from url
split_url = "https://raw.githubusercontent.com/askforalfred/alfred/master/data/splits/oct21.json"
data_url = "https://raw.githubusercontent.com/askforalfred/alfred/master/data/json_2.1.0/"

with urllib.request.urlopen(split_url) as url:
    splits = json.loads(url.read().decode())
    print({k: len(v) for k, v in splits.items()})


# In[ ]:


# debug
debug_ex = []


# # Merge cdf

# In[ ]:


# load data from url
split_url = "https://raw.githubusercontent.com/askforalfred/alfred/master/data/splits/oct21.json"
data_url = "https://raw.githubusercontent.com/askforalfred/alfred/master/data/json_2.1.0/"

with urllib.request.urlopen(split_url) as url:
    splits = json.loads(url.read().decode())
    print({k: len(v) for k, v in splits.items()})


# In[ ]:


# train data
for ROOM_NUM in tqdm(range(1, 31)):    
    cdf_list = get_cdfs_from_floorplan(ROOM_NUM, splits)

    if(len(cdf_list) == 0):
        continue
    
    for cdf_dict in cdf_list:
        modify(cdf_dict)
    
    merged_cdf = merge_cdfs(cdf_list)
    
    # sort
    merged_cdf["scene"]["required_objects"] =  sorted(merged_cdf["scene"]["required_objects"], key = lambda x: 1 if "location" in x.keys() else 0)

#     for i in range(len(cdf_list)):
#         print(i, cdf_list[i],"\n\n")

    # merged_cdf

    # json.dumps(merged_cdf)

    # sorted(cdf_list, key=lambda x: x["task_desc"])

    write_folder_root = args.train_save_path # "/Users/zhayizho/Desktop/ai2thor-3.3.1/unity/Assets/Custom/Json/Floorplans/"

    # merged_cdf["attributes"]["floor_plan"]

    write_directory = os.path.join(write_folder_root, merged_cdf["attributes"]["floor_plan"])

    task_directory = os.path.join(write_directory, "tasks")
    objinfo_directory = os.path.join(write_directory, "objinfo")

    if not os.path.exists(write_directory):
        os.makedirs(write_directory)
    if not os.path.exists(task_directory):
        os.makedirs(task_directory)
    if not os.path.exists(objinfo_directory):
        os.makedirs(objinfo_directory)

    json.dump(merged_cdf, open(write_directory + "/merged.json", "w"), indent=4)

    write_directory + "/merged.json"

    for i in tqdm(range(len(cdf_list))):
        json_name = cdf_list[i]["task_desc"].replace("/","]") + ".json"
        json.dump(cdf_list[i], open(task_directory + "/"+ json_name, "w"), indent = 4)


# In[ ]:


# valid data
for ROOM_NUM in tqdm([10, 219, 308, 424]):    
    cdf_list = get_cdfs_from_floorplan(ROOM_NUM, splits, data_type="valid_unseen")

    if(len(cdf_list) == 0):
        continue
    
    for cdf_dict in cdf_list:
        modify(cdf_dict)
    
    merged_cdf = merge_cdfs(cdf_list)
    
    # sort
    merged_cdf["scene"]["required_objects"] =  sorted(merged_cdf["scene"]["required_objects"], key = lambda x: 1 if "location" in x.keys() else 0)

#     for i in range(len(cdf_list)):
#         print(i, cdf_list[i],"\n\n")

    # merged_cdf

    # json.dumps(merged_cdf)

    # sorted(cdf_list, key=lambda x: x["task_desc"])

    write_folder_root = args.valid_save_path"/Users/zhayizho/Desktop/ai2thor-3.3.1/unity/Assets/Custom/Json/Validations/"

    # merged_cdf["attributes"]["floor_plan"]

    write_directory = os.path.join(write_folder_root, merged_cdf["attributes"]["floor_plan"])

    task_directory = os.path.join(write_directory, "tasks")
    objinfo_directory = os.path.join(write_directory, "objinfo")

    if not os.path.exists(write_directory):
        os.makedirs(write_directory)
    if not os.path.exists(task_directory):
        os.makedirs(task_directory)
    if not os.path.exists(objinfo_directory):
        os.makedirs(objinfo_directory)

    json.dump(merged_cdf, open(write_directory + "/merged.json", "w"), indent=4)

    write_directory + "/merged.json"

    for i in tqdm(range(len(cdf_list))):
        json_name = cdf_list[i]["task_desc"].replace("/","]") + ".json"
        json.dump(cdf_list[i], open(task_directory + "/"+ json_name, "w"), indent = 4)


# In[ ]:


# test data
for ROOM_NUM in tqdm([9, 29, 215, 226, 315, 325, 404, 424, 425]):    
    cdf_list = get_cdfs_from_floorplan(ROOM_NUM, splits, data_type="tests_unseen")

    if(len(cdf_list) == 0):
        continue
    
    for cdf_dict in cdf_list:
        modify(cdf_dict)
    
    merged_cdf = merge_cdfs(cdf_list)
    
    # sort
    merged_cdf["scene"]["required_objects"] =  sorted(merged_cdf["scene"]["required_objects"], key = lambda x: 1 if "location" in x.keys() else 0)

#     for i in range(len(cdf_list)):
#         print(i, cdf_list[i],"\n\n")

    # merged_cdf

    # json.dumps(merged_cdf)

    # sorted(cdf_list, key=lambda x: x["task_desc"])

    write_folder_root = args.test_save_path #"/Users/zhayizho/Desktop/ai2thor-3.3.1/unity/Assets/Custom/Json/Tests/"

    # merged_cdf["attributes"]["floor_plan"]

    write_directory = os.path.join(write_folder_root, merged_cdf["attributes"]["floor_plan"])

    task_directory = os.path.join(write_directory, "tasks")
    objinfo_directory = os.path.join(write_directory, "objinfo")

    if not os.path.exists(write_directory):
        os.makedirs(write_directory)
    if not os.path.exists(task_directory):
        os.makedirs(task_directory)
    if not os.path.exists(objinfo_directory):
        os.makedirs(objinfo_directory)

    json.dump(merged_cdf, open(write_directory + "/merged.json", "w"), indent=4)

    write_directory + "/merged.json"

    for i in tqdm(range(len(cdf_list))):
        json_name = cdf_list[i]["task_desc"].replace("/","]") + ".json"
        json.dump(cdf_list[i], open(task_directory + "/"+ json_name, "w"), indent = 4)


# In[ ]:




