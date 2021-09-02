from torch.utils import data
from new.new_rendered import NewRenderedScene, NewRenderedComposite, MAX_ROOM_SIZE, border_offset, IMG_SIZE
import random
import math
import torch
import numpy as np

import json
import cv2
from scipy import ndimage


def rotate_image(image, angle, center):
  rot_mat = cv2.getRotationMatrix2D(center, angle, 1.0)
  # print("image", image.shape, image)
  result = cv2.warpAffine(image, rot_mat, image.shape[1::-1], flags=cv2.INTER_LINEAR)
  return result

class NewRotationDataset():
    """
    Dataset for training/testing the "should continue" network
    """
    def __init__(self, data_dir, seed=None, ablation=None):
        self.data_dir = data_dir #"/home/ubuntu/research/3D-FRONT-ToolBox/metadata43DSLN/all_room_info_b.json"
        self.seed = seed
        self.ablation = ablation

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

        object_nodes = scene.object_nodes
        #Since we need to at least rotate one object, this differs from location dataset slightly
        num_objects = random.randint(0, len(object_nodes)-1) 
        num_categories = len(scene.categories)

        for i in range(num_objects):
            node = object_nodes[i]
            composite.add_node(node)

        #Select the node we want to rotate
        node = object_nodes[num_objects]
        
        #Just some made up distribution of different cases
        #Focusing on 180 degree, then 90, then others
        ran = random.uniform(0,1)
        if ran < 0.2:
            r = math.pi
            target = 0
        elif ran < 0.4:
            r = math.pi / 2 * random.randint(1,3)
            target = 0
        elif ran < 0.6:
            r = math.pi / 8 * random.randint(1,15)
            target = 0
        else:
            r = 0
            target = 1

        # o = Obj(modelId)
        #Get the transformation matrix from object space to scene space
        # t = RotationDataset.pgen.get_projection(scene.room).to_2d(np.asarray(node["transform"]).reshape(4,4))
        #Since centered already in object space, rotating the object in object space is the easier option
        sin, cos = math.sin(r), math.cos(r)
        # t_rot = np.asarray([[cos, 0, -sin, 0], \
        #                     [0, 1, 0, 0], \
        #                     [sin, 0, cos, 0], \
        #                     [0, 0, 0, 1]])
        # o.transform(np.dot(t_rot,t))
        # Render the rotated view of the object
        # rotated = torch.from_numpy(TopDownView.render_object_full_size(o, composite.size))
        
        # TODO use the actual rotated image rather than the original 
        room_box = composite.room_box
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
        rotated = cv2.rectangle(node_img,(node_min_x,node_min_z),(node_max_x,node_max_z),(255,255,255),-1)
        rotated /= 255.0

        #rotated = ndimage.rotate(rotated, r * 180 / math.pi)
        center = (int(0.5 * (node_max_x+ node_min_x)), int(0.5 * (node_max_z+ node_min_z)))
        rotated = torch.from_numpy(rotate_image(rotated, r / math.pi * 180, center))

        #Calculate the relevant info needed to composite it to the input
        sin, cos = composite.get_transformation(*node["rot"])
        original_r = math.atan2(sin, cos)
        sin = math.sin(original_r + r)
        cos = math.cos(original_r + r)
        category = NewRenderedScene.cat_to_index[node["type"]]
        composite.add_height_map(rotated, category, sin, cos)

        inputs = composite.get_composite(ablation=self.ablation)
        #Add attention channel, which is just the outline of the targeted object
        rotated[rotated>0] = 1
        inputs[-1] = rotated

        return inputs, target