using UnityEngine;

[CreateAssetMenu(fileName = "FishDef", menuName = "Game/Fish Def")]
public class FishDef : ScriptableObject
{
    [Header("Identity")]
    public string id;          // internal id, example: "cipura"
    public string displayName; // shown in UI, example: "Cipura"

    [Header("Visual")]
    public Sprite icon;        // you will assign the fish sprite here

    [Header("Economy")]
    public float basePrice;    // reference price per kg
    public int rarity;         // 1 = common, 5 = very rare (optional)
}
