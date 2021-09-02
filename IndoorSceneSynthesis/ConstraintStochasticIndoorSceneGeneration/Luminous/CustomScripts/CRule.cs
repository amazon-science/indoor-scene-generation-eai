using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

[System.Serializable]
public enum CRoomType : int {
    Kitchen = 0,
    LivingRoom = 1,
    Bedroom = 2,
    Bathroom = 3
}

[System.Serializable]
public enum CGenerationType : int {
    Furniture = 0,
    Object = 1,
    Decoration = 2,
    NotSure = 3, //both furiniture and object e.g. vase, box, plant
}

[System.Serializable]
public enum CRoomLayoutType : int {
    Square = 0,
    Rectangle = 1,
    TwoRoom = 2,
}

public static class CRoomRestrictions {
    public static Dictionary<SimObjType, List<SimObjType>> FurnitureAvoidFurnitures = new Dictionary<SimObjType, List<SimObjType>>()
    {
        { SimObjType.DiningTable, new List<SimObjType>(){ SimObjType.Sofa, SimObjType.ArmChair} }
    };
    public static Dictionary<string, CRoomLayoutType> RoomName2LayoutType = new Dictionary<string, CRoomLayoutType>()
    {
        { "LivingRoom201",CRoomLayoutType.Rectangle },
        { "LivingRoom202",CRoomLayoutType.Square },
        { "LivingRoom203",CRoomLayoutType.TwoRoom },
        { "LivingRoom204",CRoomLayoutType.TwoRoom },
        { "LivingRoom205",CRoomLayoutType.Square },
        { "LivingRoom206",CRoomLayoutType.Square },
        { "LivingRoom207",CRoomLayoutType.Rectangle },
        { "LivingRoom208",CRoomLayoutType.TwoRoom },
        { "LivingRoom209",CRoomLayoutType.Rectangle },
        { "LivingRoom210",CRoomLayoutType.Square },

        { "LivingRoom211",CRoomLayoutType.TwoRoom },
        { "LivingRoom212",CRoomLayoutType.Square },
        { "LivingRoom213",CRoomLayoutType.Rectangle },
        { "LivingRoom214",CRoomLayoutType.Rectangle },
        { "LivingRoom215",CRoomLayoutType.Square },
        { "LivingRoom216",CRoomLayoutType.Rectangle },
        { "LivingRoom217",CRoomLayoutType.Square },
        { "LivingRoom218",CRoomLayoutType.Rectangle },
        { "LivingRoom219",CRoomLayoutType.Square },
        { "LivingRoom220",CRoomLayoutType.Square },

        { "LivingRoom221",CRoomLayoutType.Square },
        { "LivingRoom222",CRoomLayoutType.Square },
        { "LivingRoom223",CRoomLayoutType.TwoRoom },
        { "LivingRoom224",CRoomLayoutType.TwoRoom },
        { "LivingRoom225",CRoomLayoutType.Square },
        { "LivingRoom226",CRoomLayoutType.Square },
        { "LivingRoom227",CRoomLayoutType.Square },
        { "LivingRoom228",CRoomLayoutType.Square },
        { "LivingRoom229",CRoomLayoutType.Square },
        { "LivingRoom230",CRoomLayoutType.Rectangle },



    };

