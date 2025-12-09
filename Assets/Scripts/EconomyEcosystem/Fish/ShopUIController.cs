using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Economy; // TradeManager, ShopData, ShopRuntimeState, RuntimeFishStock, NpcResponseType

public class ShopUIController : MonoBehaviour
{
    [Header("Root")]
    public GameObject rootPanel;        // ShopMainPanel veya ShopRootPanel
    public TMP_Text shopNameText;
    public Button closeButton;

    [Header("Left list")]
    public Transform fishListContent;          // ScrollView Content
    public ShopFishListItemUI fishListItemPrefab;

    [Header("Center panel - selected fish")]
    public Image selectedFishIcon;
    public TMP_Text selectedFishNameText;
    public TMP_Text selectedFishStockText;
    public TMP_Text selectedFishBasePriceText;
    public TMP_Text fishInfoText;

    [Header("Quantity")]
    public TMP_InputField quantityInput;
    public Button quantityMinusButton;
    public Button quantityPlusButton;

    [Header("Totals")]
    public TMP_Text baseTotalText;     // "TOTAL: 0"
    public TMP_Text discountText;      // "PAZARLIK: 0"
    public TMP_Text finalTotalText;    // "= 0"

    [Header("Right panel - npc")]
    public Image npcPortrait;
    public TMP_Text npcDialogueText;

    [Header("Bottom bar")]
    public Button bargainButton;      // Offer
    public Button acceptButton;       // Accept
    public Button cancelButton;       // Cancel

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

    // Runtime state
    ShopRuntimeState currentShop;
    RuntimeFishStock selectedStock;
    int currentQuantity = 0;

    float baseTotal = 0f;
    float finalTotal = 0f;
    bool hasBargain = false;

    float sliderMinTotal = 0f;
    float sliderMaxTotal = 0f;

    // Fiyat kilitli mi? (npc teklifi kabul ettikten sonra true)
    bool priceLocked = false;

    void Awake()
    {
        // Button listeners
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

        if (bargainSlider != null)
        {
            bargainSlider.minValue = 0f;
            bargainSlider.maxValue = 1f;
            bargainSlider.onValueChanged.AddListener(OnBargainSliderChanged);
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

    // =====================================================
    // Helper: fiyat kilidi
    // =====================================================

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
            bargainButton.interactable = canEdit;
    }

    // =====================================================
    // Event handlers from TradeManager
    // =====================================================

    void HandleShopOpened(ShopRuntimeState state)
    {
        currentShop = state;
        selectedStock = null;
        currentQuantity = 0;
        baseTotal = 0f;
        finalTotal = 0f;
        hasBargain = false;
        SetPriceLock(false);

        if (rootPanel != null)
            rootPanel.SetActive(true);

        if (shopNameText != null && state.data != null)
            shopNameText.text = state.data.displayName;

        // Npc portrait
        if (npcPortrait != null && state.data != null)
            npcPortrait.sprite = state.data.npcPortrait;

        // Default npc line bos
        if (npcDialogueText != null)
            npcDialogueText.text = "";

        // Clear selected fish panel
        ClearSelectedFishUI();

        // Clear left list
        if (fishListContent != null)
        {
            for (int i = fishListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(fishListContent.GetChild(i).gameObject);
            }
        }

        // Populate fish list
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
    }

    // =====================================================
    // Called by list items
    // =====================================================

    public void OnFishSelected(RuntimeFishStock stock)
    {
        selectedStock = stock;
        hasBargain = false;
        SetPriceLock(false);

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
            selectedFishBasePriceText.text = "Price: " + stock.unitPricePerKg.ToString("0.0") + " /kg";

        if (fishInfoText != null)
            fishInfoText.text = stock.fish.description;

        currentQuantity = 1;
        if (quantityInput != null)
            quantityInput.text = "1";

        RecalculateBaseAndFinal();
    }

    // =====================================================
    // Quantity and totals
    // =====================================================

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

    // =====================================================
    // Bargain popup
    // =====================================================

    void OnBargainClicked()
    {
        if (selectedStock == null)
            return;
        if (priceLocked)
            return;

        OpenBargainPopup();
    }

    void OpenBargainPopup()
    {
        if (bargainOverlay == null || selectedStock == null)
            return;

        if (currentQuantity <= 0)
            currentQuantity = 1;

        // Base total for this quantity
        baseTotal = currentQuantity * selectedStock.unitPricePerKg;
        if (!hasBargain)
            finalTotal = baseTotal;

        // Slider range: 0.6x - 1.0x
        sliderMinTotal = baseTotal * 0.6f;
        sliderMaxTotal = baseTotal;

        if (normalPriceText != null)
            normalPriceText.text = "Normal: " + baseTotal.ToString("0");

        if (minPriceText != null)
            minPriceText.text = "Min: " + sliderMinTotal.ToString("0");

        if (maxPriceText != null)
            maxPriceText.text = "Max: " + sliderMaxTotal.ToString("0");

        if (bargainSlider != null)
            bargainSlider.value = 1f; // Start at normal price

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

        // Base total tekrar hesap
        baseTotal = currentQuantity * selectedStock.unitPricePerKg;

        float t = bargainSlider != null ? bargainSlider.value : 1f;
        float offeredTotal = Mathf.Lerp(sliderMinTotal, sliderMaxTotal, t);

        // Basit kabul kurali: normal fiyatin en az %80'i ise kabul et
        float acceptThreshold = baseTotal * 0.8f;
        bool accepted = offeredTotal >= acceptThreshold;

        if (accepted)
        {
            hasBargain = true;
            finalTotal = offeredTotal;
            SetPriceLock(true);          // artik kg degismez, offer acilmaz
            UpdateTotalsUI();

            if (npcDialogueText != null)
                npcDialogueText.text = "Deal. That is acceptable.";
        }
        else
        {
            // Pazarlik gecersiz, eski fiyat korunur
            hasBargain = false;
            finalTotal = baseTotal;
            SetPriceLock(false);         // tekrar deneyebilir
            UpdateTotalsUI();

            if (npcDialogueText != null)
                npcDialogueText.text = "No, that is too low.";
        }

        CloseBargainPopup();
    }

    // =====================================================
    // Accept / Cancel / Close
    // =====================================================

    void OnAcceptClicked()
    {
        // Popup acikken accept calismasin
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

        // Pay
        playerMoney -= priceToPay;
        UpdatePlayerMoneyUI();

        // Notify TradeManager (stock, quantity, final price)
        TradeManager.Instance.CompleteTrade(selectedStock, currentQuantity, finalTotal);

        // Close shop and exit conversation
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
