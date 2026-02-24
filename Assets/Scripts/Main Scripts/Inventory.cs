using UnityEngine;
using System.Collections.Generic;

public static class Inventory
{
    const float ToyCoreGlossSmoothness = 0.88f;

    static readonly Dictionary<Material, Material> GlossBallMaterialCache = new Dictionary<Material, Material>();

    public static Item[] Slots = new Item[3] { Items.NoItem, Items.NoItem, Items.NoItem };
    public static Item Equipped = Items.NoItem;
    public static Item NextItem
    {
        get => Slots[0];
    }

    public static void GiveItem(Item item)
    {
        // Checks where to add the item (from the first to the last slot)
        int position = -1;
        for (int i = 0; i < Slots.Length; i++)
            if(Slots[i] == Items.NoItem)
            {
                position = i;
                break;
            }
        
        if(position == -1)
        {
            ItemGUI.instance.PurchaseFailSound();
            return;
        }

        Slots[position] = item;

        // Post Inventory Changes
        InventoryChanged();
    }

    private static byte goldenCount = 0;

    public static void PurchaseItem(int price, Crates rarity)
    {
        
        // Can the item be afforded
        #if UNITY_EDITOR
            if(Player.instance.Tickets < price && !ItemGUI.instance.FreeShops)
        #else
            if(Player.instance.Tickets < price)
        #endif
        {
            ItemGUI.instance.PurchaseFailSound();
            return;
        }

        // Checks where to add the item (from the first to the last slot)
        int position = -1;
        for (int i = 0; i < Slots.Length; i++)
            if(Slots[i] == Items.NoItem)
            {
                position = i;
                break;
            }

        // No suitable position available
        if(position == -1)
        {
            ItemGUI.instance.PurchaseFailSound();
            return;
        }

        // Achievement progression is disabled in the simplified product flow.
        if(rarity == Crates.Golden && ++goldenCount >= 3)
            goldenCount = 0;
        else if(rarity != Crates.Golden) goldenCount = 0;

        // Subtracts the price from the ticket count
        #if UNITY_EDITOR
            if(!ItemGUI.instance.FreeShops)
        #endif
                Player.instance.Tickets -= price;

        Slots[position] = GenerateItemFromRarity(rarity);
        ItemGUI.instance.PurchaseSound();

        // Post Inventory Changes
        InventoryChanged();
    }

    public static void Unequip()
    {
        Equipped.OnUnequip();
        Equipped = Items.NoItem;
    }

    public static void UseItem()
    {
        // If there are no balls in the field to apply the item to
        if(Field.instance.BallsInField.Count <= 0) return;

        // If inventory is empty or an item is already being utilised
        if(NextItem == Items.NoItem || Equipped != Items.NoItem) return;
        Equipped = NextItem;

        // Uses powerup
        Slots[0] = Slots[1];
        Slots[1] = Slots[2];
        Slots[2] = Items.NoItem;

        // Activates powerup
        // Method called for all items on activation
        foreach(Rigidbody ball in Field.instance.BallsInField)
        {
            if (ball == null) continue;

            if(Equipped.HasTrail)
            {
                TrailRenderer trail = ball.GetComponent<TrailRenderer>();
                if (trail != null)
                {
                    trail.enabled = true;
                    trail.material = Equipped.TrailMaterial;
                }
            }

            if(Equipped.ChangeBallMaterial)
            {
                MeshRenderer meshRenderer = ball.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                    meshRenderer.material = GetToyCoreGlossBallMaterial(Equipped.PoweredUpMaterial);
            }

            if(Equipped.HasCustomPhysicMaterial)
            {
                SphereCollider sphereCollider = ball.GetComponent<SphereCollider>();
                if (sphereCollider != null)
                    sphereCollider.material = Equipped.CustomPhysicMaterial;
            }
        }

        Equipped.OnEquip();

        // Post Inventory Changes
        InventoryChanged();

        // Plays sound from the board
        Field.instance.PowerupSound();
    }

