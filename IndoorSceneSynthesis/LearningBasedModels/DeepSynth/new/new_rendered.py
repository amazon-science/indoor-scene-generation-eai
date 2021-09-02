from torch.utils import data
import random
import numpy as np
import math
import pickle
import os
import json
import copy
import torch
import utils

import cv2

IMG_SIZE = 512
MAX_ROOM_SIZE = 5
border_offset = 0.5

class NewRenderedScene():
    categories = None
    cat_to_index = None

    def __init__(self, room_info_dict, shuffle=True, seed=None):

        if seed:
            random.seed(seed)

        # load categories
        if NewRenderedScene.categories is None:
            NewRenderedScene.categories = json.load(open("new/valid_types.json"))
            NewRenderedScene.cat_to_index = {cat: i for (i, cat) in enumerate(NewRenderedScene.categories)}
        
        self.room_info_dict = room_info_dict

        self.room = None

        self.object_nodes = []
        self.door_window_nodes = None # no door or window in 3D-FRONT

        # load object/furniture as nodes
        for bject in room_info_dict['valid_objects']:
            self.object_nodes.append(bject)

        # calulate floor image
        self.floor = None
        self.wall = None
        self.get_floor_and_wall_img()

        # shuffle
        if shuffle:
            random.shuffle(self.object_nodes)

    def get_floor_and_wall_img(self):
        node = self.object_nodes[0]
        node_floor = np.array(node['floor'])
        node_floor_min_x = np.min(node_floor[:,0])
        node_floor[:,0] -= node_floor_min_x - border_offset

        node_floor_min_z = np.min(node_floor[:,1])
        node_floor[:,1] -= node_floor_min_z - border_offset

        points = ((node_floor / MAX_ROOM_SIZE)*IMG_SIZE).astype(int)

        wall_img = np.zeros((512,512))
        self.wall = torch.from_numpy(cv2.polylines(wall_img, [points], True, (255,255,255), 10))
        self.wall /= 255.0

        floor_img = np.zeros((512,512))
        self.floor = torch.from_numpy(cv2.fillPoly(floor_img, pts =[points], color=(255,255,255)))
        self.floor /= (255.0 * 10) # floor 0.1

    def create_composite(self):
        """
        Create a initial composite that only contains the floor,
        wall, doors and windows. See RenderedComposite for how
        to add more objects
        """
        r = NewRenderedComposite(NewRenderedScene.categories, self.floor, self.wall, self.room_info_dict['bbox'])
        return r


