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
public class TrajJson {
    public List<ImageJson> images;
    public PlanJson plan;

    public TrajJson() {
        images = new List<ImageJson>();
        plan = new PlanJson();
    }
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