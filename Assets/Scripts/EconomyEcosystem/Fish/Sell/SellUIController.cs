using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Economy;

public class SellUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;
    public TMP_Text buyerNameText;
    public Button closeButton;

    [Header("Left list")]
    public Transform fishListContent;
    public SellFishListItemUI fishListItemPrefab;

    [Header("Center panel - selected fish")]
    public Image selectedFishIcon;
    public TMP_Text selectedFishNameText;
    public TMP_Text playerStockText;    // "You brought: X kg"
    public TMP_Text buyerDemandText;    // "Buyer wants up to Y kg" or "Does not buy this fish"
    public TMP_Text unitPriceText;
    public TMP_Text fishInfoText;       // optional text from FishDef

    [Header("Quantity")]
    public GameObject quantityPanel;
    public TMP_InputField quantityInput;
    public Button quantityMinusButton;
    public Button quantityPlusButton;

    [Header("Totals")]
    public GameObject totalsPanel;
    public TMP_Text baseTotalText;
    public TMP_Text bargainText;
    public TMP_Text finalTotalText;

    [Header("Right panel - npc")]
    public Image npcPortrait;
    public TMP_Text npcDialogueText;

    [Header("Bottom bar")]
    public Button bargainButton;   // Offer
    public Button acceptButton;    // Accept
    public Button cancelButton;    // Cancel

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

    // runtime state
    BuyerRuntimeState currentBuyer;
    BuyerRuntimeFish selectedFish;
    int currentQuantity = 0;

    float baseTotal = 0f;
    float finalTotal = 0f;
    bool hasBargain = false;

    float sliderMinTotal = 0f;
    float sliderMaxTotal = 0f;

    bool priceLocked = false;
    bool quantityLocked = false;   // <<< yeni flag

    Vector3 acceptPosDefault;
    Vector3 acceptPosCentered;

    int bargainAttempts = 0;

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
        if (SellManager.Instance != null)
        {
            SellManager.Instance.OnSellOpened += HandleSellOpened;
            SellManager.Instance.OnBuyerResponse += HandleBuyerResponse;
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
        quantityLocked = false;
        UpdateTotalsUI();

        if (MoneyManager.Instance != null)
        {
            MoneyManager.Instance.OnMoneyChanged += HandleMoneyChanged;
            HandleMoneyChanged(MoneyManager.Instance.CurrentMoney);
        }
    }

    void OnDestroy()
    {
        if (SellManager.Instance != null)
        {
            SellManager.Instance.OnSellOpened -= HandleSellOpened;
            SellManager.Instance.OnBuyerResponse -= HandleBuyerResponse;
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

        // quantity panel acik / kapali kontrolu buradan degil
        if (quantityInput != null)
            quantityInput.interactable = canEdit && !quantityLocked;
        if (quantityMinusButton != null)
            quantityMinusButton.interactable = canEdit && !quantityLocked;
        if (quantityPlusButton != null)
            quantityPlusButton.interactable = canEdit && !quantityLocked;

        if (bargainButton != null)
            bargainButton.gameObject.SetActive(canEdit);

        if (acceptButton != null)
        {
            acceptButton.transform.localPosition = locked
                ? acceptPosCentered
                : acceptPosDefault;
        }
    }

    void HandleSellOpened(BuyerRuntimeState state)
    {
        currentBuyer = state;
        selectedFish = null;
        currentQuantity = 0;
        baseTotal = 0f;
        finalTotal = 0f;
        hasBargain = false;
        bargainAttempts = 0;
        quantityLocked = false;

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (buyerNameText != null && state.data != null)
            buyerNameText.text = state.data.displayName;

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
            foreach (BuyerRuntimeFish rf in state.fishList)
            {
                if (rf == null || rf.fish == null)
                    continue;

                SellFishListItemUI item = Instantiate(fishListItemPrefab, fishListContent);
                item.Setup(rf, this);
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

    void HandleBuyerResponse(NpcResponseType type, string line)
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
        if (playerStockText != null) playerStockText.text = "";
        if (buyerDemandText != null) buyerDemandText.text = "";
        if (unitPriceText != null) unitPriceText.text = "";
        if (fishInfoText != null) fishInfoText.text = "";

        currentQuantity = 0;
        if (quantityInput != null) quantityInput.text = "";

        bargainAttempts = 0;
        quantityLocked = false;

        if (acceptButton != null)
            acceptButton.gameObject.SetActive(true);
        if (bargainButton != null)
            bargainButton.gameObject.SetActive(true);
    }

    public void OnFishSelected(BuyerRuntimeFish rf)
    {
        if (priceLocked)
            return;

        selectedFish = rf;
        hasBargain = false;
        bargainAttempts = 0;
        quantityLocked = false;

        if (selectedFishIcon != null)
        {
            selectedFishIcon.sprite = rf.fish.icon;
            selectedFishIcon.color = Color.white;
        }

        if (selectedFishNameText != null)
            selectedFishNameText.text = rf.fish.displayName;

        if (playerStockText != null)
            playerStockText.text = "You brought: " + rf.availableFromPlayerKg + " kg";

        if (rf.accepts && rf.demandKg > 0)
        {
            if (buyerDemandText != null)
                buyerDemandText.text = "Buyer wants up to: " + rf.demandKg + " kg";

            if (unitPriceText != null)
                unitPriceText.text = "Price: " + rf.unitPricePerKg.ToString("0") + " /kg";
        }
        else
        {
            if (buyerDemandText != null)
                buyerDemandText.text = "Buyer does not buy this fish.";

            if (unitPriceText != null)
                unitPriceText.text = "";
        }

        if (fishInfoText != null)
            fishInfoText.text = rf.fish.description;

        int maxQty = rf.availableFromPlayerKg;
        if (rf.accepts && rf.demandKg > 0)
            maxQty = Mathf.Min(rf.availableFromPlayerKg, rf.demandKg);

        if (!rf.accepts || maxQty <= 0)
        {
            currentQuantity = 0;
            if (quantityPanel != null)
                quantityPanel.SetActive(false);
            if (totalsPanel != null)
                totalsPanel.SetActive(false);

            SetPriceLock(true);

            if (bargainButton != null)
                bargainButton.gameObject.SetActive(false);
            if (acceptButton != null)
                acceptButton.gameObject.SetActive(false);

            UpdateTotalsUI();
            return;
        }

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
        if (selectedFish == null) return;
        if (priceLocked) return;
        if (quantityLocked) return;   // <<< kg degistirme blok

        int maxQty = selectedFish.availableFromPlayerKg;
        if (selectedFish.accepts && selectedFish.demandKg > 0)
            maxQty = Mathf.Min(selectedFish.availableFromPlayerKg, selectedFish.demandKg);

        if (maxQty <= 0) return;

        currentQuantity += delta;
        if (currentQuantity < 1)
            currentQuantity = 1;
        if (currentQuantity > maxQty)
            currentQuantity = maxQty;

        if (quantityInput != null)
            quantityInput.text = currentQuantity.ToString();

        if (!hasBargain)
            finalTotal = currentQuantity * selectedFish.unitPricePerKg;

        RecalculateBaseAndFinal();
    }

    void OnQuantityInputChanged(string value)
    {
        if (selectedFish == null)
        {
            if (quantityInput != null) quantityInput.text = "";
            return;
        }

        if (quantityLocked)
        {
            // zaten sabit; inputu eski degerde tut
            if (quantityInput != null && currentQuantity > 0)
                quantityInput.text = currentQuantity.ToString();
            return;
        }

        if (priceLocked)
        {
            if (quantityInput != null && currentQuantity > 0)
                quantityInput.text = currentQuantity.ToString();
            return;
        }

        int maxQty = selectedFish.availableFromPlayerKg;
        if (selectedFish.accepts && selectedFish.demandKg > 0)
            maxQty = Mathf.Min(selectedFish.availableFromPlayerKg, selectedFish.demandKg);

        if (maxQty <= 0)
        {
            currentQuantity = 0;
            if (quantityInput != null) quantityInput.text = "";
            RecalculateBaseAndFinal();
            return;
        }

        int parsed;
        if (!int.TryParse(value, out parsed))
            parsed = currentQuantity > 0 ? currentQuantity : 1;

        parsed = Mathf.Clamp(parsed, 1, maxQty);
        currentQuantity = parsed;

        if (quantityInput != null)
            quantityInput.text = currentQuantity.ToString();

        if (!hasBargain)
            finalTotal = currentQuantity * selectedFish.unitPricePerKg;

        RecalculateBaseAndFinal();
    }

    void RecalculateBaseAndFinal()
    {
        if (selectedFish == null)
        {
            baseTotal = 0f;
            finalTotal = 0f;
            hasBargain = false;
            UpdateTotalsUI();
            return;
        }

        if (currentQuantity <= 0)
        {
            baseTotal = 0f;
            finalTotal = 0f;
            hasBargain = false;
            UpdateTotalsUI();
            return;
        }

        baseTotal = currentQuantity * selectedFish.unitPricePerKg;

        if (!hasBargain)
            finalTotal = baseTotal;

        UpdateTotalsUI();
    }

    void UpdateTotalsUI()
    {
        if (baseTotalText != null)
            baseTotalText.text = "TOTAL: " + baseTotal.ToString("0");

        float diff = finalTotal - baseTotal;

        if (bargainText != null)
        {
            if (hasBargain && Mathf.Abs(diff) > 0.01f)
            {
                if (diff > 0f)
                    bargainText.text = "BARGAIN: +" + diff.ToString("0");
                else
                    bargainText.text = "BARGAIN: " + diff.ToString("0");
            }
            else
                bargainText.text = "BARGAIN: 0";
        }

        if (finalTotalText != null)
            finalTotalText.text = "= " + finalTotal.ToString("0");
    }

    void OnBargainClicked()
    {
        if (selectedFish == null) return;
        if (priceLocked) return;

        OpenBargainPopup();
    }

    void OpenBargainPopup()
    {
        if (bargainOverlay == null || selectedFish == null || currentBuyer == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedFish.unitPricePerKg;
        if (!hasBargain)
            finalTotal = baseTotal;

        float maxMul = 1.4f;
        if (currentBuyer.data != null)
            maxMul = currentBuyer.data.sellMaxTotalMul;

        sliderMinTotal = baseTotal;
        sliderMaxTotal = baseTotal * maxMul;

        if (normalPriceText != null)
            normalPriceText.text = "Base: " + baseTotal.ToString("0");
        if (minPriceText != null)
            minPriceText.text = "Min: " + sliderMinTotal.ToString("0");
        if (maxPriceText != null)
            maxPriceText.text = "Max: " + sliderMaxTotal.ToString("0");

        if (bargainSlider != null)
            bargainSlider.value = 0f;

        OnBargainSliderChanged(bargainSlider != null ? bargainSlider.value : 0f);

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
        desiredPriceText.text = price.ToString("0");
    }

    // kg kilitleme helper
    void LockQuantityAfterOffer()
    {
        quantityLocked = true;

        if (quantityInput != null)
            quantityInput.interactable = false;
        if (quantityMinusButton != null)
            quantityMinusButton.interactable = false;
        if (quantityPlusButton != null)
            quantityPlusButton.interactable = false;

        if (quantityPanel != null)
            quantityPanel.SetActive(false);
    }

    void OnBargainConfirmClicked()
    {
        if (selectedFish == null || currentBuyer == null || SellManager.Instance == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        // ilk confirm aninda kg sabitle
        if (!quantityLocked)
            LockQuantityAfterOffer();

        baseTotal = currentQuantity * selectedFish.unitPricePerKg;

        float t = bargainSlider != null ? bargainSlider.value : 0f;
        float desiredTotal = Mathf.Lerp(sliderMinTotal, sliderMaxTotal, t);

        bargainAttempts++;

        BargainResult result = SellManager.Instance.EvaluateSellBargain(
            selectedFish, currentQuantity, baseTotal, desiredTotal);

        finalTotal = result.finalTotal;
        hasBargain = (result.responseType == NpcResponseType.Accept ||
                      result.responseType == NpcResponseType.Counter);

        UpdateTotalsUI();

        if (npcDialogueText != null && !string.IsNullOrEmpty(result.npcLine))
            npcDialogueText.text = result.npcLine;

        if (result.responseType == NpcResponseType.Accept)
        {
            // teklif kabul: tamamen kilitle
            SetPriceLock(true);
            if (quantityPanel != null)
                quantityPanel.SetActive(false);
        }
        else if (result.responseType == NpcResponseType.Counter)
        {
            // karsi teklif: sadece fiyat acik, kg zaten quantityLocked
            SetPriceLock(false);
        }
        else if (result.responseType == NpcResponseType.Angry)
        {
            // zaten SellManager icinde daily reject sayiliyor
        }

        // 2 deneme sonra hala kabul yoksa bugunluk bitti
        if (bargainAttempts >= 2 && result.responseType != NpcResponseType.Accept)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "I will not negotiate more today.";

            if (acceptButton != null)
                acceptButton.gameObject.SetActive(false);
            if (bargainButton != null)
                bargainButton.gameObject.SetActive(false);

            if (quantityPanel != null)
                quantityPanel.SetActive(false);
            if (totalsPanel != null)
                totalsPanel.SetActive(false);
        }

        CloseBargainPopup();
    }

    void OnAcceptClicked()
    {
        if (bargainOverlay != null && bargainOverlay.activeSelf)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "Close or confirm the bargain first.";
            return;
        }

        if (SellManager.Instance == null)
        {
            Debug.LogWarning("[SellUI] SellManager.Instance is null.");
            return;
        }

        if (currentBuyer == null)
        {
            Debug.LogWarning("[SellUI] currentBuyer is null.");
            return;
        }

        if (selectedFish == null)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "Select what you want to sell first.";
            return;
        }

        if (!selectedFish.accepts || selectedFish.demandKg <= 0)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "Buyer does not want this fish.";
            return;
        }

        int maxQty = selectedFish.availableFromPlayerKg;
        if (selectedFish.demandKg > 0)
            maxQty = Mathf.Min(selectedFish.availableFromPlayerKg, selectedFish.demandKg);

        if (maxQty <= 0)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "You have nothing to sell.";
            return;
        }

        if (currentQuantity <= 0)
            currentQuantity = 1;
        if (currentQuantity > maxQty)
            currentQuantity = maxQty;

        baseTotal = currentQuantity * selectedFish.unitPricePerKg;

        if (!hasBargain)
            finalTotal = baseTotal;

        SellManager.Instance.CompleteSale(selectedFish, currentQuantity, finalTotal);

        OnCloseClicked();
    }

    void OnCloseClicked()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);

        selectedFish = null;
        currentBuyer = null;
        hasBargain = false;
        baseTotal = 0f;
        finalTotal = 0f;
        currentQuantity = 0;
        quantityLocked = false;
        SetPriceLock(false);
        UpdateTotalsUI();
        ClearSelectedFishUI();

        if (bargainOverlay != null)
            bargainOverlay.SetActive(false);

        if (VillagerInteractionCam.Current != null)
            VillagerInteractionCam.Current.ExitConversation();
    }
}
