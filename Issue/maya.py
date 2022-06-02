import math
import json
import math
import os
import sys
from typing import Dict, Union, Callable, Tuple, Optional
import numpy as np

from controller import MayaController

def fix_vertices_and_faces(vs, fs):
    '''
    Blender mesh.from_pydata() -> Maya mesh.Create(OpenMaya)

    More information
        Blender
            https://docs.blender.org/api/current/bpy.types.Mesh.html?highlight=from_pydata#bpy.types.Mesh.from_pydata
        Maya
            https://help.autodesk.com/view/MAYAUL/2017/ENU/?guid=__py_ref_class_open_maya_1_1_m_fn_mesh_html
            https://forums.autodesk.com/t5/maya-programming/create-mesh-from-list/td-p/7575371
            https://forums.cgsociety.org/t/creating-polygon-object-from-scratch-with-pymel/1845044/2
    '''
    v_dict = {}
    new_vs = []
    for v in vs:
        # print(str(v))
        if not str(v) in v_dict:
            v_dict[str(v)] = len(v_dict)
            new_vs.append(v)
    
    new_fs = []
    for f in fs:
        new_f = []
        for f_point in f:
            new_f.append(v_dict[str(vs[f_point])])
        new_fs.append(new_f)
    
    return new_vs, new_fs
 
def euler_from_quaternion(x, y, z, w):
        """
        Convert a quaternion into euler angles (roll, pitch, yaw)
        roll is rotation around x in radians (counterclockwise)
        pitch is rotation around y in radians (counterclockwise)
        yaw is rotation around z in radians (counterclockwise)
        """
        t0 = +2.0 * (w * x + y * z)
        t1 = +1.0 - 2.0 * (x * x + y * y)
        roll_x = math.atan2(t0, t1)
     
        t2 = +2.0 * (w * y - z * x)
        t2 = +1.0 if t2 > +1.0 else t2
        t2 = -1.0 if t2 < -1.0 else t2
        pitch_y = math.asin(t2)
     
        t3 = +2.0 * (w * z + x * y)
        t4 = +1.0 - 2.0 * (y * y + z * z)
        yaw_z = math.atan2(t3, t4)
     
        return roll_x, pitch_y, yaw_z # in radians


def create_instance_table(sceneDict: Dict):
    _ = {'id': [], 'position': [], 'rotation': [], 'scale': [], 'ref': []}
    for room in sceneDict['scene']['room']:
        for child in room['children']:
            _['id'].append(child['instanceid'])
            _['position'].append(child['pos'])
            _['rotation'].append(child['rot'])
            _['scale'].append(child['scale'])
            _['ref'].append(child['ref'])
    return {key: np.array(value) for key, value in _.items()}


def create_mesh_table(sceneDict: Dict):
    _ = {'id': [], 'material_id': [], 'type': [], 'xyz': [], 'normal': [], 'uv': [], 'face': []}
    for index, mesh in enumerate(sceneDict['mesh']):
        _['id'].append(mesh['uid'])
        _['material_id'].append(mesh['material'])
        _['type'].append(mesh['type'])
        _['xyz'].append(np.array(mesh['xyz']).reshape(-1, 3).T.tolist())
        _['normal'].append(np.array(mesh['normal']).reshape(-1, 3).T.tolist())
        _['uv'].append(np.array(mesh['uv']).reshape(-1, 2).T.tolist())
        _['face'].append(np.array(mesh['faces']).reshape(-1, 3).T.tolist())
    return {key: np.array(value) for (key, value) in _.items()}


