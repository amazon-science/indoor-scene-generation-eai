# generate nlp from high-level desc
import random

action2language = {
    "GotoLocation": ["go to", "find", "walk to"],
    "PickupObject": ["pick up", "take", "carry"],
    "PutObject": ["put", "place"],
    "SliceObject": ["slice", "cut"],
    "CoolObject":["put", "cool"],
    "HeatObject":["heat", "cook"],
    "CleanObject":["clean","wash","rinse"],
    "ToggleObject":["turn on"]
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
}

def generate_language_from_high_pddl(high_pddl:list):
    '''
    Generate natural language from high pddl according to rules
    '''
    language_lines = []
    for plan in high_pddl:
        action = plan["planner_action"]["action"]
        
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
            lan_line += ["in", "the", "fridge"]
        elif action == "HeatObject":
            lan_line += ["in", "the", "microwave"]
        elif action == "GotoLocation":
            if target_object in ["cabinet","fridge"]:
                lan_line += [random.choice(["and open it","and open the {}".format(target_object), 
                "and then, open the {} door".format(target_object)])]
        elif action == "CleanObject":
            lan_line += ["in the sink"]

        language_lines.append(" ".join(lan_line))

    return ". ".join(language_lines) + "."
            
