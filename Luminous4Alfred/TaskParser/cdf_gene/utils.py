import os
import json
import urllib.request
from .params import sim_obj_name2type, need_open_obj_types

from tqdm.auto import tqdm

def get_target(example, var):
    '''
    returns the object type of a task param
    '''
    return example['pddl_params'][var] if example['pddl_params'][var] is not None else None

def get_targets(example):
    '''
    returns a dictionary of all targets for the task
    '''
    targets = {
        'object': get_target(example, 'object_target'),
        'parent': get_target(example, 'parent_target'),
        'toggle': get_target(example, 'toggle_target'),
        'mrecep': get_target(example, 'mrecep_target'),
    }

    # slice exampleception
    if 'object_sliced' in example['pddl_params'] and example['pddl_params']['object_sliced']:
        targets['object'] += 'Sliced'  # Change, e.g., "Apple" -> "AppleSliced" as pickup target.

    return targets

def get_sliced_script_from_targets(targets):
    if "Sliced" in targets['object']:
            unsliced_obj = targets['object'].replace("Sliced","")
            # TODO wrong knife
            knife_type = "ButterKnife" if targets['object'] == "BreadSliced" else "Knife"
            script = [{
                "action": "GotoLocation",
                "name": "{}_1".format(knife_type)
            },
            {
                "action": "PickupObject",
                "name": "{}_1".format(knife_type)
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(unsliced_obj)
            },
            {
                "action": "SliceObject",
                "name": "{}_1".format(unsliced_obj)
            },
            # drop knife
            # {
            #     "action": "DropHandObject"
            # }
            {
                "action": "GotoLocation",
                "name": "{}_1".format("CounterTop")
            },
            {
                "action": "PutObject",
                "name": "{}_1".format("CounterTop")
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['object'])
            },
            ]
    else:
        script = [{
            "action": "GotoLocation",
            "name": "{}_1".format(targets['object'])
        }]
    
    return script

