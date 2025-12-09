using System;
using System.Collections.Generic;
using UnityEngine;

public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance;

    [Header("Shops in this town")]
    public List<ShopData> allShopsInTown = new List<ShopData>();

    [Header("Runtime state (read-only from UI)")]
    public ShopRuntimeState currentShop;

    [Header("Events")]
    // Called when a shop is opened and stock is generated
    public Action<ShopRuntimeState> OnShopOpened;
    // Called when NPC responds to an offer
    public Action<NpcResponseType, string, float> OnNpcResponse;
    // NpcResponseType, selectedDialogueLine, optionalCounterPricePerKg

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Call this when player enters a shop trigger
    public void EnterShop(ShopData shopData)
    {
        if (shopData == null)
        {
            Debug.LogError("TradeManager.EnterShop called with null shopData");
            return;
        }

        currentShop = new ShopRuntimeState();
        currentShop.data = shopData;
        currentShop.angerLevel = 0;
        GenerateStockForToday(currentShop);

        // pick a greet line
        string greet = PickRandomLine(shopData.dialogue != null ? shopData.dialogue.greetLines : null);

        if (OnShopOpened != null)
        {
            OnShopOpened.Invoke(currentShop);
        }

        if (OnNpcResponse != null && !string.IsNullOrEmpty(greet))
        {
            OnNpcResponse.Invoke(NpcResponseType.Accept, greet, 0f);
        }
    }

    // Generate stock for the current day (basic version)
    void GenerateStockForToday(ShopRuntimeState shopState)
    {
        shopState.currentStock.Clear();

        ShopData data = shopState.data;
        if (data == null)
            return;

        if (data.possibleFish == null || data.possibleFish.Count == 0)
            return;

        int minCount = Mathf.Clamp(data.minFishCount, 1, data.possibleFish.Count);
        int maxCount = Mathf.Clamp(data.maxFishCount, minCount, data.possibleFish.Count);
        int fishCount = UnityEngine.Random.Range(minCount, maxCount + 1);

        // create a working list to pick from
        List<ShopFishSlot> pool = new List<ShopFishSlot>(data.possibleFish);

        for (int i = 0; i < fishCount && pool.Count > 0; i++)
        {
            int index = UnityEngine.Random.Range(0, pool.Count);
            ShopFishSlot slot = pool[index];
            pool.RemoveAt(index);

            if (slot.fish == null)
                continue;

            RuntimeFishStock stock = new RuntimeFishStock();
            stock.fish = slot.fish;

            int qty = UnityEngine.Random.Range(slot.minQty, slot.maxQty + 1);
            stock.quantityKg = Mathf.Max(0, qty);

            float mul = UnityEngine.Random.Range(slot.priceMinMul, slot.priceMaxMul);
            if (stock.fish.basePrice <= 0f)
            {
                stock.unitPrice = 1f * mul;
            }
            else
            {
                stock.unitPrice = stock.fish.basePrice * mul;
            }

            shopState.currentStock.Add(stock);
        }
    }

    // UI will call this when player makes an offer for a specific fish
    public void PlayerOffer(RuntimeFishStock targetStock, int offerQuantityKg, float offerPricePerKg)
    {
        if (currentShop == null || currentShop.data == null)
        {
            Debug.LogWarning("PlayerOffer called but no current shop is active.");
            return;
        }

        if (targetStock == null)
        {
            Debug.LogWarning("PlayerOffer called with null targetStock.");
            return;
        }

        if (offerQuantityKg <= 0 || offerPricePerKg <= 0f)
        {
            Debug.LogWarning("PlayerOffer called with invalid quantity or price.");
            return;
        }

        if (offerQuantityKg > targetStock.quantityKg)
        {
            // cannot buy more than available
            string line = "I do not have that much fish.";
            if (OnNpcResponse != null)
            {
                OnNpcResponse.Invoke(NpcResponseType.TooLow, line, 0f);
            }
            return;
        }

        ShopData data = currentShop.data;

        float unitPrice = targetStock.unitPrice;
        float lowThreshold = unitPrice * data.lowOfferThreshold;
        float counterThreshold = unitPrice * data.counterOfferThreshold;

        NpcResponseType responseType;
        string dialogueLine = string.Empty;
        float counterPricePerKg = 0f;

        if (offerPricePerKg >= unitPrice)
        {
            // accept
            responseType = NpcResponseType.Accept;
            dialogueLine = PickRandomLine(data.dialogue != null ? data.dialogue.offerOkLines : null);

            // apply sale
            targetStock.quantityKg -= offerQuantityKg;
            if (targetStock.quantityKg < 0)
                targetStock.quantityKg = 0;
        }
        else if (offerPricePerKg >= counterThreshold)
        {
            // counter offer
            responseType = NpcResponseType.Counter;
            dialogueLine = PickRandomLine(data.dialogue != null ? data.dialogue.counterOfferLines : null);

            // simple example: counter price = average of offer and unit
            counterPricePerKg = (offerPricePerKg + unitPrice) * 0.5f;
        }
        else if (offerPricePerKg >= lowThreshold)
        {
            // low, but not extremely low: angry but not ban
            responseType = NpcResponseType.Angry;
            currentShop.angerLevel += 1;
            dialogueLine = PickRandomLine(data.dialogue != null ? data.dialogue.angryLines : null);
        }
        else
        {
            // very low offer: strong anger
            responseType = NpcResponseType.TooLow;
            currentShop.angerLevel += 2;
            dialogueLine = PickRandomLine(data.dialogue != null ? data.dialogue.offerLowLines : null);
        }

        if (currentShop.angerLevel >= data.angerLimit)
        {
            responseType = NpcResponseType.Ban;
            dialogueLine = PickRandomLine(data.dialogue != null ? data.dialogue.banLines : null);
        }

        if (OnNpcResponse != null)
        {
            OnNpcResponse.Invoke(responseType, dialogueLine, counterPricePerKg);
        }
    }

    // Helper for picking random dialogue line
    string PickRandomLine(List<string> list)
    {
        if (list == null || list.Count == 0)
            return string.Empty;

        int index = UnityEngine.Random.Range(0, list.Count);
        return list[index];
    }
}
