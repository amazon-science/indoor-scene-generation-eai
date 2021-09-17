import numpy as np
import revtok
import os
from tqdm import tqdm
 
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

def remove_spaces(s):
    cs = ' '.join(s.split())
    return cs

def remove_spaces_and_lower(s):
    cs = remove_spaces(s)
    cs = cs.lower()
    return cs

def get_objects_with_name_and_prop(name, prop, metadata):
    return [obj for obj in metadata['objects']
            if name in obj['objectId'] and obj[prop]]

# record movable and pickupable
def record_movable_pickupable_transform(metadata, object_cdf_info, excude_name_list= []):    
    obj_has_record = [obj["objectName"] for obj in object_cdf_info]
    record = []
    for obj in metadata["objects"]:
        obj_info = {}
        if obj["pickupable"] or obj["moveable"]:
            if obj["name"] not in excude_name_list:
                if obj["name"] in obj_has_record:
                    object_cdf = object_cdf_info[obj_has_record.index(obj["name"])]
                    obj_info["objectName"] = object_cdf["objectName"]
                    obj_info["rotation"] = object_cdf["rotation"]
                    obj_info["position"] = object_cdf["position"]
                else:
                    obj_info["objectName"] = obj["name"]
                    obj_info["rotation"] = obj["rotation"]
                    obj_info["position"] = obj["position"]

                record.append(obj_info)

    return record

    # get task finishing conditions
def get_goal_conditions_meet(task_type:str, pddl, state, env=None):

    targets = {
        'object': pddl['object_target'],
        'parent': pddl['parent_target'],
        'toggle': pddl['toggle_target'],
        'mrecep': pddl['mrecep_target'],
    }

    if task_type == "look_at_obj_in_light":
        ts = 2
        s = 0

        toggleables = get_objects_with_name_and_prop(
            pddl['toggle_target'], 'toggleable', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            pddl['object_target'], 'pickupable', state.metadata)
        inventory_objects = state.metadata['inventoryObjects']

        # check if object needs to be sliced
        if 'Sliced' in pddl['object_target']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        # check if the right object is in hand
        if len(inventory_objects) > 0 and inventory_objects[0]['objectId'] in [p['objectId'] for p in pickupables]:
            s += 1
        # check if the lamp is visible and turned on
        if np.any([t['isToggled'] and t['visible'] for t in toggleables]):
            s += 1

        return s, ts
    elif task_type == "pick_and_place_simple":
        ts = 1
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)

        # check if object needs to be sliced
        if 'Sliced' in targets['object']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        if np.any([np.any([p['objectId'] in r['receptacleObjectIds']
                           for r in receptacles if r['receptacleObjectIds'] is not None])
                   for p in pickupables]):
            s += 1

        return s, ts
    elif task_type == "pick_and_place_with_movable_recep":
        ts = 3
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)
        movables = get_objects_with_name_and_prop(
            targets['mrecep'], 'pickupable', state.metadata)

        # check if object needs to be sliced
        if 'Sliced' in targets['object']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        pickup_in_place = [p for p in pickupables for m in movables
                           if 'receptacleObjectIds' in p and m['receptacleObjectIds'] is not None
                           and p['objectId'] in m['receptacleObjectIds']]
        movable_in_place = [m for m in movables for r in receptacles
                            if 'receptacleObjectIds' in r and r['receptacleObjectIds'] is not None
                            and m['objectId'] in r['receptacleObjectIds']]
        # check if the object is in the final receptacle
        if len(pickup_in_place) > 0:
            s += 1
        # check if the movable receptacle is in the final receptacle
        if len(movable_in_place) > 0:
            s += 1
        # check if both the object and movable receptacle stack is in the final receptacle
        if np.any([np.any([p['objectId'] in m['receptacleObjectIds'] for p in pickupables]) and
                   np.any([r['objectId'] in m['parentReceptacles'] for r in receptacles]) for m in movables
                   if m['parentReceptacles'] is not None and m['receptacleObjectIds'] is not None]):
            s += 1

        return s, ts
    elif task_type == "pick_clean_then_place_in_recep":
        ts = 3
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)

        if 'Sliced' in targets['object']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        objs_in_place = [p['objectId'] for p in pickupables for r in receptacles
                         if r['receptacleObjectIds'] is not None and p['objectId'] in r['receptacleObjectIds']]
        objs_cleaned = [p['objectId'] for p in pickupables if p['objectId'] in env.cleaned_objects]

        # check if object is in the receptacle
        if len(objs_in_place) > 0:
            s += 1
        # check if some object was cleaned
        if len(objs_cleaned) > 0:
            s += 1
        # check if the object is both in the receptacle and clean
        if np.any([obj_id in objs_cleaned for obj_id in objs_in_place]):
            s += 1

        return s, ts
    elif task_type == "pick_heat_then_place_in_recep":
        ts = 3
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)

        # check if object needs to be sliced
        if 'Sliced' in targets['object']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        objs_in_place = [p['objectId'] for p in pickupables for r in receptacles
                         if r['receptacleObjectIds'] is not None and p['objectId'] in r['receptacleObjectIds']]
        objs_heated = [p['objectId'] for p in pickupables if p['objectId'] in env.heated_objects]

        # check if object is in the receptacle
        if len(objs_in_place) > 0:
            s += 1
        # check if some object was heated
        if len(objs_heated) > 0:
            s += 1
        # check if the object is both in the receptacle and hot
        if np.any([obj_id in objs_heated for obj_id in objs_in_place]):
            s += 1

        return s, ts
    elif task_type == "pick_cool_then_place_in_recep":
        ts = 3
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)

        if 'Sliced' in targets['object']:
            ts += 1
            if len([p for p in pickupables if 'Sliced' in p['objectId']]) >= 1:
                s += 1

        objs_in_place = [p['objectId'] for p in pickupables for r in receptacles
                         if r['receptacleObjectIds'] is not None and p['objectId'] in r['receptacleObjectIds']]
        objs_cooled = [p['objectId'] for p in pickupables if p['objectId'] in env.cooled_objects]

        # check if object is in the receptacle
        if len(objs_in_place) > 0:
            s += 1
        # check if some object was cooled
        if len(objs_cooled) > 0:
            s += 1
        # check if the object is both in the receptacle and cold
        if np.any([obj_id in objs_cooled for obj_id in objs_in_place]):
            s += 1

        return s, ts
    else: # pick_two
        ts = 2
        s = 0

        receptacles = get_objects_with_name_and_prop(
            targets['parent'], 'receptacle', state.metadata)
        pickupables = get_objects_with_name_and_prop(
            targets['object'], 'pickupable', state.metadata)

        # check if object needs to be sliced
        if 'Sliced' in targets['object']:
            ts += 2
            s += min(len([p for p in pickupables if 'Sliced' in p['objectId']]), 2)

        # placing each object counts as a goal_condition
        s += min(np.max([sum([1 if r['receptacleObjectIds'] is not None
                                   and p['objectId'] in r['receptacleObjectIds'] else 0
                              for p in pickupables])
                         for r in receptacles]), 2)
        return s, ts