def get_task_goals_and_script(example):
    targets = get_targets(example)
    if example['task_type'] == "look_at_obj_in_light":
        task_goal = {
                "goal_id": 0,
                "object_states": [
                    {
                        "name": "{}_1".format(targets['toggle']),
                        "isToggled": True
                    },
                    {
                        "name": "{}_1".format(targets['object']),
                        "isPickedUp": True
                    }
                ],
                "object_states_relation": "and"
            }
        
        script = [
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
            "action": "GotoLocation",
            "name": "{}_1".format(targets['toggle'])
            },
            {
                "action": "ToggleObjectOn",
                "name": "{}_1".format(targets['toggle'])
            }]
    elif example['task_type'] == "pick_and_place_simple":
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                }],
                "object_states_relation": "and"
            }
        script = get_sliced_script_from_targets(targets)

        script += [
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
        }]
    elif example['task_type'] == "pick_and_place_with_movable_recep":
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['mrecep'])
                },{
                    "name": "{}_1".format(targets['mrecep']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                }
                ],
                "object_states_relation": "and"
            }

        script = get_sliced_script_from_targets(targets)

        script += [
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['mrecep'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['mrecep'])
            },
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['mrecep'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            } 
        ]
    elif example['task_type'] == "pick_two_obj_and_place":
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                },{
                    "name": "{}_2".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                }
                ],
                "object_states_relation": "and"
            }

        script = get_sliced_script_from_targets(targets)

        script += [
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_2".format(targets['object'])
            },
            {
                "action": "PickupObject",
                "name": "{}_2".format(targets['object'])
            },   
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            },
        ]
    elif example['task_type'] == "pick_cool_then_place_in_recep":
        target_obj_temperature = "Cold"
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                },{
                    "name": "{}_1".format(targets['object']),
                    "ObjectTemperature": target_obj_temperature
                }
                ],
                "object_states_relation": "and"
            }

        script = get_sliced_script_from_targets(targets)

        script += [{
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format("Fridge")
            },
            {
                "action": "CoolObject",
                "name": "{}_1".format(targets['object'])
            },
            # {
            #     "action": "OpenObject",
            #     "name": "{}_1".format("Fridge")
            # },
            # {
            #     "action": "PutObject",
            #     "name": "{}_1".format("Fridge")
            # },
            # # haven't closed frigde, may close and re-open it for sure
            # {
            #     "action": "PickupObject",
            #     "name": "{}_1".format(targets['object'])
            # },   
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            },
        ]
    elif example['task_type'] == "pick_heat_then_place_in_recep":
        target_obj_temperature = "Hot"
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                },{
                    "name": "{}_1".format(targets['object']),
                    "ObjectTemperature": target_obj_temperature
                }
                ],
                "object_states_relation": "and"
            }
        
        script = get_sliced_script_from_targets(targets)

        script += [
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format("Microwave")
            },
            {
                "action": "HeatObject",
                "name": "{}_1".format(targets['object'])
            },
            # {
            #     "action": "OpenObject",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "PutObject",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "CloseObject",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "ToggleObjectOn",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "ToggleObjectOff",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "OpenObject",
            #     "name": "{}_1".format("Microwave")
            # },
            # {
            #     "action": "PickupObject",
            #     "name": "{}_1".format(targets['object'])
            # },   
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            },
        ]
    elif example['task_type'] == "pick_clean_then_place_in_recep":
        task_goal = {
                "goal_id": 0,
                "object_states": [{
                    "name": "{}_1".format(targets['object']),
                    "receptacle_name": "{}_1".format(targets['parent'])
                },{
                    "name": "{}_1".format(targets['object']),
                    "isDirty": False,
                }
                ],
                "object_states_relation": "and"
            }

        script = get_sliced_script_from_targets(targets)
        
        script += [
            {
                "action": "PickupObject",
                "name": "{}_1".format(targets['object'])
            },
            {
                "action": "GotoLocation",
                "name": "{}_1".format("SinkBasin")
            },
            {
                "action": "CleanObject",
                "name": "{}_1".format(targets['object'])
            },
            # {
            #     "action": "PutObject",
            #     "name": "{}_1".format("SinkBasin")
            # },
            # {
            #     "action": "ToggleObjectOn",
            #     "name": "{}_1".format("SinkBasin")
            # },
            # {
            #     "action": "ToggleObjectOff",
            #     "name": "{}_1".format("SinkBasin")
            # },
            # {
            #     "action": "PickupObject",
            #      "name": "{}_1".format(targets['object'])
            # },
            {
                "action": "GotoLocation",
                "name": "{}_1".format(targets['parent'])
            },
            {
                "action": "PutObject",
                "name": "{}_1".format(targets['parent'])
            },
        ]
    else:
        task_goal = None
        script = None
        #raise("no implemeted for task type: {}".format(example['task_type']))
        
    return task_goal, script


