from torch.utils import data
from new.new_rendered import NewRenderedScene, NewRenderedComposite, MAX_ROOM_SIZE, IMG_SIZE, border_offset
import random
import math
import torch
import numpy as np

import json

class NewLocationDataset():
    """
    Dataset for training/testing the "should continue" network
    """
    def __init__(self, data_dir, p_auxiliary=0.7, seed=None, ablation=None):
        self.data_dir = data_dir #"/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/all_room_info_b.json"
        self.seed = seed
        self.ablation = ablation
        self.p_auxiliary = p_auxiliary

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

        #Select a subset of objects randomly. Number of objects is uniformly
        #distributed between [0, total_number_of_objects]
        #Doors and windows do not count here
        object_nodes = scene.object_nodes
        num_objects = random.randint(0, len(object_nodes))

        num_categories = len(scene.categories)
        OUTSIDE = num_categories + 2
        EXISTING = num_categories + 1
        NOTHING = num_categories

        centroids = []
        existing_categories = torch.zeros(num_categories)
        future_categories = torch.zeros(num_categories)
        #Process existing objects
        room_box = composite.room_box
        for i in range(num_objects):
            #Add object to composite
            node = object_nodes[i]
            composite.add_node(node)
            #Add existing centroids
            #xmin, _, ymin, _ = node["bbox_min"]
            #xmax, _, ymax, _ = node["bbox_max"]
            
            node_min_x = (node['new_bbox'][0][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 
            node_max_x = (node['new_bbox'][1][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 

            node_min_z = (node['new_bbox'][0][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 
            node_max_z = (node['new_bbox'][1][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 

            node_min_x = int(node_min_x * IMG_SIZE)
            node_max_x = int(node_max_x *IMG_SIZE)

            node_min_z = int(node_min_z * IMG_SIZE)
            node_max_z = int(node_max_z *IMG_SIZE)


            centroids.append(((node_min_x+node_max_x)/2, (node_min_z+node_max_z)/2, EXISTING))

            category = NewRenderedScene.cat_to_index[node["type"]]
            existing_categories[category] += 1

        inputs = composite.get_composite(ablation=self.ablation)
        size = inputs.shape[1]
        # Process removed objects
        # print("num_objects", num_objects, "len(object_nodes)", len(object_nodes))
        for i in range(num_objects, len(object_nodes)):
            node = object_nodes[i]
            #xmin, _, ymin, _ = node["bbox_min"]
            #xmax, _, ymax, _ = node["bbox_max"]

            node_min_x = (node['new_bbox'][0][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 
            node_max_x = (node['new_bbox'][1][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 

            node_min_z = (node['new_bbox'][0][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 
            node_max_z = (node['new_bbox'][1][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 

            node_min_x = int(node_min_x * IMG_SIZE)
            node_max_x = int(node_max_x *IMG_SIZE)

            node_min_z = int(node_min_z * IMG_SIZE)
            node_max_z = int(node_max_z *IMG_SIZE)

            category = NewRenderedScene.cat_to_index[node["type"]]
            centroids.append(((node_min_x+node_max_x)/2, (node_min_z+node_max_z)/2, category))
            future_categories[category] += 1

        resample = True
        if random.uniform(0,1) > self.p_auxiliary: #Sample an object at random
            x,y,output_centroid = random.choice(centroids)
            x = int(x)
            y = int(y)
        else: #Or sample an auxiliary category
            while resample:
                x, y = random.randint(0,511), random.randint(0,511) #Should probably remove this hardcoded size at somepoint
                good = True
                for (xc, yc, _) in centroids: 
                    #We don't want to sample an empty space that's too close to a centroid
                    #That is, if it can fall within the ground truth attention mask of an object
                    if x-4 < xc < x+5 and y-4 < yc < y+5:
                        good = False 

                if good:
                    if not inputs[0][x][y]:
                        output_centroid = OUTSIDE #Outside of room
                        if random.uniform(0,1) > 0.8: #Just some hardcoded stuff simple outside room is learned very easily
                            resample = False
                    else:
                        output_centroid = NOTHING #Inside of room
                        resample = False

        #Attention mask
        xmin = max(x - 4, 0)
        xmax = min(x + 5, size)
        ymin = max(y - 4, 0)
        ymax = min(y + 5, size)
        inputs[-1, xmin:xmax, ymin:ymax] = 1 #Create attention mask

        #Compute weight for L_Global
        #If some objects in this cateogory are removed, weight is zero
        #Otherwise linearly scaled based on completeness of the room
        #See paper for details
        penalty = torch.zeros(num_categories)
        penalty[future_categories==0] = num_objects/len(object_nodes)

        return inputs, output_centroid, existing_categories, penalty