    public static Dictionary<SimObjType, CGenerationType> RoomObj2GenerationType = new Dictionary<SimObjType, CGenerationType>()
    {
        //{ SimObjType.Desk, CGenerationType.Furniture },
        //{ SimObjType.Bed, CGenerationType.Furniture },
        //{ SimObjType.Chair, CGenerationType.Furniture },
        //{ SimObjType.ShelvingUnit, CGenerationType.Furniture },
        //{ SimObjType.TVStand, CGenerationType.Furniture },


        //{ SimObjType.Book, CGenerationType.Object },
        //{ SimObjType.DeskLamp, CGenerationType.Object },
        //{ SimObjType.Television, CGenerationType.Object },
        //{ SimObjType.Laptop, CGenerationType.Object },


        //{ SimObjType.LightSwitch, CGenerationType.Decoration },

        //
        {SimObjType.Apple,CGenerationType.Object},
        {SimObjType.AppleSliced,CGenerationType.Object},
        {SimObjType.Tomato,CGenerationType.Object},
        {SimObjType.TomatoSliced,CGenerationType.Object},
        {SimObjType.Bread,CGenerationType.Object},
        {SimObjType.BreadSliced,CGenerationType.Object},
        {SimObjType.Sink,CGenerationType.Furniture},
        {SimObjType.Pot,CGenerationType.Object},
        {SimObjType.Pan,CGenerationType.Object},
        {SimObjType.Knife,CGenerationType.Object},
        {SimObjType.Fork,CGenerationType.Object},
        {SimObjType.Spoon,CGenerationType.Object},
        {SimObjType.Bowl,CGenerationType.Object},
        {SimObjType.Toaster,CGenerationType.Object},
        {SimObjType.CoffeeMachine,CGenerationType.Object},
        {SimObjType.Microwave,CGenerationType.Object},
        {SimObjType.StoveBurner,CGenerationType.Furniture},
        {SimObjType.Fridge,CGenerationType.Furniture},
        {SimObjType.Cabinet,CGenerationType.Furniture},
        {SimObjType.Egg,CGenerationType.Object},
        {SimObjType.Chair,CGenerationType.Furniture},
        {SimObjType.Lettuce,CGenerationType.Object},
        {SimObjType.Potato,CGenerationType.Object},
        {SimObjType.Mug,CGenerationType.Object},
        {SimObjType.Plate,CGenerationType.Object},
        {SimObjType.DiningTable,CGenerationType.Furniture},
        {SimObjType.CounterTop,CGenerationType.Furniture},
        {SimObjType.GarbageCan,CGenerationType.Furniture},
        {SimObjType.Omelette,CGenerationType.Object},
        {SimObjType.EggShell,CGenerationType.Object},
        {SimObjType.EggCracked,CGenerationType.Object},
        {SimObjType.StoveKnob,CGenerationType.Object},
        {SimObjType.Container,CGenerationType.Furniture},
        {SimObjType.Cup,CGenerationType.Object},
        {SimObjType.ButterKnife,CGenerationType.Object},
        {SimObjType.PotatoSliced,CGenerationType.Object},
        {SimObjType.MugFilled,CGenerationType.Object},
        {SimObjType.BowlFilled,CGenerationType.Object},
        {SimObjType.Statue,CGenerationType.Object},
        {SimObjType.LettuceSliced,CGenerationType.Object},
        {SimObjType.ContainerFull,CGenerationType.Furniture},
        {SimObjType.BowlDirty,CGenerationType.Object},
        {SimObjType.Sandwich,CGenerationType.Object},
        {SimObjType.Television,CGenerationType.Object},
        {SimObjType.HousePlant,CGenerationType.NotSure},
        {SimObjType.TissueBox,CGenerationType.Object},
        {SimObjType.VacuumCleaner,CGenerationType.NotSure},
        {SimObjType.Painting,CGenerationType.Decoration},
        {SimObjType.WateringCan,CGenerationType.NotSure}, //wateringcan is placed beside plants
        {SimObjType.Laptop,CGenerationType.Object},
        {SimObjType.RemoteControl,CGenerationType.Object},
        {SimObjType.Box,CGenerationType.NotSure},
        {SimObjType.Newspaper,CGenerationType.Object},
        {SimObjType.TissueBoxEmpty,CGenerationType.Object},
        {SimObjType.PaintingHanger,CGenerationType.Furniture},
        {SimObjType.KeyChain,CGenerationType.Object},
        {SimObjType.Dirt,CGenerationType.Object},
        {SimObjType.CellPhone,CGenerationType.Object},
        {SimObjType.CreditCard,CGenerationType.Object},
        {SimObjType.Cloth,CGenerationType.NotSure}, //cloth can be placed on the floor or on another object
        {SimObjType.Candle,CGenerationType.Object},
        {SimObjType.Toilet,CGenerationType.Furniture},
        {SimObjType.Plunger,CGenerationType.Object},
        {SimObjType.Bathtub,CGenerationType.Object},
        {SimObjType.ToiletPaper,CGenerationType.Object},
        {SimObjType.ToiletPaperHanger,CGenerationType.Object},
        {SimObjType.SoapBottle,CGenerationType.Object},
        {SimObjType.SoapBottleFilled,CGenerationType.Object},
        {SimObjType.SoapBar,CGenerationType.Object},
        {SimObjType.ShowerDoor,CGenerationType.Furniture},
        {SimObjType.SprayBottle,CGenerationType.Object},
        {SimObjType.ScrubBrush,CGenerationType.Object},
        {SimObjType.ToiletPaperRoll,CGenerationType.Object},
        {SimObjType.LightSwitch,CGenerationType.Decoration},
        {SimObjType.Bed,CGenerationType.Furniture},
        {SimObjType.Book,CGenerationType.Object},
        {SimObjType.AlarmClock,CGenerationType.Object},
        {SimObjType.SportsEquipment,CGenerationType.Object},
        {SimObjType.Pen,CGenerationType.Object},
        {SimObjType.Pencil,CGenerationType.Object},
        {SimObjType.Blinds,CGenerationType.Object},
        {SimObjType.Mirror,CGenerationType.Decoration},
        {SimObjType.TowelHolder,CGenerationType.Decoration},
        {SimObjType.Towel,CGenerationType.Object},
        {SimObjType.Watch,CGenerationType.Object},
        {SimObjType.MiscTableObject,CGenerationType.Object},
        {SimObjType.ArmChair,CGenerationType.Furniture},
        {SimObjType.BaseballBat,CGenerationType.Furniture},
        {SimObjType.BasketBall,CGenerationType.NotSure},
        {SimObjType.Faucet,CGenerationType.Object},
        {SimObjType.Boots,CGenerationType.Object},
        {SimObjType.Bottle,CGenerationType.Object},
        {SimObjType.DishSponge,CGenerationType.Object},
        {SimObjType.Drawer,CGenerationType.Furniture},
        {SimObjType.FloorLamp,CGenerationType.Furniture},
        {SimObjType.Kettle,CGenerationType.Object},
        {SimObjType.LaundryHamper,CGenerationType.Furniture},
        {SimObjType.LaundryHamperLid,CGenerationType.Furniture},
        {SimObjType.Lighter,CGenerationType.Object},
        {SimObjType.Ottoman,CGenerationType.Furniture},
        {SimObjType.PaintingSmall,CGenerationType.Decoration},
        {SimObjType.PaintingMedium,CGenerationType.Decoration},
        {SimObjType.PaintingLarge,CGenerationType.Decoration},
        {SimObjType.PaintingHangerSmall,CGenerationType.Decoration},
        {SimObjType.PaintingHangerMedium,CGenerationType.Decoration},
        {SimObjType.PaintingHangerLarge,CGenerationType.Decoration},
        {SimObjType.PanLid,CGenerationType.Object},
        {SimObjType.PaperTowelRoll,CGenerationType.Object},
        {SimObjType.PepperShaker,CGenerationType.Object},
        {SimObjType.PotLid,CGenerationType.Object},
        {SimObjType.SaltShaker,CGenerationType.Object},
        {SimObjType.Safe,CGenerationType.Furniture},
        {SimObjType.SmallMirror,CGenerationType.Object},
        {SimObjType.Sofa,CGenerationType.Furniture},
        {SimObjType.SoapContainer,CGenerationType.Object},
        {SimObjType.Spatula,CGenerationType.Object},
        {SimObjType.TeddyBear,CGenerationType.Object},
        {SimObjType.TennisRacket,CGenerationType.NotSure},
        {SimObjType.Tissue,CGenerationType.Object},
        {SimObjType.Vase,CGenerationType.Object},
        {SimObjType.WallMirror,CGenerationType.Decoration},
        {SimObjType.MassObjectSpawner,CGenerationType.Object},
        {SimObjType.MassScale,CGenerationType.Object},
        {SimObjType.Footstool,CGenerationType.Object},
        {SimObjType.Shelf,CGenerationType.Furniture},
        {SimObjType.Dresser,CGenerationType.Furniture},
        {SimObjType.Desk,CGenerationType.Furniture},
        {SimObjType.SideTable,CGenerationType.Furniture},
        {SimObjType.Pillow,CGenerationType.Object},
        {SimObjType.Bench,CGenerationType.Furniture},
        {SimObjType.Cart,CGenerationType.Furniture},
        {SimObjType.ShowerGlass,CGenerationType.Furniture},
        {SimObjType.DeskLamp,CGenerationType.Object},
        {SimObjType.Window,CGenerationType.Furniture},
        {SimObjType.BathtubBasin,CGenerationType.Furniture},
        {SimObjType.SinkBasin,CGenerationType.Furniture},
        {SimObjType.CD,CGenerationType.NotSure},
        {SimObjType.Curtains,CGenerationType.Furniture},
        {SimObjType.Poster,CGenerationType.Furniture},
        {SimObjType.HandTowel,CGenerationType.Object},
        {SimObjType.HandTowelHolder,CGenerationType.Furniture},
        {SimObjType.Ladle,CGenerationType.Furniture},
        {SimObjType.WineBottle,CGenerationType.Furniture},
        {SimObjType.ShowerCurtain,CGenerationType.Furniture},
        {SimObjType.ShowerHead,CGenerationType.Furniture},
        {SimObjType.TVStand,CGenerationType.Furniture},
        {SimObjType.CoffeeTable,CGenerationType.Furniture},
        {SimObjType.ShelvingUnit,CGenerationType.Furniture},
        {SimObjType.AluminumFoil,CGenerationType.Object},
        {SimObjType.DogBed,CGenerationType.Furniture},
        {SimObjType.Dumbbell,CGenerationType.Object},
        {SimObjType.TableTopDecor,CGenerationType.Decoration},
        {SimObjType.RoomDecor,CGenerationType.Decoration},
        {SimObjType.Stool,CGenerationType.Furniture},
        {SimObjType.GarbageBag,CGenerationType.NotSure},
        {SimObjType.Desktop,CGenerationType.Furniture},
        {SimObjType.TargetCircle,CGenerationType.Object},
        {SimObjType.Floor,CGenerationType.Furniture},
        {SimObjType.ScreenFrame,CGenerationType.Object},
        {SimObjType.ScreenSheet,CGenerationType.Object},
    };

