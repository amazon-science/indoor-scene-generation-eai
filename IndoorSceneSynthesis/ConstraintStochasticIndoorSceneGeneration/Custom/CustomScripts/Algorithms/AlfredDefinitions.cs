using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ImageJson {
    public string image_name;
    public int high_idx;
    public int low_idx;
}

[System.Serializable]
public class DiscreteActionHigh {
    public string action;
    public List<string> args;

    public DiscreteActionHigh() {
        args = new List<string>();
    }
}

[System.Serializable]
public class PlannerActionHigh {
    public string action;
}

[System.Serializable]
public class HighPddl {
    public DiscreteActionHigh discrete_action;
    public PlannerActionHigh planner_action;
    public int high_idx;

    public HighPddl() {
        discrete_action = new DiscreteActionHigh();
        planner_action = new PlannerActionHigh();
    }
}

[System.Serializable]
public class ApiAction {
    public string action;
    public string objectId = "";
    //public bool forceAction;
}

[System.Serializable]
public class DiscreteActionLow {
    public string action;
    public List<string> args;

    public DiscreteActionLow() {
        args = new List<string>();
    }
}

[System.Serializable]
public class LowAction {
    public ApiAction api_action = new ApiAction();
    public DiscreteActionLow discrete_action = new DiscreteActionLow();
    public int high_idx;
}

[System.Serializable]
public class PlanJson {
    public List<HighPddl> high_pddl;
    public List<LowAction> low_actions;

    public PlanJson() {
        this.high_pddl = new List<HighPddl>();
        this.low_actions = new List<LowAction>();
    }
}

[System.Serializable]
public class AgentInitInfo {
    public string action = "TeleportFull";
    public int rotation;
    public int horizon;
    public bool rotateOnTeleport = true;
    public float x;
    public float y;
    public float z;
}


[System.Serializable]
public class VVector3 {
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class OObjectPose {
    public string objectName = "";
    public VVector3 position;
    public VVector3 rotation;

    public OObjectPose() { }
    public OObjectPose(string obj_name) {
        GameObject go = GameObject.Find(obj_name);
        if (go == null) {
            Debug.LogError("No game object " + obj_name + " in current scene");
        }
        objectName = obj_name;
        position = new VVector3();
        position.x = go.transform.position.x;
        position.y = go.transform.position.y;
        position.z = go.transform.position.z;

        rotation = new VVector3();
        rotation.x = go.transform.eulerAngles.x;
        rotation.y = go.transform.eulerAngles.y;
        rotation.z = go.transform.eulerAngles.z;
    }
}


[System.Serializable]
public class SceneInfo {
    public AgentInitInfo init_action;
    public List<OObjectPose> object_poses;
    public string floor_plan = "";

    public SceneInfo() {
        init_action = new AgentInitInfo();
        object_poses = new List<OObjectPose>();
        floor_plan = "";
    }
}

[System.Serializable]
public class PDDL {
    public string mrecep_target = "";
    public bool object_sliced = false;
    public string object_target = "";
    public string parent_target = "";
    public string toggle_target = "";

}

[System.Serializable]
public class TrajJson {
    public List<ImageJson> images;
    public PlanJson plan;
    public SceneInfo scene;
    public PDDL pddl_params;
    public string task_desc = "";

    public TrajJson() {
        images = new List<ImageJson>();
        plan = new PlanJson();
        scene = new SceneInfo();
        pddl_params = new PDDL();
        task_desc = "";

    }
}

public enum AlfredTaskType {
    all,
    look_at_obj_in_light,
    pick_and_place_simple,
    pick_and_place_with_movable_recep,
    pick_clean_then_place_in_recep,
    pick_cool_then_place_in_recep,
    pick_heat_then_place_in_recep,
    pick_two_obj_and_place
}

public enum LowLevelActionType {
    MoveAhead,
    RotateLeft,
    RotateRight,
    LookUp,
    LookDown,
    PickupObject,
    PutObject,
    OpenObject,
    CloseObject,
    ToggleObjectOn,
    ToggleObjectOff,
    SliceObject,
    Done,
}

public enum AlfredForWhatTask {
    ET,
    HITUT,
}


public class AlfredDefinitions {
    public static Dictionary<string, string> ApiActionName2DiscreteActionName = new Dictionary<string, string>() {
        { "MoveAhead","MoveAhead_25" },
        { "LookUp","LookUp_15" },
        { "LookDown","LookDown_15" },
        {"RotateRight","RotateRight_90" },
        {"RotateLeft","RotateLeft_90" },
    };


    public static List<LowLevelActionType> InteractiveActionType = new List<LowLevelActionType>() {
        LowLevelActionType.ToggleObjectOff,
        LowLevelActionType.ToggleObjectOn,
        LowLevelActionType.CloseObject,
        LowLevelActionType.OpenObject,
        LowLevelActionType.PutObject,
        LowLevelActionType.PickupObject,
    };

    public static List<string> NeedOpenObjs = new List<string>() {
        "safa", "microwave", "fridge", "drawer", "cabinet"
    };
}