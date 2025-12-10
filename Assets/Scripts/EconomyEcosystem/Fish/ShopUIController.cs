using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Economy; // TradeManager, ShopData, ShopRuntimeState, RuntimeFishStock, NpcResponseType

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
    public GameObject quantityPanel;        // QuantityPanel objesi (kg label, +/- ve input parent)
    public TMP_InputField quantityInput;
    public Button quantityMinusButton;
    public Button quantityPlusButton;

    [Header("Totals")]
    public TMP_Text baseTotalText;
    public TMP_Text discountText;
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

    [Header("Player money (temp)")]
    public int playerMoney = 100;
    public TMP_Text playerMoneyText;

    // runtime state
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

        ClearSelectedFishUI();
        SetPriceLock(false);
        UpdateTotalsUI();
        UpdatePlayerMoneyUI();
    }

    // price lock helper
    void SetPriceLock(bool locked)
    {
        priceLocked = locked;
        bool canEdit = !locked;

        if (quantityPanel != null)
            quantityPanel.SetActive(canEdit);

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

    // events from TradeManager
    void HandleShopOpened(ShopRuntimeState state)
    {
        currentShop = state;
        selectedStock = null;
        currentQuantity = 0;
        baseTotal = 0f;
        finalTotal = 0f;
        hasBargain = false;

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
        UpdatePlayerMoneyUI();
    }

    void HandleNpcResponse(NpcResponseType type, string line)
    {
        if (npcDialogueText != null && !string.IsNullOrEmpty(line))
            npcDialogueText.text = line;
    }

    void ClearSelectedFishUI()
    {
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

        if (quantityPanel != null)
            quantityPanel.SetActive(false);
    }

    // called by list items
    public void OnFishSelected(RuntimeFishStock stock)
    {
        if (priceLocked)
            return;

        selectedStock = stock;
        hasBargain = false;

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
            selectedFishBasePriceText.text = "Price: " + stock.unitPricePerKg.ToString("0") + " /kg";

        if (fishInfoText != null)
            fishInfoText.text = stock.fish.description;

        currentQuantity = 1;
        if (quantityInput != null)
            quantityInput.text = "1";

        if (quantityPanel != null)
            quantityPanel.SetActive(true);

        RecalculateBaseAndFinal();
    }

    // quantity and totals
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

        UpdateTotalsUI();
    }

    void UpdateTotalsUI()
    {
        if (baseTotalText != null)
            baseTotalText.text = "TOTAL: " + baseTotal.ToString("0");

        float discount = baseTotal - finalTotal;

        if (discountText != null)
        {
            if (hasBargain && discount > 0.01f)
                discountText.text = "PAZARLIK: -" + discount.ToString("0");
            else
                discountText.text = "PAZARLIK: 0";
        }

        if (finalTotalText != null)
            finalTotalText.text = "= " + finalTotal.ToString("0");
    }

    void UpdatePlayerMoneyUI()
    {
        if (playerMoneyText != null)
            playerMoneyText.text = playerMoney.ToString();
    }

    // bargain popup
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

        float minMul = currentShop != null && currentShop.data != null
            ? currentShop.data.bargainMinTotalMul
            : 0.6f;
        float maxMul = currentShop != null && currentShop.data != null
            ? currentShop.data.bargainMaxTotalMul
            : 1.0f;

        sliderMinTotal = baseTotal * minMul;
        sliderMaxTotal = baseTotal * maxMul;

        if (normalPriceText != null)
            normalPriceText.text = "Normal: " + baseTotal.ToString("0");
        if (minPriceText != null)
            minPriceText.text = "Min: " + sliderMinTotal.ToString("0");
        if (maxPriceText != null)
            maxPriceText.text = "Max: " + sliderMaxTotal.ToString("0");

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
        desiredPriceText.text = price.ToString("0");
    }

    void OnBargainConfirmClicked()
    {
        if (selectedStock == null || currentShop == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;

        float t = bargainSlider != null ? bargainSlider.value : 1f;
        float offeredTotal = Mathf.Lerp(sliderMinTotal, sliderMaxTotal, t);

        float acceptThreshold = baseTotal * 0.8f;
        bool accepted = offeredTotal >= acceptThreshold;

        if (accepted)
        {
            hasBargain = true;
            finalTotal = offeredTotal;

            // kilitle ve paneli tamamen kapat
            SetPriceLock(true);
            if (quantityPanel != null)
                quantityPanel.SetActive(false);

            UpdateTotalsUI();

            if (npcDialogueText != null)
                npcDialogueText.text = "Deal. That is acceptable.";
        }
        else
        {
            hasBargain = false;
            finalTotal = baseTotal;
            SetPriceLock(false);
            UpdateTotalsUI();

            if (npcDialogueText != null)
                npcDialogueText.text = "No, that is too low.";
        }

        CloseBargainPopup();
    }

    // accept / cancel / close
    void OnAcceptClicked()
    {
        if (bargainOverlay != null && bargainOverlay.activeSelf)
            return;

        if (TradeManager.Instance == null || selectedStock == null || currentShop == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        baseTotal = currentQuantity * selectedStock.unitPricePerKg;
        if (!hasBargain)
            finalTotal = baseTotal;

        int priceToPay = Mathf.CeilToInt(finalTotal);

        if (playerMoney < priceToPay)
        {
            if (npcDialogueText != null)
                npcDialogueText.text = "You do not have enough money.";
            return;
        }

        playerMoney -= priceToPay;
        UpdatePlayerMoneyUI();

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

        if (villagerInteraction != null)
            villagerInteraction.ExitConversation();
    }
}