    public static Dictionary<SimObjType, float> SOPRotationOffset = new Dictionary<SimObjType, float>()
    {
        { SimObjType.Bed, Mathf.PI},
        //{SimObjType.Microwave,Mathf.PI },
    };

    public static List<SimObjType> DonotMoveObjTypes = new List<SimObjType>() { SimObjType.Microwave, SimObjType.CoffeeMachine };
}


[System.Serializable]
public class CFurnitureItemRule {
    [Header("Property")]
    public string objName;
    public SimObjType objType;
    public bool isObject = false;
    public bool isFurniture = false;
    [Header("Rule")]
    public string borderRule;

    public int numInRoom = 0;
    public SimObjType groupToObjType = SimObjType.Undefined;

    public int placePriority = 0;
    public int roomSection = 0;


    public CFurnitureItemRule() { }
    public CFurnitureItemRule(string objectName, string f_or_o, string brule, string numlr = "0",
        string group2obj = "", string priori = "", string room_sec = "") {
        objName = objectName;
        objType = (SimObjType)System.Enum.Parse(typeof(SimObjType), objName);
        if (f_or_o == "f") {
            isFurniture = true;
        } else if (f_or_o == "o") {
            isObject = true;
        }

        borderRule = brule;

        numInRoom = int.Parse(numlr);

        if (group2obj.Length > 0) {
            groupToObjType = (SimObjType)System.Enum.Parse(typeof(SimObjType), group2obj);
        }

        if (priori.Length > 0) {
            placePriority = int.Parse(priori);
        }

        if (room_sec.Length > 0) {
            roomSection = int.Parse(room_sec);
        }
    }

