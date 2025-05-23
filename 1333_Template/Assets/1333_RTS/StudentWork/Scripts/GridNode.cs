using System.Collections;
using System.Collections.Generic;
using UnityEngine;



    [System.Serializable]
    public class GridNode
    {
        public string Name; // Grid Index
        public Vector3 WorldPosition;
        public bool Walkable;
        public int Weight;
        public int GCost = int.MaxValue;
        public int FCost = int.MaxValue;
        public int HCost = int.MaxValue;
        public Vector3 Connection;
        public TerrainType TerrainTypes;
    }

