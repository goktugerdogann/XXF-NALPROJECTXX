using System.Collections.Generic;

public enum NpcResponseType
{
    Accept,
    Counter,
    TooLow,
    Angry,
    Ban
}

[System.Serializable]
public class RuntimeFishStock
{
    public FishDef fish;
    public int quantityKg;
    public float unitPrice; // final price per kg for this shop today
}

public class ShopRuntimeState
{
    public ShopData data;
    public List<RuntimeFishStock> currentStock = new List<RuntimeFishStock>();
    public int angerLevel = 0;

    // optional: you can extend this later (lastDayGenerated, etc.)
}