    public static List<CFurnitureItemRule> GetItemRule(string csvPath) {
        List<CFurnitureItemRule> itemRulesList = new List<CFurnitureItemRule>();
        using (var reader = new StreamReader(csvPath)) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                string[] values = line.Split(',');
                if (values[0] == "Name") //obj name
                {
                    continue; //skip header
                }
                if (values[2].Length == 0) //border rule
                {
                    values[2] = "random";
                }
                if (values[3].Length == 0) //num in room
                {
                    values[3] = "0";
                }
                if (values[4].Length == 0) //group obj
                {
                    values[4] = "";
                }
                if (values[5].Length == 0) //priority
                {
                    values[5] = "";
                }
                if (values[6].Length == 0) //room section in large room
                {
                    values[6] = "0";
                }


                itemRulesList.Add(new CFurnitureItemRule(values[0], values[1], values[2], values[3], values[4], values[5], values[6]));
            }
        }
        if (itemRulesList.Count != 161) {
#if UNITY_EDITOR
            Debug.LogError("Wrong csv file, the count of items should be 161!");
#endif
        }
        return itemRulesList;
    }
}


[System.Serializable]
public class CObjectItemRule {
    [Header("Property")]
    public string objName;
    public SimObjType objType;
    public bool isObject = false;
    public bool isFurniture = false;
    [Header("Rule")]
    public string localLocation;
    public float rho;
    public float theta;

