using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public class CFurniturePool : MonoBehaviour
{
    public int randomSeed;
    [Header("Room Type")]
    public CRoomType roomType;

    [HideInInspector]
    public CRoomLayoutType roomLayoutType;
  
    [SerializeField]
    public List<CFurnitureItemRule> furnitureItemRules;
    [Header("Furniture Pool")]
    public List<GameObject> furnitureList = new List<GameObject>();
    public List<GameObject> kitchenTableList = new List<GameObject>();
    [Header("recepter")]
    public SimObjPhysics floorPhysics;
    //Geometry
    public List<Vector2> cornrerList;
    public List<Vector2> borderList;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

}