class NewRenderedComposite():
    """
    Multi-channel top-down composite, used as input to NN
    """
    def __init__(self, categories, floor, wall, room_box):
        #Optional door_window just in case
        self.size = floor.shape[0]

        self.categories = categories
        self.room_box = room_box

        self.room_mask = (floor + wall)
        self.room_mask[self.room_mask != 0] = 1

        self.wall_mask = wall.clone()
        self.wall_mask[self.wall_mask != 0] = 0.5

        self.height_map = torch.max(floor, wall)
        self.cat_map = torch.zeros((len(self.categories),self.size,self.size))

        self.sin_map = torch.zeros((self.size,self.size))
        self.cos_map = torch.zeros((self.size,self.size))

        self.door_map = torch.zeros((self.size, self.size))
        self.window_map = torch.zeros((self.size, self.size))

        # no window and door
        # if door_window_nodes:

    def get_transformation(self, x, y, z, w):
        """
        Bad naming, really just getting the sin and cos of the
        angle of rotation.
        """
        t2 = +2.0 * (w * y - z * x)
        t2 = +1.0 if t2 > +1.0 else t2
        t2 = -1.0 if t2 < -1.0 else t2

        return (t2, np.sqrt(1 - t2**2))

    def add_height_map(self, to_add, category, sin, cos):
        """
        Add a new object to the composite. 
        Height map, category, and angle of rotation are
        all the information required.
        """
        update = to_add>self.height_map
        # print("update", torch.sum(update))
        self.height_map[update] = to_add[update]
        mask = torch.zeros(to_add.size())
        mask[to_add>0] = 0.5
        self.cat_map[category] = self.cat_map[category] + mask
        self.sin_map[update] = (sin + 1) / 2
        self.cos_map[update] = (cos + 1) / 2

    def add_node(self, node):
        """
        Add a new object to the composite.
        Computes the necessary information and calls
        add_height_map
        node:
          {'jid': '8bba3d84-9d53-4dc6-ad3e-dfac6907b177',
        'category_id': 'e7a0801e-7abd-4634-b3bc-8777342d124e',
        'size': {'xLen': 83.64559936523438,
            'yLen': 83.64070129394531,
            'zLen': 84.06220245361328},
        'scale': [1.0, 1.0, 1.0],
        'pos': [0.2328, 1.6166959838867188, 1.8273],
        'rot': [0.0, 0.0, 0.0, 1.0],
        'category': 'Pendant Lamp',
        'floor': [[-1.548, 3.523],
            [1.4037, 3.523],
            [1.4037, -0.1638],
            [-1.548, -0.1638],
            [-1.548, 3.523]],
        'unormalized_bbox': [[-0.1854279968261719,
            1.6166959838867188,
            1.4069889877319335],
            [0.6510279968261719, 2.0348994903564455, 2.247611012268066]],
        'new_bbox': [[0.46162279471959494, 1.6166959838867188, 0.42605755336116236],
            [0.7450038949846435, 0.8139597961425782, 0.6540661311348774]],
        'type': 'lamp',
        'rotation': 0.0},
        """
        room_box = self.room_box
        # print("node['new_bbox']", node['new_bbox'])
        node_min_x = (node['new_bbox'][0][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 
        node_max_x = (node['new_bbox'][1][0] * room_box[0] + border_offset) / MAX_ROOM_SIZE 

        node_min_z = (node['new_bbox'][0][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 
        node_max_z = (node['new_bbox'][1][2] * room_box[2] + border_offset) / MAX_ROOM_SIZE 

        node_min_x = int(node_min_x * IMG_SIZE)
        node_max_x = int(node_max_x *IMG_SIZE)

        node_min_z = int(node_min_z * IMG_SIZE)
        node_max_z = int(node_max_z *IMG_SIZE)

        node_img = np.zeros((512,512))
        to_add = torch.from_numpy(cv2.rectangle(node_img,(node_min_x,node_min_z),(node_max_x,node_max_z),(255,255,255),-1))
        to_add /= 255.0

        # h = node["height_map"]
        category = NewRenderedScene.cat_to_index[node["type"]]
        # xsize, ysize = h.shape
        # xmin = math.floor(node["bbox_min"][0])
        # ymin = math.floor(node["bbox_min"][2])
        # to_add = torch.zeros((self.size, self.size))
        # to_add[xmin:xmin+xsize,ymin:ymin+ysize] = h

        sin, cos = self.get_transformation(*node["rot"])
        self.add_height_map(to_add, category, sin, cos)
    
    def get_cat_map(self):
        return self.cat_map.clone()


    def get_composite(self, num_extra_channels=1, ablation=None):
        """
        Create the actual multi-channel representation.
        Which is a N x img_size x img_size tensor.
        See the paper for more information.
        Current channel order:
            -0: room mask
            -1: wall mask
            -2: object mask
            -3: height map
            -4, 5: sin and cos of the angle of rotation
            -6, 7: single category channel for door and window
            -8~8+C: single category channel for all other categories

        Parameters
        ----------
        num_extra_channels (int, optional): number of extra empty 
            channels at the end. 1 for most tasks, 0 for should continue
        ablation (string or None, optional): if set, return a subset of all
            the channels for ablation study, see the paper for more details
        """
        if ablation is None:
            composite = torch.zeros((len(self.categories)+num_extra_channels+8, self.size, self.size))
            composite[0] = self.room_mask
            composite[1] = self.wall_mask
            composite[2] = self.cat_map.sum(0)
            composite[3] = self.height_map
            composite[4] = self.sin_map
            composite[5] = self.cos_map
            composite[6] = self.door_map
            composite[7] = self.window_map
            for i in range(len(self.categories)):
                composite[i+8] = self.cat_map[i]
        elif ablation == "depth":
            composite = torch.zeros((1+num_extra_channels, self.size, self.size))
            composite[0] = self.height_map
        elif ablation == "basic":
            composite = torch.zeros((6+num_extra_channels, self.size, self.size))
            composite[0] = self.room_mask
            composite[1] = self.wall_mask
            composite[2] = self.cat_map.sum(0)
            composite[3] = self.height_map
            composite[4] = self.sin_map
            composite[5] = self.cos_map
        else:
            raise NotImplementedError

        return composite