    public SimObjType groupToObjType = SimObjType.Undefined;

    public int faceCenter = 0;
    public string otherInfo;


    public CObjectItemRule() { }
    public CObjectItemRule(string objectName, string f_or_o, string srho, string stheta,
        string group2obj = "", string sfaceCenter = "", string other_info = "") {
        objName = objectName;
        objType = (SimObjType)System.Enum.Parse(typeof(SimObjType), objName);
        if (f_or_o == "f") {
            isFurniture = true;
        } else if (f_or_o == "o") {
            isObject = true;
        }

        if (srho.Length != 0) {
            //Debug.Log("localLocation " + localLocation);
            rho = float.Parse(srho);
        } else {
            rho = UnityEngine.Random.Range(0.1f, 0.2f);
        }

        if (srho.Length != 0) {
            //Debug.Log("localLocation " + localLocation);
            theta = float.Parse(stheta);
        } else {
            theta = UnityEngine.Random.Range(0.4f, 0.6f);
        }

        if (group2obj.Length > 0) {
            groupToObjType = (SimObjType)System.Enum.Parse(typeof(SimObjType), group2obj);
        }

        if (sfaceCenter.Length > 0) {
            faceCenter = int.Parse(sfaceCenter);
        }

        if (other_info.Length > 0) {
            otherInfo = other_info;
        }
    }

    public static List<CObjectItemRule> GetItemRule(string csvPath) {
        List<CObjectItemRule> itemRulesList = new List<CObjectItemRule>();
        using (var reader = new StreamReader(csvPath)) {
            while (!reader.EndOfStream) {
                string line = reader.ReadLine();
                string[] values = line.Split(',');
                if (values[0] == "Name") //obj name
                {
                    continue; //skip header
                }
                if (values[1].Length == 0) //f or o
                {
                    values[1] = "";
                }
                if (values[2].Length == 0) //rho
                {
                    values[2] = "";
                }
                if (values[3].Length == 0) //theta
                {
                    values[3] = "";
                }
                if (values[4].Length == 0) //group obj
                {
                    values[4] = "";
                }
                if (values[5].Length == 0) //face center
                {
                    values[5] = "";
                }
                if (values[6].Length == 0) //other info
                {
                    values[6] = "0";
                }

                itemRulesList.Add(new CObjectItemRule(values[0], values[1], values[2], values[3], values[4], values[5], values[6]));
            }
        }
        return itemRulesList;
    }
}