def create_material_table(sceneDict: Dict, material_when_unavailable: Union[int, Callable] = 0xffffffff):
    def compatible_texture(_material):
        if 'texture' not in _material: return ''
        _texture = _material['texture']
        return _texture['value'] if isinstance(_texture, dict) else _texture

    def compatible_color(rgba_or_rgb_array):
        r, g, b, a = rgba_or_rgb_array if 4 == len(rgba_or_rgb_array) else [*rgba_or_rgb_array, 255]
        return a << 24 | r << 16 | g << 8 | b

    def compatible_color_mode(_material):
        if not bool(_material.get('texture')): return 'color'
        if 'colorMode' in _material: return _material['colorMode']
        if bool(_material.get('useColor')): return 'color'
        return 'texture'

    def compatible_uv_transform(_material):
        return np.array(_material['UVTransform']).reshape(3, 3) if 'UVTransform' in _material else np.eye(3)

    _ = {'id': [], 'texture': [], 'color': [], 'colorMode': [], 'UVTransform': [], }
    for _material in sceneDict['material']:
        _['id'].append(_material['uid'])
        try:
            _['texture'].append(compatible_texture(_material['texture']))
            _['color'].append(compatible_color(_material['color']))
            _['colorMode'].append(compatible_color_mode(_material))
            _['UVTransform'].append(compatible_uv_transform(_material))
        except:
            if isinstance(material_when_unavailable, int):
                _['texture'].append('')
                _['UVTransform'].append(np.eye(3))
                _['color'].append(material_when_unavailable)
                _['colorMode'].append('color')
            else:
                _m = material_when_unavailable(_material)
                _['texture'].append(compatible_texture(_m['texture']))
                _['UVTransform'].append(compatible_uv_transform(_m))
                _['color'].append(compatible_color(_m['color']))
                _['colorMode'].append(compatible_color_mode(_m))
    return {key: np.array(value) for key, value in _.items()}


def create_furniture_table(sceneDict: Dict):
    _ = {'id': [], 'jid': []}
    for furniture in sceneDict['furniture']:
        _['id'].append(furniture['uid'])
        _['jid'].append(furniture['jid'])
    return {key: np.array(value) for key, value in _.items()}


def join(ndarray1Dict, ndarray2Dict, c1, c2, rsuffix):
    leftColumnNames = list(ndarray1Dict.keys())
    rightColumnNames = list(ndarray2Dict.keys())

    def rightName(_name):
        return _name if _name not in leftColumnNames else f'{rsuffix}{_name}'

    columnNames = leftColumnNames + [rightName(name) for name in rightColumnNames]
    dict1KeyIndex = list(ndarray1Dict.keys()).index(c1)
    result = []
    for row1 in zip(*ndarray1Dict.values()):
        indices = (ndarray2Dict[c2] == row1[dict1KeyIndex])
        row_join = [v[indices] for v in ndarray2Dict.values()]
        for row2 in zip(*row_join): result.append([*row1, *row2])
    columns = np.array(result).T.tolist()
    return {name: column for name, column in zip(columnNames, columns)}


def import_mesh(furniture_all, mc:MayaController):
    for index, furniture in enumerate(zip(furniture_all['id'], furniture_all['jid'], furniture_all['position'], furniture_all['rotation'], furniture_all['scale'])):
    #     if index > 10:
    #         break
        
        fid, jid, position, rotation, scale = furniture
        raw_model_path = os.path.join(shapeLocalSource, jid, 'raw_model.obj').replace("\\","/")
        texture_path = os.path.join(shapeLocalSource, jid, 'texture.png').replace("\\","/")
        print(raw_model_path, os.path.exists(raw_model_path))
        print(texture_path, os.path.exists(texture_path))
        print(position)
        
        if  os.path.exists(raw_model_path) and os.path.exists(texture_path):
            fid = fid.split("/")[0]
            #print(fid)
            mc.SendPythonCommand(f"cmds.file('{raw_model_path}', i=True, gr=True, gn='furniture_group', mergeNamespacesOnClash=True, namespace='component_{fid}')")
            mc.SetObjectWorldTransform('furniture_group',position)
            print("rotation: ", euler_from_quaternion(*rotation), rotation)
            mc.SetObjectLocalRotation('furniture_group',np.rad2deg(euler_from_quaternion(*rotation)))
            # mc.SetObjectAttribute("furniture_group", "scaleX", )

            mc.SetObjectAttribute("furniture_group", "scaleX", scale[0])
            mc.SetObjectAttribute("furniture_group", "scaleY", scale[1])
            mc.SetObjectAttribute("furniture_group", "scaleZ", scale[2])
            
            mc.SendPythonCommand(f"cmds.rename('furniture_group', 'furniture_{fid}')")
        