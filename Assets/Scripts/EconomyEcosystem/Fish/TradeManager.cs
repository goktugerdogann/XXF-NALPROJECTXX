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

        // current in-use shop for UI
        public ShopRuntimeState CurrentShop { get; private set; }

        // day system
        public int currentDay = 0;

        // one runtime state per shop per day
        Dictionary<ShopData, ShopRuntimeState> shopCache =
            new Dictionary<ShopData, ShopRuntimeState>();

        public event Action<ShopRuntimeState> OnShopOpened;
        public event Action<NpcResponseType, string> OnNpcResponse;

        // greeting lines
        string[] happyGreetLines =
        {
            "Welcome, fish are very fresh today.",
            "Good to see you, the sea was kind today.",
            "You came at the right time, I have great fish.",
            "Ah, my favorite customer. Fresh catch just arrived."
        };

        string[] neutralGreetLines =
        {
            "Hello.",
            "Yes? What do you need?",
            "Take a look, maybe you will find something.",
            "The day is calm, like the sea."
        };

        string[] annoyedGreetLines =
        {
            "Say what you want, I am busy.",
            "Do not waste my time.",
            "If you are not buying, do not stand in the way.",
            "Hurry up, I do not have all day."
        };

        string[] angryGreetLines =
        {
            "Do not waste my time.",
            "If you are here to haggle too much, leave.",
            "I am not in the mood. Speak quickly.",
            "You again? This better be quick."
        };

        // bargain lines
        string[] angryRejectLines =
        {
            "This offer is an insult. I refuse.",
            "If I sell for that price, I will starve.",
            "No way. Go somewhere else with that price."
        };

        string[] noDiscountLines =
        {
            "No discount. Take it or leave it.",
            "The price is firm. No haggling.",
            "This is the real value. I cannot go lower."
        };

        string[] acceptLines =
        {
            "You bargained well, I will sell for ",
            "Fine, for you I will make it ",
            "All right, we have a deal for "
        };

        string[] counterLines =
        {
            "I cannot go that low, but I can do ",
            "That is too low. How about ",
            "I will lower it a bit. Let us say "
        };

        // bargain tuning
        [Header("Bargain tuning")]
        [Tooltip("offer/base >= easyAcceptMul -> direct accept")]
        public float easyAcceptMul = 0.88f;   // about 42-48 accepted when base=48

        [Tooltip("offer/base >= counterMinMul and < easyAcceptMul -> counter offer")]
        public float counterMinMul = 0.83f;   // about 40-42 counter when base=48

        // anger system
        [Header("Anger system")]
        [Tooltip("Above this value, shop will refuse to trade for the rest of the day")]
        public float angerThreshold = 1.0f;

        [Tooltip("How much anger to add on very low offer")]
        public float angerIncreaseLowball = 0.6f;

        [Tooltip("How much anger to add on normal counter situation")]
        public float angerIncreaseCounter = 0.3f;

        [Tooltip("How much anger to reduce on a good, fair deal")]
        public float angerDecreaseOnGoodDeal = 0.2f;

        // anger per shop runtime state
        Dictionary<ShopRuntimeState, float> shopAnger =
            new Dictionary<ShopRuntimeState, float>();

        string RandomNpcLine(string[] pool)
        {
            if (pool == null || pool.Length == 0)
                return "";
            int idx = Random.Range(0, pool.Length);
            return pool[idx];
        }

        string RandomNpcLineWithPrice(string[] pool, float price)
        {
            if (pool == null || pool.Length == 0)
                return price.ToString("0") + ".";
            int idx = Random.Range(0, pool.Length);
            return pool[idx] + price.ToString("0") + ".";
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        // call this when a new day starts
        public void AdvanceDay()
        {
            currentDay++;
            shopCache.Clear();
            shopAnger.Clear();

            if (debugLogs)
                Debug.Log("TradeManager: new day " + currentDay + ", shop cache cleared.");
        }

        // called from villager when player interacts
        public void EnterShop(ShopData shopData)
        {
            if (shopData == null)
            {
                Debug.LogError("TradeManager.EnterShop called with null ShopData");
                return;
            }

            ShopRuntimeState state;

            if (!shopCache.TryGetValue(shopData, out state))
            {
                state = GenerateRuntimeShop(shopData);
                shopCache[shopData] = state;
            }

            CurrentShop = state;

            if (!shopAnger.ContainsKey(CurrentShop))
                shopAnger[CurrentShop] = 0f;

            if (debugLogs)
                Debug.Log("Entered shop: " + shopData.displayName + " (day " + currentDay + ")");

            OnShopOpened?.Invoke(CurrentShop);

            string line;

            if (IsShopBlocked(CurrentShop))
            {
                line = "I will not trade with you today.";
            }
            else
            {
                line = GetGreetingLine(CurrentShop.mood);
            }

            OnNpcResponse?.Invoke(NpcResponseType.Greeting, line);
        }

        ShopRuntimeState GenerateRuntimeShop(ShopData shopData)
        {
            ShopRuntimeState state = new ShopRuntimeState();
            state.data = shopData;

            state.mood = PickRandomMood(shopData);

            List<FishDef> pool = new List<FishDef>(shopData.possibleFish);
            int count = Mathf.Clamp(
                Random.Range(shopData.minFishTypes, shopData.maxFishTypes + 1),
                0, pool.Count);

            for (int i = 0; i < count; i++)
            {
                if (pool.Count == 0) break;

                int index = Random.Range(0, pool.Count);
                FishDef def = pool[index];
                pool.RemoveAt(index);

                RuntimeFishStock runtimeStock = new RuntimeFishStock();
                runtimeStock.fish = def;

                runtimeStock.quantityKg = Random.Range(
                    shopData.minStockKg,
                    shopData.maxStockKg + 1);

                float mul = Random.Range(shopData.minPriceMul, shopData.maxPriceMul);
                mul *= shopData.shopPriceBias;

                float rawPrice = def.basePricePerKg * mul;
                runtimeStock.unitPricePerKg = Mathf.Round(rawPrice);

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

            return NpcMood.Angry;
        }

        string GetGreetingLine(NpcMood mood)
        {
            string[] pool;

            switch (mood)
            {
                case NpcMood.Happy:
                    pool = happyGreetLines;
                    break;
                case NpcMood.Annoyed:
                    pool = annoyedGreetLines;
                    break;
                case NpcMood.Angry:
                    pool = angryGreetLines;
                    break;
                default:
                    pool = neutralGreetLines;
                    break;
            }

            if (pool == null || pool.Length == 0)
                return "Hello.";

            int index = Random.Range(0, pool.Length);
            return pool[index];
        }

        public bool IsShopBlocked(ShopRuntimeState shop)
        {
            if (shop == null) return false;

            float anger;
            if (!shopAnger.TryGetValue(shop, out anger))
                return false;

            return anger >= angerThreshold;
        }

        public void BlockCurrentShopForToday()
        {
            if (CurrentShop == null) return;

            shopAnger[CurrentShop] = angerThreshold;

            if (debugLogs)
                Debug.Log("TradeManager: shop blocked for today due to bargaining.");
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

            // Guvenlik kontrolu
            if (CurrentShop == null || stock == null || quantityKg <= 0 || baseTotal <= 0.01f)
            {
                result.finalTotal = baseTotal;
                result.responseType = NpcResponseType.Angry;
                result.npcLine = "I do not like this offer.";
                return result;
            }

            // Bu dukkan icin izin verilen min / max total
            float minAllowed = baseTotal * CurrentShop.data.bargainMinTotalMul;
            float maxAllowed = baseTotal * CurrentShop.data.bargainMaxTotalMul;

            // Oyuncunun istedigi fiyati bu araliga zorluyoruz
            desiredTotal = Mathf.Clamp(desiredTotal, minAllowed, maxAllowed);

            // Oyuncunun istedigi indirim miktari
            float discountAmount = baseTotal - desiredTotal; // ne kadar dusurmek istiyor
            if (discountAmount < 0f) discountAmount = 0f;

            float discountPct = 0f; // 0-1 arasi
            if (baseTotal > 0.01f)
                discountPct = discountAmount / baseTotal;

            // Burada random esikler uretiyoruz
            // Orn: acceptRange = 0.22f (%22), counterExtra = 0.07f -> counterRange = %29
            float acceptRange = Random.Range(0f, 0.30f);   // 0% - 30% arasi, direkt kabul
            float counterExtra = Random.Range(0f, 0.15f);  // ek 0% - 15% arasi, karsi teklif alani
            float counterRange = acceptRange + counterExtra;

            float finalTotal = baseTotal;
            NpcResponseType respType;
            string line;

            if (discountPct <= acceptRange + 0.0001f)
            {
                // Oyuncunun istedigi indirim random kabul esiginden kucuk:
                // Direkt istedigi fiyata veriyoruz.
                finalTotal = desiredTotal;
                respType = NpcResponseType.Accept;
                line = "Alright, I can accept this price.";
            }
            else if (discountPct <= counterRange + 0.0001f)
            {
                // Burada NPC karsi teklif yapiyor.
                // Fiyat, baseTotal ile desiredTotal arasinda bir yerde.
                float t = Random.Range(0.3f, 0.7f); // ortalara yakin bir oran
                finalTotal = Mathf.Lerp(baseTotal, desiredTotal, t);

                // Guvenlik icin clamp
                finalTotal = Mathf.Clamp(finalTotal, minAllowed, baseTotal);

                respType = NpcResponseType.Counter;
                line = "That is too low, but I can go down to " + finalTotal.ToString("0") + ".";
            }
            else
            {
                // Oyuncu cok fazla indirim istemis: hic bir indirim yok, NPC kiziyor.
                finalTotal = baseTotal;
                respType = NpcResponseType.Angry;
                line = "No discount. Take it or leave it.";
            }

            result.finalTotal = finalTotal;
            result.responseType = respType;
            result.npcLine = line;

            // UI icin event
            OnNpcResponse?.Invoke(respType, line);

            return result;
        }

        float GetMoodDiscountFactor(NpcMood mood)
        {
            switch (mood)
            {
                case NpcMood.Happy:
                    return 0.9f;
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
            if (DeliveryManager.Instance != null)
            {
                DeliveryManager.Instance.SpawnFishCrates(stock.fish, quantityKg);
            }
            // Stoktan dus
            stock.quantityKg -= quantityKg;
            if (stock.quantityKg < 0)
                stock.quantityKg = 0;

            if (debugLogs)
            {
                Debug.Log("Player bought " + quantityKg + " kg of " +
                          stock.fish.displayName + " for total " + totalPaid.ToString("0"));
            }

            // IMPORTANT: fishDef VE kg ile kasa spawn et
            if (DeliveryManager.Instance != null)
            {
                DeliveryManager.Instance.SpawnFishCrates(stock.fish, quantityKg);
            }

            OnNpcResponse?.Invoke(NpcResponseType.Accept, "Deal. Pleasure doing business.");
        }


    }
}
