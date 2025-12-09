using System;
using System.Collections.Generic;
using UnityEngine;

namespace Economy
{
    public enum NpcMood
    {
        Happy,
        Neutral,
        Annoyed,
        Angry
    }

    public enum NpcResponseType
    {
        Greeting,
        Accept,
        Counter,
        Angry,
        Ban
    }

    [Serializable]
    public class RuntimeFishStock
    {
        public FishDef fish;
        public int quantityKg;
        public float unitPricePerKg;
    }

    public class ShopRuntimeState
    {
        public ShopData data;
        public NpcMood mood;
        public List<RuntimeFishStock> currentStock = new List<RuntimeFishStock>();
    }

    public struct BargainResult
    {
        public float baseTotal;
        public float desiredTotal;
        public float finalTotal;
        public NpcResponseType responseType;
        public string npcLine;
    }
}
