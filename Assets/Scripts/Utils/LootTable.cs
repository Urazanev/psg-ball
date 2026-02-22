using System.Collections.Generic;
using UnityEngine;

public static class LootTable
{
    struct WeightedDrop
    {
        public Item Item;
        public int Weight;
    }

    public static Item GetBasicDrop() => GetDrop(Crates.Rusty);

    public static Item GetProDrop() => GetDrop(Crates.Brass);

    public static Item GetRustyDrop() => GetBasicDrop();

    public static Item GetBrassDrop() => GetProDrop();

    public static Item GetGoldenDrop() => GetProDrop();

    public static Item GetDrop(Crates crateType)
    {
        List<WeightedDrop> weightedDrops = BuildDrops(crateType);
        if (weightedDrops.Count == 0)
            return Items.NoItem;

        int totalWeight = 0;
        for (int i = 0; i < weightedDrops.Count; i++)
            totalWeight += Mathf.Max(0, weightedDrops[i].Weight);

        if (totalWeight <= 0)
            return Items.NoItem;

        int roll = Random.Range(0, totalWeight);
        int cumulative = 0;
        for (int i = 0; i < weightedDrops.Count; i++)
        {
            cumulative += Mathf.Max(0, weightedDrops[i].Weight);
            if (roll < cumulative)
                return weightedDrops[i].Item;
        }

        return weightedDrops[weightedDrops.Count - 1].Item ?? Items.NoItem;
    }

    static List<WeightedDrop> BuildDrops(Crates crateType)
    {
        switch (crateType)
        {
            case Crates.Rusty:
                return new List<WeightedDrop>
                {
                    new WeightedDrop { Item = Items.PingPong, Weight = 35 },
                    new WeightedDrop { Item = Items.TicketPrize, Weight = 40 },
                    new WeightedDrop { Item = Items.HealthBonus, Weight = 25 },
                };
            case Crates.Brass:
            case Crates.Golden:
                return new List<WeightedDrop>
                {
                    new WeightedDrop { Item = Items.ExtraBall, Weight = 35 },
                    new WeightedDrop { Item = Items.AngelWings, Weight = 40 },
                    new WeightedDrop { Item = Items.HealthBonus, Weight = 25 },
                };
            default:
                return new List<WeightedDrop>();
        }
    }
}
