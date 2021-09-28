using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public static class CVisualizationParams
{
    //3d-sln name 2 aithor sop
    public static Dictionary<string, SimObjType> SLN3D2Sop = new Dictionary<string, SimObjType>()
    {
        { "curtain", SimObjType.Curtains},
        { "shower_curtain", SimObjType.ShowerCurtain},
        { "dresser", SimObjType.Dresser},
        { "counter", SimObjType.CounterTop},
        { "bookshelf", SimObjType.Shelf},
        { "picture", SimObjType.Painting},
        { "mirror", SimObjType.Mirror},
        { "floor_mat", SimObjType.Undefined}, //?????
        { "chair", SimObjType.Chair},
        { "sink", SimObjType.Sink},
        { "desk", SimObjType.Desk},
        { "table", SimObjType.DiningTable},
        { "lamp", SimObjType.FloorLamp}, //!!!!!
        { "door", SimObjType.Undefined}, //??????
        { "clothes", SimObjType.Cloth},
        { "person", SimObjType.Undefined}, //?????

        { "toilet", SimObjType.Toilet}, 
        { "cabinet", SimObjType.Dresser}, //!!!!
        { "floor", SimObjType.Floor},
        { "window", SimObjType.Window},
        { "blinds", SimObjType.Undefined}, //---- has blinds need sop
        { "wall", SimObjType.Undefined}, //?????
        { "pillow", SimObjType.Pillow}, 
        { "whiteboard", SimObjType.Undefined}, //????? 
        { "bathtub", SimObjType.Undefined}, //??????
        { "television", SimObjType.Television},
        { "night_stand", SimObjType.Undefined}, //?????
        { "sofa", SimObjType.Sofa},
        { "refridgerator", SimObjType.Fridge},
        { "bed", SimObjType.Bed},
        { "shelves", SimObjType.Shelf},//?????
        { "wardrobe", SimObjType.Dresser},

    };

    //rotation offsets for object in SUNCG
    public static Dictionary<int, List<SimObjType>> RotationOffset2SuncgSimObjType = new Dictionary<int, List<SimObjType>>()
    {
        {180, new List<SimObjType>(){ SimObjType.Chair} },

        {90, new List<SimObjType>(){ SimObjType.Chair} }
    };

    //SceneFormerType 2 Simobjtypo
    public static Dictionary<string, SimObjType> SceneFormer2Sop = new Dictionary<string, SimObjType>()
    {
        { "window", SimObjType.Undefined}, //????,
        { "stand", SimObjType.TVStand}, //????
        { "wardrobe_cabinet", SimObjType.Cabinet},
        { "door", SimObjType.Undefined}, //????
        { "double_bed", SimObjType.Bed},
        { "dresser", SimObjType.Dresser},
        { "dressing_table", SimObjType.SideTable},


    };

    //3d-sln name 2 aithor sop
    public static Dictionary<string, SimObjType> FRONT3D2Sop = new Dictionary<string, SimObjType>()
    {
        { "chair", SimObjType.Chair},
        { "frame", SimObjType.Undefined},
        { "table", SimObjType.CoffeeTable},
        { "barstool", SimObjType.Stool},
        { "desk", SimObjType.Desk},
        { "nightstand", SimObjType.Dresser},
        { "shelf", SimObjType.SideTable},
        { "stand", SimObjType.TVStand},
        { "lamp", SimObjType.FloorLamp},
        { "cabinet", SimObjType.Cabinet},
        { "armoire", SimObjType.Dresser},
        { "sofa", SimObjType.Sofa},
        { "bed", SimObjType.Bed},
        { "stool", SimObjType.Stool},
        { "armchair", SimObjType.ArmChair},
        { "wardrobe", SimObjType.Dresser},

    };

}