def generate_cdf_from_task(task, data_url="https://raw.githubusercontent.com/askforalfred/alfred/master/data/json_2.1.0/"):
    json_path = os.path.join(data_url, "train", task['task'], 'traj_data.json')

    with urllib.request.urlopen(json_path) as url:
        ex = json.loads(url.read().decode())

    cdf = {
        "task_desc": "",
        "scene": {
            "scene_type": [],
            "agent_init": [],
            "init_actions": [],
            "required_objects": []
        },
        "script": [],
        "task_goals": [],
        "attributes":{}
    }
    # basic info
    cdf["task_desc"] = task["task"]
    floor_plan = ex['scene']['floor_plan']
    cdf["attributes"]['floor_plan'] = floor_plan
    floor_plan_id = int(floor_plan.split("Plan")[-1])
    cdf['scene']['scene_type'].append(0 if floor_plan_id < 100 else 1 if floor_plan_id < 300 else 2 if floor_plan_id < 400 \
        else 3)
    
    # if cdf['scene']['scene_type'][0] in [0,1,2]:
    #     if "cool" in task["task"] or "heat" in task["task"] or "clean" in task["task"]:
    #         print("buscar para task:", task)
    
    # set goals and script
    task_goal, script = get_task_goals_and_script(ex)
    cdf["task_goals"].append(task_goal)
    cdf["script"] = script
    
    # get required objects and set up scripts to solve the problem
    current_location = ""
    required_object_names = []
    required_object = None
    for plan in ex['plan']['high_pddl']:
        # print("plan",plan['discrete_action'])
        # print(cdf['scene']['required_objects'])
        # print("required_object_names", required_object_names, required_object, "\n\n")


        required_object = None
        discrete_action = plan['discrete_action']
        if discrete_action["action"] == "GotoLocation":
            target_obj = discrete_action["args"][0]
            current_location = sim_obj_name2type[target_obj] + "_1"

            if target_obj not in required_object_names:
                required_object_names.append(target_obj)
                required_object = {
                    "name":current_location,
                }

        elif discrete_action["action"] in ["PickupObject","SliceObject"]:
            target_obj = discrete_action["args"][0]
            
            if ex['task_type'] == "pick_two_obj_and_place":
                if target_obj not in required_object_names:
                    required_object_names.append(target_obj)
                    required_object = {
                        "name":sim_obj_name2type[target_obj] + "_1",
                        "location": [{
                            current_location: "on",
                        }]
                    }
                elif target_obj in required_object_names:
                    #print(plan)
                    if discrete_action["action"] == "PickupObject" and current_location == sim_obj_name2type[target_obj] + "_1":
                        # wrong current location: find the place of the first object
                        for req_obj in cdf['scene']['required_objects']:
                            if req_obj["name"] == current_location:
                                if "location" in req_obj:
                                    current_location = list(req_obj["location"][0].keys())[0]
                                    break
                                
                        required_object = {
                            "name":sim_obj_name2type[target_obj] + "_2",
                            "location": [{
                                current_location: "on",
                            }]
                        }
                    else:
                        required_object = {
                            "name":sim_obj_name2type[target_obj] + "_2",
                            "location": [{
                                current_location: "on",
                            }]
                        }
                        
            
            elif ex['task_type'] == "pick_and_place_with_movable_recep":
                #print("plan", plan["planner_action"])
                
                if "coordinateReceptacleObjectId" in plan["planner_action"]:
                    recep_obj = plan["planner_action"]["coordinateReceptacleObjectId"][0].lower()
                    # print("??????", recep_obj)
                    if recep_obj not in required_object_names:
                        required_object_names.append(recep_obj)
                        required_object = {
                            "name":sim_obj_name2type[recep_obj] + "_1",
                        }
                        current_location = sim_obj_name2type[recep_obj] + "_1"
                    else:
                        current_location = sim_obj_name2type[recep_obj] + "_1"
                    
                if target_obj in required_object_names:
                    required_object_names.append(target_obj)

                    if required_object != None:
                        cdf['scene']['required_objects'][-1] = required_object
                    else:
                        cdf['scene']['required_objects'].remove(cdf['scene']['required_objects'][-1])

                    required_object = {
                        "name":sim_obj_name2type[target_obj] + "_1",
                        "location": [{
                            current_location: "on",
                        }]
                    }
                    
                else:
                    required_object_names.append(target_obj)
                    required_object = {
                        "name":sim_obj_name2type[target_obj] + "_1",
                        "location": [{
                            current_location: "on",
                        }]
                    }
                
            
            else:
                if target_obj not in required_object_names:
                    required_object_names.append(target_obj)
                    required_object = {
                        "name":sim_obj_name2type[target_obj] + "_1",
                        "location": [{
                            current_location: "on",
                        }]
                    }
            
        else:
            pass

        if required_object is not None:
            cdf['scene']['required_objects'].append(required_object)
            
            # turn off the lamp
            if "Lamp" in required_object["name"]:
                init_action = {
                    "action": "ToggleObjectOff",
                    "name": "{}".format(required_object["name"]),
                    "forceAction": True
                }
                cdf["scene"]["init_actions"].append(init_action)
            
        #print("required_object", required_object, "\n\n")

    #print(cdf)
    
    # parse cdf again and make some neccessary changes
    modify(cdf)

    #object_vocab += required_object_names

    return cdf, ex, required_object_names

