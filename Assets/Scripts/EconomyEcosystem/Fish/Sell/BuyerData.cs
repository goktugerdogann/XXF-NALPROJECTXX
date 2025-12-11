using System;
using System.Collections.Generic;
using UnityEngine;
using Economy;

[CreateAssetMenu(fileName = "BuyerData", menuName = "Economy/BuyerData")]
public class BuyerData : ScriptableObject
{
    public string id;
    public string displayName;
    public Sprite npcPortrait;

    [Header("Fish rules for this buyer")]
    public List<BuyerFishRule> fishRules = new List<BuyerFishRule>();

    [Header("Bargain config (player sells)")]
    // max total mul for bargain (player sells)
    public float sellMaxTotalMul = 1.4f;

    public BuyerFishRule GetRuleForFish(FishDef fish)
    {
        if (fish == null) return null;

        for (int i = 0; i < fishRules.Count; i++)
        {
            BuyerFishRule rule = fishRules[i];
            if (rule != null && rule.fish == fish)
                return rule;
        }

        return null;
    }
}

[Serializable]
public class BuyerFishRule
{
    public FishDef fish;
    public bool accepts = true;

    [Header("Demand from this buyer (kg)")]
    public int minDemandKg = 5;
    public int maxDemandKg = 30;

    [Header("Price settings")]
    // Base price per kg for this buyer (for this fish)
    public float basePricePerKg = 10.0f;

    // Random multiplier range, for example 0.8 to 1.4
    public float randomMulMin = 0.8f;
    public float randomMulMax = 1.4f;
}
