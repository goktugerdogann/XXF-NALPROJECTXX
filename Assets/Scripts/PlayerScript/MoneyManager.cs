using System;
using UnityEngine;
using TMPro;

public class MoneyManager : MonoBehaviour
{
    public static MoneyManager Instance { get; private set; }

    [Header("Start money")]
    public int startingMoney = 100;

    [Header("UI")]
    public TMP_Text moneyText;

    public int CurrentMoney { get; private set; }

    public event Action<int> OnMoneyChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
       
        Instance = this;

        // temporary default, SaveManager will override on load
        if (CurrentMoney <= 0)
            CurrentMoney = startingMoney;

        UpdateUIAndFireEvent();
    }
   
    void UpdateUIAndFireEvent()
    {
        if (moneyText != null)
            moneyText.text = CurrentMoney.ToString();

        if (OnMoneyChanged != null)
            OnMoneyChanged(CurrentMoney);
    }

    public bool CanAfford(int amount)
    {
        return CurrentMoney >= amount;
    }

    public bool TrySpend(int amount)
    {
        if (!CanAfford(amount))
            return false;

        CurrentMoney -= amount;
        UpdateUIAndFireEvent();

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();

        return true;
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;

        CurrentMoney += amount;
        UpdateUIAndFireEvent();

        if (SaveManager.Instance != null)
            SaveManager.Instance.SaveGame();
    }

    public void SetMoney(int amount)
    {
        CurrentMoney = amount;
        UpdateUIAndFireEvent();
    }
   
    public void ResetToStartingMoney()
    {
        CurrentMoney = startingMoney;
        UpdateUIAndFireEvent();
    }
}
