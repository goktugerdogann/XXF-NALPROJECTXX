using System.Collections.Generic;
using UnityEngine;

namespace Economy
{
    [CreateAssetMenu(fileName = "ShopData", menuName = "Economy/ShopData")]
    public class ShopData : ScriptableObject
    {
        public string id;
        public string displayName;

        [Header("Fish pool for this shop")]
        public List<FishDef> possibleFish = new List<FishDef>();

        [Header("How many fish types per day")]
        public int minFishTypes = 2;
        public int maxFishTypes = 5;

        [Header("Stock range per fish (kg)")]
        public int minStockKg = 10;
        public int maxStockKg = 80;

        [Header("Price random range (per fish)")]
        public float minPriceMul = 0.7f;
        public float maxPriceMul = 1.3f;

        [Header("Price bias for this shop (applied to all fish)")]
        public float shopPriceBias = 1.0f;

        [Header("Bargain slider config (multiplier of total)")]
        public float bargainMinTotalMul = 0.6f; // 60 percent of base
        public float bargainMaxTotalMul = 1.0f; // 100 percent of base

        [Header("Npc mood weights for this shop")]
        public float happyWeight = 1f;
        public float neutralWeight = 2f;
        public float annoyedWeight = 1f;
        public float angryWeight = 0.5f;

        [Header("Npc visuals")]
        public Sprite npcPortrait;

    }
}
