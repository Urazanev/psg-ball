using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemGUI : MonoBehaviour
{
    // Same-scene "singleton" pattern 
    private static ItemGUI _instance;
    public static ItemGUI instance
    {
        get
        {
            if (!_instance)
                _instance = FindObjectOfType<ItemGUI>();
            return _instance;
        }
    }
    
    #if UNITY_EDITOR
        [Header("Debug (editor only)")]
        public bool FreeShops;

        [SerializeField]
        bool ClearInventory;

        [SerializeField] 
        ItemEnumeration GiveItem;
    #endif

    [Header("Item Shop")]
    [SerializeField]
    Button RustyCrate;

    [SerializeField]
    Button BrassCrate;

    [SerializeField]
    Button GoldenCrate;

    [Header("Simplified Menu")]
    [SerializeField]
    bool UseSimplifiedMenu = true;

    [SerializeField]
    GameObject AchievementsPanel;

    [SerializeField]
    GameObject AchievementsLabel;

    [Header("Powerup Slots")]
    [SerializeField]
    List<Image> FirstItem;

    [SerializeField]
    List<Image> SecondItem;

    [SerializeField]
    List<Image> ThirdItem;

    [Header("Debug")]
    [SerializeField]
    bool VerbosePerkLogs = false;

    void Awake()
    {
        #if UNITY_EDITOR
            GiveItem = ItemEnumeration.NoItem;
        #endif

        RustyCrate.onClick.AddListener(() => Inventory.PurchaseItem(3, Crates.Rusty));
        BrassCrate.onClick.AddListener(() => Inventory.PurchaseItem(7, Crates.Brass));
        GoldenCrate.onClick.AddListener(() => Inventory.PurchaseItem(7, Crates.Brass));

        ConfigureSimplifiedMenu();
    }

    #if UNITY_EDITOR
        void Update()
        {
            if(ClearInventory)
            {
                Inventory.Clear();
                ClearInventory = false;
            }

            if(GiveItem != ItemEnumeration.NoItem)
            {
                Inventory.GiveItem(Items.GetItemFromEnumeration(GiveItem));
                GiveItem = ItemEnumeration.NoItem;
            }
        }
    #endif

    // Method to be used in an event system on the inspector
    public void GetItemInfo(string _item)
    {
        if(!ItemEnumeration.TryParse(_item, out ItemEnumeration item)) return;

        Item itemFound = Items.GetItemFromEnumeration(item);

        PlayerGUI.instance.InfoName.text = itemFound.Name;
        PlayerGUI.instance.InfoDescription.text = itemFound.Description;
    }

    public void GetSlotInfo(int slot)
    {
        if(slot < 0 || slot >= Inventory.Slots.Length) return;

        PlayerGUI.instance.InfoName.text = Inventory.Slots[slot].Name;
        PlayerGUI.instance.InfoDescription.text = Inventory.Slots[slot].Description;
    }

    public void LoadItems()
    {
        EnsureSlotIconsBound();
        LoadItem(Inventory.Slots[0], FirstItem);
        LoadItem(Inventory.Slots[1], SecondItem);
        LoadItem(Inventory.Slots[2], ThirdItem);

        if (VerbosePerkLogs)
            LogPerkState("LoadItems");
    }

    public void PurchaseSound() => GUIAudio.Speaker.PlayOneShot(GUIAudio.PurchaseSound);

    public void PurchaseFailSound() => GUIAudio.Speaker.PlayOneShot(GUIAudio.PurchaseFailSound);

    void LoadItem(Item item, List<Image> itemSlot)
    {
        if (itemSlot == null) return;
        Sprite icon = item != null ? item.Icon : null;

        bool hasIcon = icon != null;
        for (int i = 0; i < itemSlot.Count; i++)
        {
            Image image = itemSlot[i];
            if (image == null) continue;

            image.gameObject.SetActive(hasIcon);
            image.enabled = hasIcon;
            image.sprite = icon;
            if (hasIcon)
            {
                Color color = image.color;
                if (color.a < 0.999f)
                {
                    color.a = 1f;
                    image.color = color;
                }
            }
        }
    }

    void EnsureSlotIconsBound()
    {
        EnsureItemIcon(Inventory.Slots[0]);
        EnsureItemIcon(Inventory.Slots[1]);
        EnsureItemIcon(Inventory.Slots[2]);
    }

    static void EnsureItemIcon(Item item)
    {
        if (item == null) return;
        if (item is NoItem) return;
        if (item.Icon != null) return;

        Sprite fallbackIcon = ResolveItemIcon(item);
        if (fallbackIcon != null)
            item.Icon = fallbackIcon;
    }

    static Sprite ResolveItemIcon(Item item)
    {
        if (item == null || Items.instance == null) return null;

        if (item == Items.AngelWings) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_angelwings");
        if (item == Items.ExtraBall) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_extraball");
        if (item == Items.HealthBonus) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_healthbonus");
        if (item == Items.PingPong) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_pingpong");
        if (item == Items.TicketPrize) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_ticketprize");
        if (item == Items.Fireball) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_fireball");
        if (item == Items.WaterDroplet) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_waterdroplet");
        if (item == Items.LuckyCharm) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_luckycharm");
        if (item == Items.CurseOfAnubis) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_curseofanubis");
        if (item == Items.CameraFlip) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_cameraflip");
        if (item == Items.Rock) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_rock");
        if (item == Items.TennisBall) return Resources.Load<Sprite>("UI/Sprites/PSG-Ball/icon_tennisball");
        return null;
    }

    void LogPerkState(string source)
    {
        string s0 = DescribeItem(Inventory.Slots[0]);
        string s1 = DescribeItem(Inventory.Slots[1]);
        string s2 = DescribeItem(Inventory.Slots[2]);
        Debug.Log($"ItemGUI[{source}] slots: 0={s0}, 1={s1}, 2={s2}");

        LogTargetList("First", FirstItem);
        LogTargetList("Second", SecondItem);
        LogTargetList("Third", ThirdItem);
    }

    static string DescribeItem(Item item)
    {
        if (item == null) return "<null>";
        string iconName = item.Icon != null ? item.Icon.name : "<no-icon>";
        return $"{item.name}/{item.GetType().Name}/{iconName}";
    }

    static void LogTargetList(string label, List<Image> list)
    {
        if (list == null)
        {
            Debug.Log($"ItemGUI[{label}] targets: <null-list>");
            return;
        }

        for (int i = 0; i < list.Count; i++)
        {
            Image image = list[i];
            if (image == null)
            {
                Debug.Log($"ItemGUI[{label}] target[{i}]: <null>");
                continue;
            }

            RectTransform rect = image.rectTransform;
            string spriteName = image.sprite != null ? image.sprite.name : "<null>";
            Debug.Log(
                $"ItemGUI[{label}] target[{i}]: go={image.gameObject.name}, active={image.gameObject.activeInHierarchy}, enabled={image.enabled}, alpha={image.color.a:F2}, sprite={spriteName}, anchored=({rect.anchoredPosition.x:F1},{rect.anchoredPosition.y:F1})");
        }
    }

    void ConfigureSimplifiedMenu()
    {
        if(!UseSimplifiedMenu) return;

        if(AchievementsPanel == null)
        {
            GameObject achievementsPanel = GameObject.Find("Achievements Panel");
            if(achievementsPanel != null)
                AchievementsPanel = achievementsPanel;
        }

        if(AchievementsPanel != null)
            AchievementsPanel.SetActive(false);

        if(AchievementsLabel == null)
        {
            TMP_Text[] tmpLabels = Object.FindObjectsOfType<TMP_Text>(true);
            foreach(TMP_Text label in tmpLabels)
            {
                if(label != null && label.text == "Achievements")
                {
                    AchievementsLabel = label.gameObject;
                    break;
                }
            }

            if(AchievementsLabel == null)
            {
                Text[] legacyLabels = Object.FindObjectsOfType<Text>(true);
                foreach(Text label in legacyLabels)
                {
                    if(label != null && label.text == "Achievements")
                    {
                        AchievementsLabel = label.gameObject;
                        break;
                    }
                }
            }
        }

        if(AchievementsLabel != null)
            AchievementsLabel.SetActive(false);

        if(GoldenCrate != null)
            GoldenCrate.gameObject.SetActive(false);
    }
}
