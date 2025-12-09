using System;
using System.Collections.Generic;
using UnityEngine;

public enum ShopType
{
    Cheap,
    Premium,
    Mixed,
    Wholesale
}

[Serializable]
public class ShopFishSlot
{
    [Header("Fish")]
    public FishDef fish;      // which fish can appear in this shop

    [Header("Quantity Range (kg)")]
    public int minQty = 10;
    public int maxQty = 100;

    [Header("Price Multiplier Range (relative to fish.basePrice)")]
    public float priceMinMul = 0.8f;
    public float priceMaxMul = 1.2f;
}

[CreateAssetMenu(fileName = "ShopData", menuName = "Game/Shop Data")]
public class ShopData : ScriptableObject
{
    [Header("Identity")]
    public string id;            // internal id, example: "Balikci_A_01"
    public string displayName;   // UI name, example: "Ali's Fish Shop"
    public ShopType shopType;

    [Header("Dialogues")]
    public ShopDialogueSet dialogue;

    [Header("Fish Configuration")]
    public List<ShopFishSlot> possibleFish = new List<ShopFishSlot>();
    public int minFishCount = 2; // how many fish types minimum today
    public int maxFishCount = 4; // how many fish types maximum today

    [Header("Negotiation Settings")]
    public int angerLimit = 3;          // how many bad offers before ban
    public float lowOfferThreshold = 0.7f;     // offer < unitPrice * this => too low
    public float counterOfferThreshold = 0.9f; // offer between this and 1.0 => counter
}
