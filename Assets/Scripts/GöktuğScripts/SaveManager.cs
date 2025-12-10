using System.IO;
using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("Refs")]
    public ItemDatabase itemDatabase;
    public Transform playerTransform;
    public float loadSpawnYOffset = 0.1f;

    [System.Serializable]
    public class InventorySlotData
    {
        public string itemId;
        public int amount;
    }

    [System.Serializable]
    public class WorldItemData
    {
        public string itemId;
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class PlayerData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    [System.Serializable]
    public class SaveData
    {
        public int money;  // saved money
        public List<InventorySlotData> inventory = new List<InventorySlotData>();
        public List<WorldItemData> worldItems = new List<WorldItemData>();
        public PlayerData player = new PlayerData();
    }

    string SavePath
    {
        get { return Path.Combine(Application.persistentDataPath, "placements.json"); }
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        LoadGame();
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();

        // inventory
        if (Inventory.Instance != null)
        {
            var inv = Inventory.Instance;

            for (int i = 0; i < inv.slots.Count; i++)
            {
                var slot = inv.slots[i];
                InventorySlotData s = new InventorySlotData();

                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    s.itemId = slot.item.id;
                    s.amount = slot.amount;
                }
                else
                {
                    s.itemId = "";
                    s.amount = 0;
                }

                data.inventory.Add(s);
            }
        }

        // world items
        PickupItem[] worldItems = FindObjectsOfType<PickupItem>();
        foreach (var p in worldItems)
        {
            if (p.itemData == null) continue;
            if (!p.gameObject.activeInHierarchy) continue;

            WorldItemData w = new WorldItemData
            {
                itemId = p.itemData.id,
                position = p.transform.position,
                rotation = p.transform.rotation
            };
            data.worldItems.Add(w);
        }

        // player
        if (playerTransform != null)
        {
            data.player.position = playerTransform.position;
            data.player.rotation = playerTransform.rotation;
        }

        // money
        if (MoneyManager.Instance != null)
        {
            data.money = MoneyManager.Instance.CurrentMoney;
        }
        else
        {
            data.money = 0;
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);

        Debug.Log("SaveManager: saved " +
                  data.inventory.Count + " inventory slots, " +
                  data.worldItems.Count + " world items, " +
                  "money=" + data.money + ". Path: " + SavePath);
    }

    public void LoadGame()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("SaveManager: no save file yet, starting fresh.");

            // no save file, use starting money
            if (MoneyManager.Instance != null)
                MoneyManager.Instance.ResetToStartingMoney();

            return;
        }

        string json = File.ReadAllText(SavePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            Debug.LogWarning("SaveManager: save file invalid.");

            if (MoneyManager.Instance != null)
                MoneyManager.Instance.ResetToStartingMoney();

            return;
        }

        // inventory
        if (Inventory.Instance != null && data.inventory != null && data.inventory.Count > 0)
        {
            var inv = Inventory.Instance;

            while (inv.slots.Count < data.inventory.Count)
                inv.slots.Add(new InventorySlot());

            for (int i = 0; i < data.inventory.Count && i < inv.slots.Count; i++)
            {
                var s = data.inventory[i];
                var slot = inv.slots[i];

                if (string.IsNullOrEmpty(s.itemId) || s.amount <= 0)
                {
                    slot.item = null;
                    slot.amount = 0;
                }
                else
                {
                    ItemData item = itemDatabase != null ? itemDatabase.GetItemById(s.itemId) : null;
                    if (item == null)
                    {
                        slot.item = null;
                        slot.amount = 0;
                        Debug.LogWarning("SaveManager: itemId not found in database: " + s.itemId);
                    }
                    else
                    {
                        slot.item = item;
                        slot.amount = s.amount;
                    }
                }
            }

            InventoryUI.Instance?.UpdateUI();
        }

        // clear existing world items
        var existing = FindObjectsOfType<PickupItem>();
        foreach (var p in existing)
        {
            if (!p.saveToWorld) continue;
            Destroy(p.gameObject);
        }

        // spawn saved world items
        if (data.worldItems != null)
        {
            foreach (var w in data.worldItems)
            {
                if (string.IsNullOrEmpty(w.itemId)) continue;

                ItemData item = itemDatabase != null ? itemDatabase.GetItemById(w.itemId) : null;
                if (item == null || item.worldPrefab == null)
                {
                    Debug.LogWarning("SaveManager: cannot spawn world item, missing data for id: " + w.itemId);
                    continue;
                }

                GameObject obj = Object.Instantiate(item.worldPrefab, w.position, w.rotation);

                PickupItem pi = obj.GetComponent<PickupItem>();
                if (pi == null)
                    pi = obj.AddComponent<PickupItem>();

                pi.itemData = item;
                if (pi.amount <= 0) pi.amount = 1;

                pi.saveToWorld = true;
            }
        }

        // player
        if (playerTransform != null && data.player != null)
        {
            Vector3 pos = data.player.position + Vector3.up * loadSpawnYOffset;
            playerTransform.position = pos;
            playerTransform.rotation = data.player.rotation;
        }

        // money
        if (MoneyManager.Instance != null)
        {
            if (data.money > 0)
                MoneyManager.Instance.SetMoney(data.money);
            else
                MoneyManager.Instance.ResetToStartingMoney();
        }

        Debug.Log("SaveManager: loaded " +
                  (data.inventory != null ? data.inventory.Count : 0) +
                  " inventory slots, " +
                  (data.worldItems != null ? data.worldItems.Count : 0) +
                  " world items, money=" + data.money + ".");
    }
}