def modify(cdf_dict):
    '''
    Modify cdf by implicit rules
    '''
    #{"name": "Box_1", "location": [{"Sofa_1": "on"}]}, {"name": "DeskLamp_1"}]}

    required_objects = cdf_dict["scene"]["required_objects"]
    solving_scripts = cdf_dict["script"]
    task_goals = cdf_dict["task_goals"]

    # if len(required_objects) == 0:
    #     return
    
    # #print(required_objects)

    # # different room cases:
    # if cdf_dict["scene"]["scene_type"][0] in [1]: # living room 
    #     # modify desklamp: put it on a side table
    #     side_table_index = 1
    #     for req_obj in required_objects:
    #         if "SideTable" in req_obj["name"]: 
    #             side_table_index += 1

    #     has_desklamp = False
    #     for req_obj in required_objects:
    #         if "DeskLamp_1" in req_obj["name"]: 
    #             req_obj["name"] = "SideTable_{}".format(side_table_index)
    #             has_desklamp = True
    #             break
    #     if has_desklamp:
    #         new_req_obj = {"name": "DeskLamp_1", "location": [{"SideTable_{}".format(side_table_index): "on"}]}
    #         required_objects.append(new_req_obj)

    #     # rename Shelf: doesn't exist in the new version of ithor
    #     for req_obj in required_objects:
    #         if "Shelf_" in req_obj["name"] or "Drawer_" in req_obj["name"]: 
    #             req_obj["name"] = req_obj["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #         if "location" in req_obj:
    #             #print(req_obj["location"])
    #             key_value  = req_obj["location"][0]
    #             key = list(key_value)[0]
    #             value = key_value[key]
    #             if "Shelf_" in str(key) or "Drawer_" in str(key):
    #                 new_key = str(key).replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #                 req_obj["location"].append({new_key: value})
    #                 req_obj["location"].remove(key_value)
        
    #     # rename shelf for solving script
    #     for script in solving_scripts:
    #          if "Shelf_" in script["name"] or "Drawer_" in script["name"]: 
    #             script["name"] = script["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")

    #     # rename shelf for target goals
    #     for goal in task_goals:
    #         for obj_state in goal["object_states"]:
    #             if "name" in obj_state and ("Shelf_" in obj_state["name"] or "Drawer_" in obj_state["name"]): 
    #                 obj_state["name"] = obj_state["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #             if "receptacle_name" in obj_state and ("Shelf_" in obj_state["receptacle_name"] or "Drawer_" in obj_state["receptacle_name"]): 
    #                 obj_state["receptacle_name"] = obj_state["receptacle_name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
                
    
    # elif cdf_dict["scene"]["scene_type"][0] in [2]: #bedroom

    #     # modify desklamp: put it on a side table
    #     desk_index = 1
    #     # for req_obj in required_objects:
    #     #     if "SideTable" in req_obj["name"]: 
    #     #         desk_index += 1

    #     has_desklamp = False
    #     for req_obj in required_objects:
    #         if "DeskLamp_1" in req_obj["name"]: 
    #             req_obj["name"] = "Desk_{}".format(desk_index)
    #             has_desklamp = True
    #             break
    #     if has_desklamp:
    #         new_req_obj = {"name": "DeskLamp_1", "location": [{"Desk_{}".format(desk_index): "on"}]}
    #         required_objects.append(new_req_obj)

    #     # rename Shelf: doesn't exist in the new version of ithor
    #     for req_obj in required_objects:
    #         if "Shelf_" in req_obj["name"] or "Drawer_" in req_obj["name"]: 
    #             req_obj["name"] = req_obj["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #         if "location" in req_obj:
    #             #print(req_obj["location"])
    #             key_value  = req_obj["location"][0]
    #             key = list(key_value)[0]
    #             value = key_value[key]
    #             if "Shelf_" in str(key) or "Drawer_" in str(key):
    #                 new_key = str(key).replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #                 req_obj["location"].append({new_key: value})
    #                 req_obj["location"].remove(key_value)

    #     # rename shelf for solving script
    #     for script in solving_scripts:
    #          if "Shelf_" in script["name"] or "Drawer_" in script["name"]: 
    #             script["name"] = script["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")

    #     # rename shelf for target goals
    #     # print(task_goals)
    #     for goal in task_goals:
    #         for obj_state in goal["object_states"]:
    #             if "name" in obj_state and ("Shelf_" in obj_state["name"] or "Drawer_" in obj_state["name"]): 
    #                 obj_state["name"] = obj_state["name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")
    #             if "receptacle_name" in obj_state and ("Shelf_" in obj_state["receptacle_name"] or "Drawer_" in obj_state["receptacle_name"]): 
    #                 obj_state["receptacle_name"] = obj_state["receptacle_name"].replace("Shelf_", "SideTable_").replace("Drawer_", "Desk_")

    
    # # Delete Kitchen CounterTop: for default, we already include some countertops in the scene

    # elif cdf_dict["scene"]["scene_type"][0] in [0]: # Kitchen
    #     new_required_objects = []
    #     for req_obj in required_objects: 
    #         req_obj_name = req_obj["name"].split("_")[0]
    #         #print(req_obj_name)
    #         if not ("CounterTop" in req_obj["name"] or  "SinkBasin" in req_obj["name"] or "StoveBurner" in req_obj["name"] or \
    #             "Sink" in req_obj["name"] or "Drawer" in req_obj["name"] or "Cabinet" in req_obj["name"] or "Shelf" in req_obj["name"]):
    #             new_required_objects.append(req_obj)
        
    #     required_objects = new_required_objects
    #     cdf_dict["scene"]["required_objects"] = required_objects
    #     # for req_obj in required_objects: 
    #     #     if "CounterTop" in req_obj["name"] or  "SinkBasin" in req_obj["name"] or "StoveBurner" in req_obj["name"] or \
    #     #         "Sink" in req_obj["name"] or "Drawer" in req_obj["name"] or "Cabinet" in req_obj["name"]:
    #     #         required_objects.remove(req_obj)
        
    #     # rename bottle
    #     for req_obj in required_objects: 
    #         if "Glassbottle" in req_obj["name"]:
    #             req_obj["name"] = req_obj["name"].replace("Glassbottle","Bottle")
    #             #print("req_obj", req_obj["name"])

    #  # Delete Bath tub: for default, we already include some Bathtub in the scene
    # else: # Bathroom 
    #     #print("enter bathroom")
    #     remove_obj_list = ["BathtubBasin", "Cabinet", "CounterTop", "Drawer", "SinkBasin", "Sink", "ToiletPaperHanger", 
    #         "HandTowelHolder", "Shelf", "Bathtub"]
    #     new_required_objects = []
    #     for req_obj in required_objects: 
    #         req_obj_name = req_obj["name"].split("_")[0]
    #         #print(req_obj_name)
    #         if req_obj_name not in remove_obj_list:
    #             new_required_objects.append(req_obj)
        
    #     required_objects = new_required_objects
    #     cdf_dict["scene"]["required_objects"] = required_objects
        
        
    #     has_cart, need_cart = False, False

    #     # modify shelf : put it on a side cart
    #     # modify toilet paper hanger : put it on toilet
    #     has_toilet, need_toilet = False, False
    #     for req_obj in required_objects:
    #         if "Cart" in req_obj["name"]: 
    #             has_cart = True
    #         if "Toilet" in req_obj["name"]: 
    #             has_toilet = True
    #         if "location" in req_obj:
    #             #print(req_obj["location"])
    #             key_value  = req_obj["location"][0]
    #             key = list(key_value)[0]
    #             value = key_value[key]
    #             if "Shelf_" in str(key) or "HandTowelHolder_" in str(key):
    #                 need_cart = True
    #                 new_key = str(key).replace("Shelf_", "Cart_").replace("HandTowelHolder_", "Cart_")
    #                 req_obj["location"].append({new_key: value})
    #                 req_obj["location"].remove(key_value)
    #             if "ToiletPaperHanger_" in str(key):
    #                 need_toilet = True
    #                 new_key = str(key).replace("ToiletPaperHanger_", "Toilet_")
    #                 req_obj["location"].append({new_key: value})
    #                 req_obj["location"].remove(key_value)
        
    #     if (not has_cart) and need_cart:
    #         new_req_obj = {"name": "Cart_1"}
    #         required_objects.insert(0, new_req_obj)
        
    #     if not has_toilet:
    #         new_req_obj = {"name": "Toilet_1"}
    #         required_objects.insert(0, new_req_obj)
    
    # # cdf_dict["scene"]["required_objects"] = required_objects

    # # general cases
    # # remove duplicated objects
    # new_required_objects = []
    # for required_obj in required_objects:
    #     obj_name =  required_obj["name"]
    #     recept_name = list(required_obj["location"][0].keys())[0] if "location" in required_obj else None
    #     if recept_name == None or recept_name != obj_name:
    #         # print("name duplicated loc", required_obj)
    #         new_required_objects.append(required_obj)

    # cdf_dict["scene"]["required_objects"] = new_required_objects

    # modify scripts
    new_high_pddl = []
    for script in solving_scripts:
        if script["action"] == "GotoLocation":
            obj_name = script["name"]
            target_name = obj_name
            for required_obj in cdf_dict["scene"]["required_objects"]:
                if required_obj["name"] == obj_name:
                    if "location" in required_obj:
                        recept_name = list(required_obj["location"][0].keys())[0]
                        target_name = recept_name
                        break
            
            new_high_pddl.append({
                "action": "GotoLocation",
                "name": target_name
            })
        else:
            new_high_pddl.append(script)

    cdf_dict["high_pddl"] = new_high_pddl