    public static void GetMemory()
    {
        for (int i = 0; i < Slots.Length; i++)
        {
            try
            {
                Slots[i] = Items.GetItemFromEnumeration((ItemEnumeration) PlayerPrefs.GetInt($"Item{i}", 0));
            }
            catch(System.InvalidCastException ex)
            {
                Debug.LogError(ex.Message);
                Slots[i] = Items.NoItem;
            }
        }

        ItemGUI.instance.LoadItems();
    }

    public static void SetMemory()
    {
        for (int i = 0; i < Slots.Length; i++)
            PlayerPrefs.SetInt($"Item{i}", (int) Items.GetEnumerationFromItem(Slots[i]));
    }

    public static void Clear()
    {
        // Unequips current item and resets the item queue
        Unequip();
        Slots = new Item[3] { Items.NoItem, Items.NoItem, Items.NoItem };

        // Post Inventory Changes
        InventoryChanged();
    }

    static void InventoryChanged()
    {
        SetMemory();
        ItemGUI.instance.LoadItems();
    }

    struct ItemIncidence
    {
        public Item item;
        public int incidence;
    }

    // Incidence-based random item generator
    static Item GenerateItemFromRarity(Crates rarity)
    {
        List<ItemIncidence> lootTable = new List<ItemIncidence>();
        switch(rarity)
        {
            // Basic Box
            case Crates.Rusty:
                lootTable = new List<ItemIncidence>()
                {
                    new ItemIncidence(){ item = Items.PingPong, incidence = 35 },
                    new ItemIncidence(){ item = Items.TicketPrize, incidence = 40 },
                    new ItemIncidence(){ item = Items.HealthBonus, incidence = 25 }
                };
                break;

            // Pro Box (Golden reuses Pro table to keep legacy calls safe)
            case Crates.Brass:
            case Crates.Golden:
                lootTable = new List<ItemIncidence>()
                {
                    new ItemIncidence(){ item = Items.ExtraBall, incidence = 35 },
                    new ItemIncidence(){ item = Items.AngelWings, incidence = 40 },
                    new ItemIncidence(){ item = Items.HealthBonus, incidence = 25 }
                };
                break;
        }

        // Return Item from incidence
        // this is O(2n), where n = number of items inside lootTable
        int totalIncidence = 0;
        for (int i = 0; i < lootTable.Count; i++)
        {
            totalIncidence += lootTable[i].incidence;
            lootTable[i] = new ItemIncidence(){ item = lootTable[i].item, incidence = totalIncidence };
        }

        // Chooses an item inside given lootTable's incidence range
        int itemChosen = Random.Range(0, totalIncidence);

        // Returns what item does the lootTable references for the generated random index
        for (int i = 0; i < lootTable.Count; i++)
        {
            int lastInterval = 0;

            // If it's not the first entry check for the previous entry
            if(i > 0) lastInterval = lootTable[i-1].incidence;

            // Checks if the generated number is inside the interval
            if(itemChosen >= lastInterval && itemChosen < lootTable[i].incidence)
                return lootTable[i].item;
        }

        return Items.NoItem;
    }

    static Material GetToyCoreGlossBallMaterial(Material sourceMaterial)
    {
        if (sourceMaterial == null) return null;

        if (GlossBallMaterialCache.TryGetValue(sourceMaterial, out Material cached) && cached != null)
            return cached;

        Material glossMaterial = new Material(sourceMaterial);
        glossMaterial.name = $"{sourceMaterial.name}_ToyCoreGloss_Runtime";
        ApplyToyCoreGlossSettings(glossMaterial);
        GlossBallMaterialCache[sourceMaterial] = glossMaterial;
        return glossMaterial;
    }

    static void ApplyToyCoreGlossSettings(Material material)
    {
        if (material == null) return;

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", ToyCoreGlossSmoothness);
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", ToyCoreGlossSmoothness);

        if (material.HasProperty("_SpecularHighlights"))
            material.SetFloat("_SpecularHighlights", 1f);
        if (material.HasProperty("_EnvironmentReflections"))
            material.SetFloat("_EnvironmentReflections", 1f);
        if (material.HasProperty("_GlossyReflections"))
            material.SetFloat("_GlossyReflections", 1f);
    }
}
