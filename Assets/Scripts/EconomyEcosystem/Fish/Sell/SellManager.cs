using System.Collections.Generic;
using UnityEngine;

namespace Economy
{
    // Runtime data for each fish in sell UI
    public class BuyerRuntimeFish
    {
        public FishDef fish;
        public int availableFromPlayerKg;
        public int demandKg;       // buyer max kg
        public float unitPricePerKg;
        public bool accepts;
    }

    // Runtime state for current buyer session
    public class BuyerRuntimeState
    {
        public BuyerData data;
        public BuyerDeliveryZone deliveryZone;
        public List<BuyerRuntimeFish> fishList = new List<BuyerRuntimeFish>();
    }

    // Internal cached config: this does not change during the day
    class BuyerCachedFishConfig
    {
        public FishDef fish;
        public bool accepts;
        public int demandKg;
        public float unitPricePerKg;
    }

    public class SellManager : MonoBehaviour
    {
        public static SellManager Instance;

        public bool debugLogs = true;

        // how many angry rejections before buyer is locked for today
        public int maxDailyRejects = 2;

        // current opened buyer
        public BuyerRuntimeState CurrentBuyer { get; private set; }

        // events for UI
        public event System.Action<BuyerRuntimeState> OnSellOpened;
        public event System.Action<NpcResponseType, string> OnBuyerResponse;

        // per buyer: cached fish configs (demand + price) for "today"
        Dictionary<string, List<BuyerCachedFishConfig>> buyerFishConfigCache =
            new Dictionary<string, List<BuyerCachedFishConfig>>();

        // per buyer: how many times they got angry today
        Dictionary<string, int> buyerRejectCount =
            new Dictionary<string, int>();

        // buyers that will not trade anymore today
        HashSet<string> buyerLockedForToday =
            new HashSet<string>();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // helper: get key for dictionaries
        string GetBuyerKey(BuyerData buyerData)
        {
            if (buyerData == null) return "";
            if (!string.IsNullOrEmpty(buyerData.id))
                return buyerData.id;
            return buyerData.name;
        }

        // called from VillagerInteractionCam when player presses E on a buyer
        public void EnterBuyer(BuyerData buyerData, BuyerDeliveryZone zone)
        {
            if (buyerData == null || zone == null)
            {
                Debug.LogError("SellManager.EnterBuyer: buyerData or zone is null.");
                return;
            }

            string buyerKey = GetBuyerKey(buyerData);

            BuyerRuntimeState state = new BuyerRuntimeState();
            state.data = buyerData;
            state.deliveryZone = zone;

            bool isLockedToday = buyerLockedForToday.Contains(buyerKey);

            if (!isLockedToday)
            {
                // make sure we have config list for this buyer
                List<BuyerCachedFishConfig> cfgList;
                if (!buyerFishConfigCache.TryGetValue(buyerKey, out cfgList))
                {
                    cfgList = new List<BuyerCachedFishConfig>();
                    buyerFishConfigCache[buyerKey] = cfgList;
                }

                // helper local function
                System.Func<FishDef, BuyerCachedFishConfig> findCfg =
                    (FishDef fish) =>
                    {
                        for (int i = 0; i < cfgList.Count; i++)
                        {
                            if (cfgList[i].fish == fish)
                                return cfgList[i];
                        }
                        return null;
                    };

                // read crates from delivery zone
                List<SellFishSummary> summary = zone.GetCurrentFishSummary();

                for (int i = 0; i < summary.Count; i++)
                {
                    SellFishSummary s = summary[i];
                    if (s.fish == null || s.totalKg <= 0)
                        continue;

                    BuyerCachedFishConfig config = findCfg(s.fish);

                    if (config == null)
                    {
                        // create new config for this fish and buyer
                        config = new BuyerCachedFishConfig();
                        config.fish = s.fish;

                        BuyerFishRule rule = buyerData.GetRuleForFish(s.fish);

                        if (rule == null || !rule.accepts)
                        {
                            config.accepts = false;
                            config.demandKg = 0;
                            config.unitPricePerKg = 0f;
                        }
                        else
                        {
                            config.accepts = true;

                            // demand for entire day
                            int demand = Random.Range(rule.minDemandKg, rule.maxDemandKg + 1);
                            if (demand < 0) demand = 0;
                            config.demandKg = demand;

                            // base price (from rule or from fish)
                            float basePricePerKg = s.fish.basePricePerKg;
                            if (rule.basePricePerKg > 0f)
                                basePricePerKg = rule.basePricePerKg;

                            float mulMin = rule.randomMulMin;
                            float mulMax = rule.randomMulMax;
                            if (mulMax < mulMin) mulMax = mulMin;

                            float mul = Random.Range(mulMin, mulMax);
                            float rawPrice = basePricePerKg * mul;

                            config.unitPricePerKg = Mathf.Round(rawPrice);
                        }

                        cfgList.Add(config);
                    }

                    // create runtime fish from cached config
                    BuyerRuntimeFish rf = new BuyerRuntimeFish();
                    rf.fish = s.fish;
                    rf.availableFromPlayerKg = s.totalKg;
                    rf.accepts = config.accepts;
                    rf.demandKg = config.demandKg;
                    rf.unitPricePerKg = config.unitPricePerKg;

                    state.fishList.Add(rf);
                }
            }
            else
            {
                // buyer is locked today: no fish list
                state.fishList.Clear();
            }

            CurrentBuyer = state;

            if (debugLogs)
            {
                Debug.Log("SellManager: opened buyer " + buyerData.displayName +
                          " with " + state.fishList.Count + " fish types. LockedToday=" +
                          isLockedToday);

                for (int i = 0; i < state.fishList.Count; i++)
                {
                    BuyerRuntimeFish f = state.fishList[i];
                    Debug.Log("  Fish " + f.fish.name +
                              " avail=" + f.availableFromPlayerKg +
                              " demand=" + f.demandKg +
                              " pricePerKg=" + f.unitPricePerKg);
                }
            }

            // notify UI to open panel
            if (OnSellOpened != null)
                OnSellOpened(state);

            // greeting line
            if (OnBuyerResponse != null)
            {
                if (isLockedToday)
                    OnBuyerResponse(NpcResponseType.Angry, "I will not negotiate more today.");
                else
                    OnBuyerResponse(NpcResponseType.Greeting, "What do you have for me today?");
            }
        }