def get_cdfs_from_floorplan(floorplan_num:int, splits, use_gt = True, data_type="train"):
    '''
    Get the trajectories for floor plan (e.g. Floorplan301)
    use ground truth? if not, use script for deduction
    '''
    cdf_vocab = []

    for idx in tqdm(range(min(len(splits[data_type]), 100000))):
        task = splits[data_type][idx]
        floorplan = int(task["task"].split("/")[0].split("-")[-1]) 
        if floorplan != floorplan_num: # living room only
            continue
        #try:
        #only genrate the original one
        if task['repeat_idx'] != 0:
            continue

        print(idx, task)
        if use_gt:
            cdf, ex, required_object_names = generate_cdf_from_task_by_gt(task, data_type)
        else:
            cdf, ex, required_object_names = generate_cdf_from_task(task)

        #if cdf['scene']['scene_type'][0] in [2]:
        cdf_vocab.append(cdf)

        # except:
        #     print("wrong task", idx, task)

    return cdf_vocab


def merge_cdfs(cdf_list:list):
    '''
    Merge multiple cdf into one for scene genration
    '''
    assert(len(cdf_list) > 0)

    m_cdf = {
        "task_desc": "",
        "scene": {
            "scene_type": [],
            "agent_init": [],
            "init_actions": [],
            "required_objects": []
        },
        "script": [],
        "task_goals": [],
        "attributes":{}
    }

    # scene type
    m_cdf["scene"]["scene_type"] = cdf_list[0]["scene"]["scene_type"]
    m_cdf["attributes"] = cdf_list[0]["attributes"]

    # required objects
    all_required_objects = []

    conflict_cdf_count = 0

    for i in range(len(cdf_list)):
        r_objects = cdf_list[i]["scene"]["required_objects"]
        for obj in r_objects:
            obj_already_included = False
            conflict_obj = None
            for e_obj in all_required_objects:
                if e_obj["name"] == obj["name"]:
                    obj_already_included = True
                    conflict_obj = e_obj
        
            if not obj_already_included:
                all_required_objects.append(obj)
            
            else:
                # already included 
                # print(obj, conflict_obj)
                if "location" in obj and "location" in conflict_obj:
                    if obj["location"] != conflict_obj["location"]:
                        conflict_cdf_count+=1
        
    #print(conflict_cdf_count, len(cdf_list))

    m_cdf["scene"]["required_objects"] = all_required_objects

    return m_cdf


    

