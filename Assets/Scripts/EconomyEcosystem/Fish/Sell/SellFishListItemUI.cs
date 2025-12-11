using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Economy;

public class SellFishListItemUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;

    BuyerRuntimeFish fish;
    SellUIController owner;
    Button button;

    public void Setup(BuyerRuntimeFish runtimeFish, SellUIController ownerController)
    {
        fish = runtimeFish;
        owner = ownerController;

        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClicked);
        }

        if (fish != null && fish.fish != null)
        {
            if (iconImage != null)
                iconImage.sprite = fish.fish.icon;

            if (nameText != null)
                nameText.text = fish.fish.displayName;
        }
    }

    void OnClicked()
    {
        if (owner != null && fish != null)
        {
            owner.OnFishSelected(fish);
        }
    }
}