        // bargaining logic (player sells to buyer)
        public BargainResult EvaluateSellBargain(
            BuyerRuntimeFish rf,
            int quantityKg,
            float baseTotal,
            float desiredTotal)
        {
            BargainResult result = new BargainResult();
            result.baseTotal = baseTotal;
            result.desiredTotal = desiredTotal;

            string buyerKey = (CurrentBuyer != null && CurrentBuyer.data != null)
                ? GetBuyerKey(CurrentBuyer.data)
                : "";

            // if this buyer is already locked for today, no more bargaining
            if (!string.IsNullOrEmpty(buyerKey) &&
                buyerLockedForToday.Contains(buyerKey))
            {
                result.finalTotal = baseTotal;
                result.responseType = NpcResponseType.Angry;
                result.npcLine = "I will not negotiate more today.";

                if (OnBuyerResponse != null)
                    OnBuyerResponse(result.responseType, result.npcLine);

                return result;
            }

            // invalid input safety
            if (rf == null || quantityKg <= 0 || baseTotal <= 0.01f ||
                CurrentBuyer == null || CurrentBuyer.data == null)
            {
                result.finalTotal = baseTotal;
                result.responseType = NpcResponseType.Angry;
                result.npcLine = "I do not like this price.";

                if (OnBuyerResponse != null)
                    OnBuyerResponse(result.responseType, result.npcLine);

                return result;
            }

            // player wants higher total price
            float maxAllowed = baseTotal * CurrentBuyer.data.sellMaxTotalMul;
            float minAllowed = baseTotal;

            desiredTotal = Mathf.Clamp(desiredTotal, minAllowed, maxAllowed);

            float extraAmount = desiredTotal - baseTotal;
            if (extraAmount < 0f) extraAmount = 0f;

            float markupPct = 0f;
            if (baseTotal > 0.01f)
                markupPct = extraAmount / baseTotal;

            // random accept / counter bands
            float acceptRange = Random.Range(0f, 0.25f);   // percent
            float counterExtra = Random.Range(0f, 0.20f);  // percent
            float counterRange = acceptRange + counterExtra;

            float finalTotal = baseTotal;
            NpcResponseType respType;
            string line;

            if (markupPct <= acceptRange + 0.0001f)
            {
                // direct accept
                finalTotal = desiredTotal;
                respType = NpcResponseType.Accept;
                line = "Fine, I will pay that much.";
            }
            else if (markupPct <= counterRange + 0.0001f)
            {
                // counter offer somewhere in between
                float t = Random.Range(0.3f, 0.7f);
                finalTotal = Mathf.Lerp(baseTotal, desiredTotal, t);
                finalTotal = Mathf.Clamp(finalTotal, baseTotal, maxAllowed);

                respType = NpcResponseType.Counter;
                line = "That is high. I can pay " + finalTotal.ToString("0") + ".";
            }
            else
            {
                // angry reject
                finalTotal = baseTotal;
                respType = NpcResponseType.Angry;
                line = "Too expensive. I will not pay more.";
            }

            // if angry, increase reject counter and maybe lock buyer for today
            if (respType == NpcResponseType.Angry && !string.IsNullOrEmpty(buyerKey))
            {
                int count;
                if (!buyerRejectCount.TryGetValue(buyerKey, out count))
                    count = 0;
                count++;
                buyerRejectCount[buyerKey] = count;

                if (count >= maxDailyRejects)
                {
                    buyerLockedForToday.Add(buyerKey);
                    line = "I will not negotiate more today.";
                }
            }

            result.finalTotal = finalTotal;
            result.responseType = respType;
            result.npcLine = line;

            if (OnBuyerResponse != null)
                OnBuyerResponse(respType, line);

            return result;
        }

        // when sale is accepted and completed
        public void CompleteSale(BuyerRuntimeFish rf, int quantityKg, float totalPaid)
        {
            if (rf == null || quantityKg <= 0 || CurrentBuyer == null)
                return;

            if (CurrentBuyer.deliveryZone != null)
            {
                CurrentBuyer.deliveryZone.RemoveKgFromCrates(rf.fish, quantityKg);
            }

            rf.availableFromPlayerKg -= quantityKg;
            if (rf.availableFromPlayerKg < 0)
                rf.availableFromPlayerKg = 0;

            if (MoneyManager.Instance != null)
            {
                int money = Mathf.CeilToInt(totalPaid);
                MoneyManager.Instance.AddMoney(money);
            }

            if (OnBuyerResponse != null)
                OnBuyerResponse(NpcResponseType.Accept, "Good. Here is your money.");
        }

        // you can call this from your GameManager when a new day starts
        public void ResetDailyState()
        {
            buyerFishConfigCache.Clear();
            buyerRejectCount.Clear();
            buyerLockedForToday.Clear();
        }
    }
}