def generate_cdf_from_task_by_gt(task, dataset_type="train", data_url="https://raw.githubusercontent.com/askforalfred/alfred/master/data/json_2.1.0/"):
    '''
    Generate task cdf from ai2thor 2.1.0 ground-truth json data
    task: traj_json
    state: ThorEnv.last_event
    '''
    
    json_path = os.path.join(data_url, dataset_type, task['task'], 'traj_data.json')

    with urllib.request.urlopen(json_path) as url:
        ex = json.loads(url.read().decode())

    cdf = {
        "task_desc": "",
        "scene": {
            "scene_type": [],
            "agent_init": [],
            "init_actions": [],
            "required_objects": []
        },
        "script": [],
        "task_goals": [],
        "attributes":{},
        "pddl_params": None,
    }
    # basic info
    cdf["task_desc"] = task["task"]
    floor_plan = ex['scene']['floor_plan']
    cdf["attributes"]['floor_plan'] = floor_plan
    floor_plan_id = int(floor_plan.split("Plan")[-1])
    cdf['scene']['scene_type'].append(0 if floor_plan_id < 100 else 1 if floor_plan_id < 300 else 2 if floor_plan_id < 400 \
        else 3)
    
    cdf["pddl_params"] = ex["pddl_params"]
    
    # if cdf['scene']['scene_type'][0] in [0,1,2]:
    #     if "cool" in task["task"] or "heat" in task["task"] or "clean" in task["task"]:
    #         print("buscar para task:", task)
    
    # set goals and script
    task_goal, script = get_task_goals_and_script(ex)
    cdf["task_goals"].append(task_goal)
    cdf["script"] = script
    
    # get required objects and set up scripts to solve the problem
    required_object_names = []
    
    if ex["task_type"] == 'pick_and_place_simple':
        how_many_pick_up = 0
        for plan_step in ex["plan"]["high_pddl"]:
            # get object
            if plan_step["planner_action"]["action"] == "PickupObject":
                how_many_pick_up += 1
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_1"

                # get receptacle
                obj_receptacle = None
                # for obj in state.metadata["objects"]:
                #     if obj["objectId"] == obj_id_need_pickup:
                #         if obj["parentReceptacles"] is not None and len(obj["parentReceptacles"]) > 0:
                #             obj_receptacle_id = obj["parentReceptacles"][0]
                #             obj_receptacle = obj_receptacle_id.split("|")[0] + "_1"
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"   
                
                if obj_receptacle != None:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_receptacle,
                    })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })

            # get target receptacle
            elif plan_step["planner_action"]["action"] == "PutObject":
                recept_id = plan_step["planner_action"]["receptacleObjectId"]
                recept = recept_id.split("|")[0] + "_1"
                cdf["scene"]["required_objects"].append({
                        "name":recept,
                    })

    elif ex["task_type"] == 'pick_and_place_with_movable_recep':
        how_many_pick_up = 0
        for plan_step in ex["plan"]["high_pddl"]:
            # get object
            if plan_step["planner_action"]["action"] == "PickupObject":
                how_many_pick_up += 1
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_1"

                # get receptacle
                obj_receptacle = None
                # for obj in state.metadata["objects"]:
                #     if obj["objectId"] == obj_id_need_pickup:
                #         if obj["parentReceptacles"] is not None and len(obj["parentReceptacles"]) > 0:
                #             obj_receptacle_id = obj["parentReceptacles"][0]
                #             obj_receptacle = obj_receptacle_id.split("|")[0] + "_1"
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"   
                
                if obj_receptacle != None:
                    alread_in = False
                    for alread_in_obj in cdf["scene"]["required_objects"]:
                        if alread_in_obj["name"] == obj_receptacle:
                            alread_in = True
                            break
                    
                    if not alread_in:
                        cdf["scene"]["required_objects"].append({
                                "name":obj_receptacle,
                            })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })

            # get target receptacle
            elif plan_step["planner_action"]["action"] == "PutObject":
                hand_obj = plan_step["planner_action"]["coordinateObjectId"][0] + "_1"

                say_hand = False
                for alread_in_obj in cdf["scene"]["required_objects"]:
                    if alread_in_obj["name"] == hand_obj:
                        say_hand = True
                        break
                
                if say_hand:
                    continue

                recept_id = plan_step["planner_action"]["receptacleObjectId"]
                recept = recept_id.split("|")[0] + "_1"
                alread_in = False
                for alread_in_obj in cdf["scene"]["required_objects"]:
                    if alread_in_obj["name"] == recept:
                        alread_in = True
                        break
                
                if not alread_in:
                    cdf["scene"]["required_objects"].append({
                            "name":recept,
                        })

    elif ex["task_type"] == 'pick_two_obj_and_place':
        how_many_pick_up = 0
        obj_receptacle = None
        for plan_step in ex["plan"]["high_pddl"]:
            # get object
            if plan_step["planner_action"]["action"] == "PickupObject" and how_many_pick_up == 0:
                how_many_pick_up += 1
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_1"

                # get receptacle
                # for obj in state.metadata["objects"]:
                #     if obj["objectId"] == obj_id_need_pickup:
                #         if obj["parentReceptacles"] is not None and len(obj["parentReceptacles"]) > 0:
                #             obj_receptacle_id = obj["parentReceptacles"][0]
                #             obj_receptacle = obj_receptacle_id.split("|")[0] + "_1"
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"   
                
                if obj_receptacle != None:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_receptacle,
                    })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })
            elif plan_step["planner_action"]["action"] == "PickupObject" and how_many_pick_up == 1:
                how_many_pick_up += 1
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_2"

                # get receptacle
                obj_receptacle2 = None
                # for obj in state.metadata["objects"]:
                #     if obj["objectId"] == obj_id_need_pickup:
                #         if obj["parentReceptacles"] is not None and len(obj["parentReceptacles"]) > 0:
                #             obj_receptacle_id = obj["parentReceptacles"][0]
                #             obj_receptacle2 = obj_receptacle_id.split("|")[0] + "_1"
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle2 = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"   
                
                if obj_receptacle2 != None:
                    if obj_receptacle2 != obj_receptacle:
                        cdf["scene"]["required_objects"].append({
                            "name":obj_receptacle2,
                        })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle2: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })


            # get target receptacle
            elif plan_step["planner_action"]["action"] == "PutObject" and how_many_pick_up == 1:
                recept_id = plan_step["planner_action"]["receptacleObjectId"]
                recept = recept_id.split("|")[0] + "_1"
                alread_in = False
                for alread_in_obj in cdf["scene"]["required_objects"]:
                    if alread_in_obj["name"] == recept:
                        alread_in = True
                        break
                
                if not alread_in:
                    cdf["scene"]["required_objects"].append({
                            "name":recept,
                        })

    elif ex["task_type"] == 'look_at_obj_in_light':
        for plan_step in ex["plan"]["high_pddl"]:
            # get object
            if plan_step["planner_action"]["action"] == "PickupObject":
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_1"

                # get receptacle
                obj_receptacle = None
                # for obj in state.metadata["objects"]:
                #     if obj["objectId"] == obj_id_need_pickup:
                #         if obj["parentReceptacles"] is not None and len(obj["parentReceptacles"]) > 0:
                #             obj_receptacle_id = obj["parentReceptacles"][0]
                #             obj_receptacle = obj_receptacle_id.split("|")[0] + "_1"
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"   
                
                if obj_receptacle != None:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_receptacle,
                    })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })

            # get target receptacle
            elif plan_step["planner_action"]["action"] == "ToggleObject":
                turnon_name = plan_step["planner_action"]["coordinateObjectId"][0]
                turnon_obj = turnon_name + "_1"
                if turnon_name == "DeskLamp":
                    cdf["scene"]["required_objects"].append({
                            "name":"Desk_1",
                        })
                    cdf["scene"]["required_objects"].append({
                        "name":turnon_obj,
                        "location": [{
                            "Desk_1": "on",
                        }]
                    })
                    
                else:    
                    cdf["scene"]["required_objects"].append({
                            "name":turnon_obj,
                        })

    elif ex["task_type"] in ['pick_cool_then_place_in_recep', "pick_clean_then_place_in_recep", "pick_heat_then_place_in_recep"]:
        how_many_pick_up = 0
        for plan_step in ex["plan"]["high_pddl"]:
            # get object
            if plan_step["planner_action"]["action"] == "PickupObject":
                how_many_pick_up += 1
                obj_id_need_pickup = plan_step["planner_action"]["objectId"]
                obj_need_pickup = obj_id_need_pickup.split("|")[0] + "_1"

                # get receptacle
                obj_receptacle = None
                if "coordinateReceptacleObjectId" in plan_step["planner_action"] and plan_step["planner_action"]["coordinateReceptacleObjectId"] is not None:
                    obj_receptacle = plan_step["planner_action"]["coordinateReceptacleObjectId"][0] + "_1"           
                
                if obj_receptacle != None:
                    alread_in = False
                    for alread_in_obj in cdf["scene"]["required_objects"]:
                        if alread_in_obj["name"] == obj_receptacle:
                            alread_in = True
                            break
                    
                    if not alread_in:
                        cdf["scene"]["required_objects"].append({
                                "name":obj_receptacle,
                            })

                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                        "location": [{
                            obj_receptacle: "on",
                        }]
                    })
                else:
                    cdf["scene"]["required_objects"].append({
                        "name":obj_need_pickup,
                    })

            # get target receptacle
            elif plan_step["planner_action"]["action"] in ["PutObject", "HeatObject"]:
                recept_id = plan_step["planner_action"]["coordinateReceptacleObjectId"]
                recept = recept_id[0] + "_1"

                alread_in = False
                for alread_in_obj in cdf["scene"]["required_objects"]:
                    if alread_in_obj["name"] == recept:
                        alread_in = True
                        break
                
                if not alread_in:
                    cdf["scene"]["required_objects"].append({
                            "name":recept,
                        })

                

    return cdf, ex, required_object_names