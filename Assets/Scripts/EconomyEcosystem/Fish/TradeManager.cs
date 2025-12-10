using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Economy
{
    public class TradeManager : MonoBehaviour
    {
        public static TradeManager Instance { get; private set; }

        [Header("Debug")]
        public bool debugLogs = true;

        [Header("Day System")]
        [Tooltip("Gün indexi. Yeni güne geçince bunu 1 arttýr (AdvanceDay ile).")]
        public int currentDayIndex = 0;

        // Her shop için o güne ait state'i cache'liyoruz
        class CachedShopState
        {
            public int dayIndex;
            public ShopRuntimeState state;
        }

        Dictionary<ShopData, CachedShopState> cachedShops =
            new Dictionary<ShopData, CachedShopState>();

        public ShopRuntimeState CurrentShop { get; private set; }

        public event Action<ShopRuntimeState> OnShopOpened;
        public event Action<NpcResponseType, string> OnNpcResponse;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // Dýþarýdan gün deðiþtirmek için çaðýrýrsýn (þimdilik manuel)
        public void AdvanceDay()
        {
            currentDayIndex++;
            if (debugLogs)
            {
                Debug.Log("Day advanced to " + currentDayIndex);
            }
            // cachedShops'i silmiyoruz; EnterShop içinden güne göre zaten yenilenecek
        }

        // Köylü ile etkileþime girince burasý çaðrýlýyor
        public void EnterShop(ShopData shopData)
        {
            if (shopData == null)
            {
                Debug.LogError("TradeManager.EnterShop called with null ShopData");
                return;
            }

            // ARTIK: her seferinde GenerateRuntimeShop çaðýrmak yerine cache kullanalým
            CurrentShop = GetOrGenerateShopForToday(shopData);

            if (debugLogs)
            {
                Debug.Log("Entered shop: " + shopData.displayName);
            }

            OnShopOpened?.Invoke(CurrentShop);

            // Simple greeting based on mood
            string line = GetGreetingLine(CurrentShop.mood);
            OnNpcResponse?.Invoke(NpcResponseType.Greeting, line);
        }

        // Ayný gün içinde ayný shop'a girersek ayný runtime state'i döndür
        ShopRuntimeState GetOrGenerateShopForToday(ShopData shopData)
        {
            CachedShopState cached;
            if (cachedShops.TryGetValue(shopData, out cached))
            {
                if (cached != null &&
                    cached.state != null &&
                    cached.dayIndex == currentDayIndex)
                {
                    // Ayný gün, cache'teki state'i kullan
                    return cached.state;
                }
            }

            // Yeni gün veya hiç yok yeniden üret
            ShopRuntimeState newState = GenerateRuntimeShop(shopData);

            cached = new CachedShopState
            {
                dayIndex = currentDayIndex,
                state = newState
            };
            cachedShops[shopData] = cached;

            return newState;
        }

        ShopRuntimeState GenerateRuntimeShop(ShopData shopData)
        {
            ShopRuntimeState state = new ShopRuntimeState();
            state.data = shopData;

            // Mood
            state.mood = PickRandomMood(shopData);

            // Fish list
            List<FishDef> pool = new List<FishDef>(shopData.possibleFish);
            int count = Mathf.Clamp(
                Random.Range(shopData.minFishTypes, shopData.maxFishTypes + 1),
                0,
                pool.Count
            );

            for (int i = 0; i < count; i++)
            {
                if (pool.Count == 0) break;

                int index = Random.Range(0, pool.Count);
                FishDef def = pool[index];
                pool.RemoveAt(index);

                RuntimeFishStock runtimeStock = new RuntimeFishStock();
                runtimeStock.fish = def;

                runtimeStock.quantityKg = Random.Range(shopData.minStockKg, shopData.maxStockKg + 1);

                float mul = Random.Range(shopData.minPriceMul, shopData.maxPriceMul);
                mul *= shopData.shopPriceBias;

                // ----- FÝYAT BURADA AYARLANIYOR -----
                // Eski: runtimeStock.unitPricePerKg = def.basePricePerKg * mul;
                float rawPrice = def.basePricePerKg * mul;
                int roundedPrice = Mathf.Max(1, Mathf.RoundToInt(rawPrice)); // tam sayýya çevir
                runtimeStock.unitPricePerKg = roundedPrice;
                // -------------------------------------

                state.currentStock.Add(runtimeStock);
            }

            return state;
        }

        NpcMood PickRandomMood(ShopData data)
        {
            float total =
                data.happyWeight +
                data.neutralWeight +
                data.annoyedWeight +
                data.angryWeight;

            if (total <= 0.01f)
                return NpcMood.Neutral;

            float r = Random.value * total;

            if (r < data.happyWeight) return NpcMood.Happy;
            r -= data.happyWeight;

            if (r < data.neutralWeight) return NpcMood.Neutral;
            r -= data.neutralWeight;

            if (r < data.annoyedWeight) return NpcMood.Annoyed;
            // else
            return NpcMood.Angry;
        }

        string GetGreetingLine(NpcMood mood)
        {
            switch (mood)
            {
                case NpcMood.Happy:
                    return "Welcome, fish are very fresh today.";
                case NpcMood.Annoyed:
                    return "Say what you want, I am busy.";
                case NpcMood.Angry:
                    return "Do not waste my time.";
                default:
                    return "Hello.";
            }
        }

        public BargainResult EvaluateBargain(
            RuntimeFishStock stock,
            int quantityKg,
            float baseTotal,
            float desiredTotal)
        {
            BargainResult result = new BargainResult();
            result.baseTotal = baseTotal;
            result.desiredTotal = desiredTotal;

            if (CurrentShop == null || stock == null || quantityKg <= 0 || baseTotal <= 0.01f)
            {
                result.finalTotal = baseTotal;
                result.responseType = NpcResponseType.Angry;
                result.npcLine = "I do not like this offer.";
                return result;
            }

            // Clamp desiredTotal to slider range (defensive)
            float minAllowed = baseTotal * CurrentShop.data.bargainMinTotalMul;
            float maxAllowed = baseTotal * CurrentShop.data.bargainMaxTotalMul;
            desiredTotal = Mathf.Clamp(desiredTotal, minAllowed, maxAllowed);

            // How hard the player is pushing
            float t = 0f;
            if (Mathf.Abs(baseTotal - desiredTotal) > 0.01f)
            {
                float fullDiscount = baseTotal - minAllowed; // max possible discount
                float requestedDiscount = baseTotal - desiredTotal;
                t = Mathf.Clamp01(requestedDiscount / Mathf.Max(0.01f, fullDiscount));
            }

            // Mood controls how much of this discount is accepted
            float moodFactor = GetMoodDiscountFactor(CurrentShop.mood);

            // finalTotal between baseTotal and desiredTotal
            float lerpFactor = t * moodFactor;

            float finalTotal = Mathf.Lerp(baseTotal, desiredTotal, lerpFactor);

            // Small randomness so it does not feel robotic
            float noise = Random.Range(-0.02f, 0.02f); // +-2 percent
            finalTotal *= 1f + noise;
            finalTotal = Mathf.Clamp(finalTotal, minAllowed, baseTotal);

            result.finalTotal = finalTotal;

            // Response line
            if (Mathf.Approximately(finalTotal, baseTotal))
            {
                result.responseType = NpcResponseType.Angry;
                result.npcLine = "No discount. Take it or leave it.";
            }
            else if (finalTotal <= desiredTotal + 0.01f)
            {
                result.responseType = NpcResponseType.Accept;
                result.npcLine = "Alright, I will give it for " + finalTotal.ToString("0") + ".";
            }
            else
            {
                result.responseType = NpcResponseType.Counter;
                result.npcLine = "I can go down to " + finalTotal.ToString("0") + ". Any lower would make me angry.";
            }

            OnNpcResponse?.Invoke(result.responseType, result.npcLine);
            return result;
        }

        float GetMoodDiscountFactor(NpcMood mood)
        {
            switch (mood)
            {
                case NpcMood.Happy:
                    return 0.9f; // almost full requested discount
                case NpcMood.Neutral:
                    return 0.6f;
                case NpcMood.Annoyed:
                    return 0.3f;
                case NpcMood.Angry:
                    return 0.1f;
                default:
                    return 0.5f;
            }
        }

        public void CompleteTrade(RuntimeFishStock stock, int quantityKg, float totalPaid)
        {
            if (stock == null || quantityKg <= 0)
                return;

            stock.quantityKg -= quantityKg;
            if (stock.quantityKg < 0)
                stock.quantityKg = 0;

            if (debugLogs)
            {
                Debug.Log("Player bought " + quantityKg + " kg of " +
                          stock.fish.displayName + " for total " + totalPaid.ToString("0"));
            }

            OnNpcResponse?.Invoke(NpcResponseType.Accept, "Deal. Pleasure doing business.");
            // TODO: plug player money and inventory here.
        }
    }
}
