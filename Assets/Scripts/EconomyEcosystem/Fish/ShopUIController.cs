using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Economy;

public class ShopUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;
    public TMP_Text shopNameText;
    public Button closeButton;

    [Header("Left list")]
    public Transform fishListContent;
    public ShopFishListItemUI fishListItemPrefab;

    [Header("Center panel - selected fish")]
    public Image selectedFishIcon;
    public TMP_Text selectedFishNameText;
    public TMP_Text selectedFishStockText;
    public TMP_Text selectedFishBasePriceText;
    public TMP_Text fishInfoText;

    [Header("Quantity")]
    public GameObject quantityPanel;
    public TMP_InputField quantityInput;
    public Button quantityMinusButton;
    public Button quantityPlusButton;

    [Header("Totals")]
    public GameObject totalsPanel;
    public TMP_Text baseTotalText;
    public TMP_Text discountText;
    public TMP_Text finalTotalText;

    [Header("Right panel - npc")]
    public Image npcPortrait;
    public TMP_Text npcDialogueText;

    [Header("Bottom bar")]
    public Button bargainButton;
    public Button acceptButton;
    public Button cancelButton;

    [Header("Bargain popup")]
    public GameObject bargainOverlay;
    public Button bargainCloseButton;
    public TMP_Text normalPriceText;
    public TMP_Text minPriceText;
    public TMP_Text maxPriceText;
    public Slider bargainSlider;
    public TMP_Text desiredPriceText;
    public Button bargainConfirmButton;

    [Header("External")]
    public VillagerInteractionCam villagerInteraction;

    [Header("Player money UI")]
    public TMP_Text playerMoneyText;

    ShopRuntimeState currentShop;
    RuntimeFishStock selectedStock;
    int currentQuantity = 0;

    float baseTotal = 0f;
    float finalTotal = 0f;
    bool hasBargain = false;

    float sliderMinTotal = 0f;
    float sliderMaxTotal = 0f;

    bool priceLocked = false;

    Vector3 acceptPosDefault;
    Vector3 acceptPosCentered;

    int bargainAttempts = 0;

    // ---- FIX: money rounding helpers ----
    // UI ile ödeme ayný olsun diye tüm totals int mantýðýyla kilitleniyor.
    int MoneyRound(float value)
    {
        // float jitter (110.00001 gibi) problemini bitirir.
        return Mathf.RoundToInt(value);
    }

    void SnapTotalsToMoney()
    {
        baseTotal = MoneyRound(baseTotal);
        finalTotal = MoneyRound(finalTotal);
    }
    // ------------------------------------

    void Awake()
    {
        if (bargainButton != null)
            bargainButton.onClick.AddListener(OnBargainClicked);

        if (acceptButton != null)
            acceptButton.onClick.AddListener(OnAcceptClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(OnCloseClicked);

        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseClicked);

        if (bargainCloseButton != null)
            bargainCloseButton.onClick.AddListener(CloseBargainPopup);

        if (bargainConfirmButton != null)
            bargainConfirmButton.onClick.AddListener(OnBargainConfirmClicked);

        if (quantityPlusButton != null)
            quantityPlusButton.onClick.AddListener(() => ChangeQuantity(+1));

        if (quantityMinusButton != null)
            quantityMinusButton.onClick.AddListener(() => ChangeQuantity(-1));

        if (quantityInput != null)
            quantityInput.onEndEdit.AddListener(OnQuantityInputChanged);

        if (bargainSlider != null)
        {
            bargainSlider.minValue = 0f;
            bargainSlider.maxValue = 1f;
            bargainSlider.onValueChanged.AddListener(OnBargainSliderChanged);
        }

        if (acceptButton != null)
        {
            acceptPosDefault = acceptButton.transform.localPosition;

            if (bargainButton != null && cancelButton != null)
            {
                Vector3 left = bargainButton.transform.localPosition;
                Vector3 right = cancelButton.transform.localPosition;
                acceptPosCentered = (left + right) * 0.5f;
            }
            else
            {
                acceptPosCentered = acceptPosDefault;
            }
        }
    }

    void Start()
    {
        if (TradeManager.Instance != null)
        {
            TradeManager.Instance.OnShopOpened += HandleShopOpened;
            TradeManager.Instance.OnNpcResponse += HandleNpcResponse;
        }

        if (rootPanel != null)
            rootPanel.SetActive(false);

        if (bargainOverlay != null)
            bargainOverlay.SetActive(false);

        if (quantityPanel != null)
            quantityPanel.SetActive(false);
        if (totalsPanel != null)
            totalsPanel.SetActive(false);

        ClearSelectedFishUI();
        SetPriceLock(false);
        UpdateTotalsUI();

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(MoneyManager.Instance.CurrentMoney);
        }
    }

    void OnDestroy()
    {
        if (TradeManager.Instance != null)
        {
            TradeManager.Instance.OnShopOpened -= HandleShopOpened;
            TradeManager.Instance.OnNpcResponse -= HandleNpcResponse;
        }

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
    }

    void HandleMoneyChanged(int amount)
    {
        if (playerMoneyText != null)
            playerMoneyText.text = amount.ToString();
    }

    void SetPriceLock(bool locked)
    {
        priceLocked = locked;
        bool canEdit = !locked;

        if (quantityInput != null)
            quantityInput.interactable = canEdit;
        if (quantityMinusButton != null)
            quantityMinusButton.interactable = canEdit;
        if (quantityPlusButton != null)
            quantityPlusButton.interactable = canEdit;

        if (bargainButton != null)
            bargainButton.gameObject.SetActive(canEdit);

        if (acceptButton != null)
        {
            acceptButton.transform.localPosition = locked
                ? acceptPosCentered
                : acceptPosDefault;
        }
    }

    void HandleShopOpened(ShopRuntimeState state)
    {
        currentShop = state;
        selectedStock = null;
        currentQuantity = 0;
        baseTotal = 0f;
        finalTotal = 0f;
        hasBargain = false;
        bargainAttempts = 0;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (shopNameText != null && state.data != null)
            shopNameText.text = state.data.displayName;

        if (npcPortrait != null && state.data != null)
            npcPortrait.sprite = state.data.npcPortrait;

        if (npcDialogueText != null)
            npcDialogueText.text = "";

        ClearSelectedFishUI();

        if (fishListContent != null)
        {
            for (int i = fishListContent.childCount - 1; i >= 0; i--)
                Destroy(fishListContent.GetChild(i).gameObject);
        }

        if (fishListItemPrefab != null && fishListContent != null)
        {
            foreach (RuntimeFishStock stock in state.currentStock)
            {
                if (stock == null || stock.fish == null)
                    continue;

                ShopFishListItemUI item = Instantiate(fishListItemPrefab, fishListContent);
                item.Setup(stock, this);
            }
        }

        SetPriceLock(false);
        UpdateTotalsUI();

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(true);
        if (bargainButton != null)
            bargainButton.gameObject.SetActive(true);

        if (MoneyManager.Instance != null)
            HandleMoneyChanged(MoneyManager.Instance.CurrentMoney);
    }

    void HandleNpcResponse(NpcResponseType type, string line)
    {
        if (npcDialogueText != null && !string.IsNullOrEmpty(line))
            npcDialogueText.text = line;
    }

    void ClearSelectedFishUI()
    {
        if (quantityPanel != null)
            quantityPanel.SetActive(false);
        if (totalsPanel != null)
            totalsPanel.SetActive(false);

        if (selectedFishIcon != null)
        {
            selectedFishIcon.sprite = null;
            selectedFishIcon.color = new Color(1f, 1f, 1f, 0f);
        }

        if (selectedFishNameText != null) selectedFishNameText.text = "";
        if (selectedFishStockText != null) selectedFishStockText.text = "";
        if (selectedFishBasePriceText != null) selectedFishBasePriceText.text = "";
        if (fishInfoText != null) fishInfoText.text = "";

        currentQuantity = 0;
        if (quantityInput != null) quantityInput.text = "";

        bargainAttempts = 0;

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(true);
        if (bargainButton != null)
            bargainButton.gameObject.SetActive(true);
    }

    public void OnFishSelected(RuntimeFishStock stock)
    {
        if (priceLocked)
            return;

        selectedStock = stock;
        hasBargain = false;
        bargainAttempts = 0;

        if (selectedFishIcon != null)
        {
            selectedFishIcon.sprite = stock.fish.icon;
            selectedFishIcon.color = Color.white;
        }

        if (selectedFishNameText != null)
            selectedFishNameText.text = stock.fish.displayName;

        if (selectedFishStockText != null)
            selectedFishStockText.text = "Stock: " + stock.quantityKg + " kg";

        if (selectedFishBasePriceText != null)
            selectedFishBasePriceText.text =
                "Price: " + stock.unitPricePerKg.ToString("0") + " /kg";

        if (fishInfoText != null)
            fishInfoText.text = stock.fish.description;

        currentQuantity = 1;
        if (quantityInput != null)
            quantityInput.text = "1";

        if (quantityPanel != null)
            quantityPanel.SetActive(true);
        if (totalsPanel != null)
            totalsPanel.SetActive(true);

        SetPriceLock(false);

        RecalculateBaseAndFinal();
    }

    void ChangeQuantity(int delta)
    {
        if (selectedStock == null) return;
        if (priceLocked) return;

        currentQuantity += delta;
        if (currentQuantity < 1)
            currentQuantity = 1;
        if (currentQuantity > selectedStock.quantityKg)
            currentQuantity = selectedStock.quantityKg;

        if (quantityInput != null)
            quantityInput.text = currentQuantity.ToString();

        if (!hasBargain)
            finalTotal = currentQuantity * selectedStock.unitPricePerKg;

        RecalculateBaseAndFinal();
    }

    void OnQuantityInputChanged(string value)
    {
        if (selectedStock == null)
        {
            if (quantityInput != null) quantityInput.text = "";
            return;
        }

        if (priceLocked)
        {
            if (quantityInput != null && currentQuantity > 0)
                quantityInput.text = currentQuantity.ToString();
            return;
        }

        int parsed;
        if (!int.TryParse(value, out parsed))
            parsed = currentQuantity > 0 ? currentQuantity : 1;

        parsed = Mathf.Clamp(parsed, 1, selectedStock.quantityKg);
        currentQuantity = parsed;

        if (quantityInput != null)
            quantityInput.text = currentQuantity.ToString();

        if (!hasBargain)
            finalTotal = currentQuantity * selectedStock.unitPricePerKg;

        RecalculateBaseAndFinal();
    }

    void RecalculateBaseAndFinal()
    {
        if (selectedStock == null)
        {
            baseTotal = 0f;
            finalTotal = 0f;
            hasBargain = false;
            UpdateTotalsUI();
            return;
        }

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;

        if (!hasBargain)
            finalTotal = baseTotal;

        // FIX: totals money kilidi
        SnapTotalsToMoney();

        UpdateTotalsUI();
    }

    void UpdateTotalsUI()
    {
        // FIX: UI her zaman int gösterir
        int baseInt = MoneyRound(baseTotal);
        int finalInt = MoneyRound(finalTotal);
        int discountInt = baseInt - finalInt;
        if (discountInt < 0) discountInt = 0;

        if (baseTotalText != null)
            baseTotalText.text = "TOTAL: " + baseInt.ToString();

        if (discountText != null)
        {
            if (hasBargain && discountInt > 0)
                discountText.text = "BARGAIN: -" + discountInt.ToString();
            else
                discountText.text = "BARGAIN: 0";
        }

        if (finalTotalText != null)
            finalTotalText.text = "= " + finalInt.ToString();
    }

    void OnBargainClicked()
    {
        if (selectedStock == null) return;
        if (priceLocked) return;

        OpenBargainPopup();
    }

    void OpenBargainPopup()
    {
        if (bargainOverlay == null || selectedStock == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;
        if (!hasBargain)
            finalTotal = baseTotal;

        SnapTotalsToMoney();

        float minMul = currentShop != null && currentShop.data != null
            ? currentShop.data.bargainMinTotalMul
            : 0.6f;
        float maxMul = currentShop != null && currentShop.data != null
            ? currentShop.data.bargainMaxTotalMul
            : 1.0f;

        sliderMinTotal = baseTotal * minMul;
        sliderMaxTotal = baseTotal * maxMul;

        // slider min/max da int kilitli olsun (UI ile tutarlý)
        sliderMinTotal = MoneyRound(sliderMinTotal);
        sliderMaxTotal = MoneyRound(sliderMaxTotal);

        if (normalPriceText != null)
            normalPriceText.text = "Normal: " + MoneyRound(baseTotal).ToString();
        if (minPriceText != null)
            minPriceText.text = "Min: " + MoneyRound(sliderMinTotal).ToString();
        if (maxPriceText != null)
            maxPriceText.text = "Max: " + MoneyRound(sliderMaxTotal).ToString();

        if (bargainSlider != null)
            bargainSlider.value = 1f;

        OnBargainSliderChanged(bargainSlider != null ? bargainSlider.value : 1f);

        bargainOverlay.SetActive(true);
    }

    void CloseBargainPopup()
    {
        if (bargainOverlay != null)
            bargainOverlay.SetActive(false);
    }

    void OnBargainSliderChanged(float value)
    {
        if (desiredPriceText == null)
            return;

        float price = Mathf.Lerp(sliderMinTotal, sliderMaxTotal, value);
        desiredPriceText.text = MoneyRound(price).ToString();
    }

    void OnBargainConfirmClicked()
    {
        if (selectedStock == null || currentShop == null || TradeManager.Instance == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;

        float t = bargainSlider != null ? bargainSlider.value : 1f;
        float desiredTotal = Mathf.Lerp(sliderMinTotal, sliderMaxTotal, t);

        bargainAttempts++;

        BargainResult result = TradeManager.Instance.EvaluateBargain(
            selectedStock, currentQuantity, baseTotal, desiredTotal);

        finalTotal = result.finalTotal;

        // FIX: bargain sonucu da int’e kilitle
        SnapTotalsToMoney();

        hasBargain = (result.responseType == NpcResponseType.Accept ||
                      result.responseType == NpcResponseType.Counter);

        UpdateTotalsUI();

        if (npcDialogueText != null && !string.IsNullOrEmpty(result.npcLine))
            npcDialogueText.text = result.npcLine;

        if (result.responseType == NpcResponseType.Accept)
        {
            SetPriceLock(true);
            if (quantityPanel != null)
                quantityPanel.SetActive(false);
        }
        else if (result.responseType == NpcResponseType.Counter)
        {
            SetPriceLock(false);
        }

        if (bargainAttempts >= 2 && result.responseType != NpcResponseType.Accept)
        {
            if (TradeManager.Instance != null)
                TradeManager.Instance.BlockCurrentShopForToday();

            if (npcDialogueText != null)
                npcDialogueText.text = "I will not trade with you anymore today.";

            if (acceptButton != null)
                acceptButton.gameObject.SetActive(false);
            if (bargainButton != null)
                bargainButton.gameObject.SetActive(false);
        }

        CloseBargainPopup();
    }

    void OnAcceptClicked()
    {
        Debug.Log("[ShopUI] Accept clicked.");

        if (bargainOverlay != null && bargainOverlay.activeSelf)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "Close or confirm the bargain first.";
            return;
        }

        if (TradeManager.Instance != null &&
            TradeManager.Instance.IsShopBlocked(currentShop))
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "I will not trade with you anymore today.";
            Debug.Log("[ShopUI] Shop is blocked due to high anger.");
            return;
        }

        if (TradeManager.Instance == null)
        {
            Debug.LogWarning("[ShopUI] TradeManager.Instance is null.");
            return;
        }

        if (currentShop == null)
        {
            Debug.LogWarning("[ShopUI] currentShop is null.");
            return;
        }

        if (selectedStock == null)
        {
            Debug.Log("[ShopUI] No fish selected, cannot accept.");
            if (npcDialogueText != null)
                npcDialogueText.text = "Select what you want to buy first.";
            return;
        }

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;

        if (!hasBargain)
            finalTotal = baseTotal;

        // FIX: ödeme öncesi de kilitle
        SnapTotalsToMoney();

        // FIX: UI ile ayný olacak
        int priceToPay = MoneyRound(finalTotal);

        Debug.Log("[ShopUI] Price to pay: " + priceToPay + " finalTotalRaw=" + finalTotal.ToString("F6"));

        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning("[ShopUI] MoneyManager instance missing in scene.");
            return;
        }

        if (!MoneyManager.Instance.CanAfford(priceToPay))
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "You do not have enough money.";

            Debug.Log("[ShopUI] Not enough money. Player has: " +
                      MoneyManager.Instance.CurrentMoney +
                      ", needs: " + priceToPay);
            return;
        }

        bool spent = MoneyManager.Instance.TrySpend(priceToPay);

        Debug.Log("[ShopUI] TrySpend result=" + spent +
                  " CurrentMoney AFTER=" + MoneyManager.Instance.CurrentMoney);

        if (!spent)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "You do not have enough money.";
            return;
        }

        // CompleteTrade float alýyor ama biz int’e kilitledik zaten
        TradeManager.Instance.CompleteTrade(selectedStock, currentQuantity, finalTotal);

        OnCloseClicked();
    }

    void OnCloseClicked()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        selectedStock = null;
        currentShop = null;
        hasBargain = false;
        baseTotal = 0f;
        finalTotal = 0f;
        currentQuantity = 0;
        SetPriceLock(false);
        UpdateTotalsUI();
        ClearSelectedFishUI();

        if (bargainOverlay != null)
            bargainOverlay.SetActive(false);

        if (VillagerInteractionCam.Current != null)
            VillagerInteractionCam.Current.ExitConversation();
    }
}
