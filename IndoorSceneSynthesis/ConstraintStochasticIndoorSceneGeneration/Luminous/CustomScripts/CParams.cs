using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CLearningParams
{
    public static Dictionary<(SimObjType, SimObjType), float> PairType2WeightBesides = new Dictionary<(SimObjType, SimObjType), float>()
    {
        {(SimObjType.Desk, SimObjType.Desk), 0.1f},
        {(SimObjType.LightSwitch, SimObjType.Undefined), 0.1f}, //LightSwitch <-> Door(door is undefined sop.....)
    };

    public static Dictionary<(SimObjType, SimObjType), float> PairType2WeightAway = new Dictionary<(SimObjType, SimObjType), float>()
    {
        {(SimObjType.Desk, SimObjType.Bed), 2.0f},
        {(SimObjType.Painting, SimObjType.Window), 2.0f},

    };
}
