from torch.utils import data
from new.new_rendered import NewRenderedScene, NewRenderedComposite, MAX_ROOM_SIZE
import random
import math
import torch
import numpy as np

import json


class NewShouldContinueDataset():
    """
    Dataset for training/testing the "should continue" network
    """
    def __init__(self, data_dir, complete_prob=0.5, seed=None, ablation=None):
        self.data_dir = data_dir #"/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/all_room_info_b.json"
        self.seed = seed
        self.ablation = ablation
        self.complete_prob = complete_prob

        self.all_room_info = json.load(open(data_dir))

        self.all_room_keys = self.filter_room() #list(self.all_room_info.keys())

    def filter_room(self):
        all_room_keys = []
        for key,room_dict in self.all_room_info.items():
            if room_dict['bbox'][0] < MAX_ROOM_SIZE and room_dict['bbox'][2] < MAX_ROOM_SIZE:
                if len(room_dict['valid_objects']) > 2:
                    if not np.isnan(room_dict['valid_objects'][0]['pos']).any():
                        all_room_keys.append(key)
        
        return all_room_keys

    def __len__(self):
        return len(self.all_room_keys)

    def __getitem__(self, index):
        if self.seed:
            random.seed(self.seed)

        room_info_dict = self.all_room_info[self.all_room_keys[index]]
        scene = NewRenderedScene(room_info_dict)
        composite = scene.create_composite()

        num_categories = len(scene.categories)
        existing_categories = torch.zeros(num_categories)

        # Flip a coin for whether we're going remove objects or treat this as a complete scene
        is_complete = random.random() < self.complete_prob
        if not is_complete:
            # If we decide to remove objects, then remove a random number of them
            num_objects = random.randint(0, len(scene.object_nodes) - 1)
        else:
            num_objects = len(scene.object_nodes)

        for i in range(num_objects):
            node = scene.object_nodes[i]
            composite.add_node(node)
            cat_index = NewRenderedScene.cat_to_index[node["type"]]
            existing_categories[cat_index] += 1

        inputs = composite.get_composite(num_extra_channels=0, ablation=self.ablation)
        # Output is a boolean for "should we continue adding objects?"
        output = not is_complete
        return inputs, output, existing_categories