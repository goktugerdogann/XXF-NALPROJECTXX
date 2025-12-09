using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Economy
{
    public class ShopFishListItemUI : MonoBehaviour
    {
        public Image iconImage;
        public TMP_Text nameText;

        RuntimeFishStock stock;
        ShopUIController owner;
        Button button;

        public void Setup(RuntimeFishStock runtimeStock, ShopUIController ownerController)
        {
            stock = runtimeStock;
            owner = ownerController;

            if (button == null)
                button = GetComponent<Button>();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }

            if (stock != null && stock.fish != null)
            {
                if (iconImage != null)
                    iconImage.sprite = stock.fish.icon;

                if (nameText != null)
                    nameText.text = stock.fish.displayName;
            }
        }

        void OnClicked()
        {
            if (owner != null && stock != null)
            {
                owner.OnFishSelected(stock);
            }
        }
    }
}