# get language instructions
def get_lang_instr(traj_data):
    goal_ann = traj_data['turk_annotations']['anns'][0]['task_desc']
    instr_anns = traj_data['turk_annotations']['anns'][0]['high_descs']

    # tokenize annotations
    goal_ann = revtok.tokenize(remove_spaces_and_lower(goal_ann))
    instr_anns = [revtok.tokenize(remove_spaces_and_lower(instr_ann))
                for instr_ann in instr_anns]
    # this might be not needed
    goal_ann = [w.strip().lower() for w in goal_ann]
    instr_anns = [[w.strip().lower() for w in instr_ann]
                for instr_ann in instr_anns]

    lang_instr = {
            'goal': goal_ann + ['<<goal>>'],
            'instr': [instr_ann + ['<<instr>>'] for instr_ann in instr_anns] + [['<<stop>>']],
            'repeat_idx': 0
        } 

    lang_instr = lang_instr['goal'] + [item for sublist in lang_instr['instr'] for item in sublist]
    return lang_instr

def setup_scene(traj_data, env):
    # set up scene
    # env.reset("v308_1")

    # objpose
    object_poses = traj_data['scene']['object_poses']
    mp_record = record_movable_pickupable_transform(env.last_event.metadata, object_poses)
    env.step((dict(action='SetObjectPoses', objectPoses=mp_record)))

    # if look
    if "floor_plan" in traj_data['scene'] and traj_data['scene']['floor_plan'] == "Bedroom" and "look_at_obj_in_light" in traj_data["task_desc"]:
        object_toggles = [{'isOn': False, 'objectType': 'DeskLamp'}]
        env.step((dict(action='SetObjectToggles', objectToggles=object_toggles)))
    
    # if look
    if "floor_plan" in traj_data['scene'] and traj_data['scene']['floor_plan'] == "Livingroom" and "look_at_obj_in_light" in traj_data["task_desc"]:
        object_toggles = [{'isOn': False, 'objectType': 'FloorLamp'}]
        env.step((dict(action='SetObjectToggles', objectToggles=object_toggles)))

    # dirty and empty
    if "clean" in traj_data["task_desc"]:
        env.step(dict(action='SetStateOfAllObjects',
                            StateChange="CanBeDirty",
                            forceAction=True))
        env.step(dict(action='SetStateOfAllObjects',
                            StateChange="CanBeFilled",
                            forceAction=False)) 

    # agent
    env.step(dict(traj_data['scene']['init_action']))
