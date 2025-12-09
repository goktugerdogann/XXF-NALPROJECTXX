using UnityEngine;

namespace Economy
{
    [CreateAssetMenu(fileName = "FishDef", menuName = "Economy/FishDef")]
    public class FishDef : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite icon;

        [Header("Economy")]
        public float basePricePerKg = 10f;

        [Header("Info Texts")]
        [TextArea(2, 4)]
        public string description;
        [TextArea(2, 4)]
        public string bestTownsInfo;
        [TextArea(2, 4)]
        public string tips;
    }
}
