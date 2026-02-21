# Perks Lite (working product baseline)

## Goal
Reduce cognitive load and RNG variance while keeping the core loop intact:
- play pinball
- earn tickets
- buy crates
- equip and use perks in slots

No perk/item code is deleted. This is a configuration-level simplification.

## Enabled Drop Pool
`UsePerksLiteLootTables = true` in `Assets/Scripts/Main Scripts/Inventory.cs`.

### Rusty Crate (3 tickets)
- CameraFlip
- PingPong
- TennisBall
- HealthBonus
- TicketPrize

### Brass Crate (6 tickets)
- CameraFlip
- PingPong
- TennisBall
- AngelWings
- ExtraBall
- HealthBonus
- TicketPrize

### Golden Crate (10 tickets)
- AngelWings
- ExtraBall
- HealthBonus
- TicketPrize

## Disabled from Active Drops
These still exist in project code/assets, but are excluded from crate loot in Lite mode:
- Fireball
- WaterDroplet
- Rock
- LuckyCharm
- CurseOfAnubis

## Menu Simplification
`Assets/Scripts/Main Scripts/ItemGUI.cs` now has:
- `UseSimplifiedMenu` (default: true)
- hides `Achievements Panel`
- hides `Golden Crate` button

This removes early UI noise while preserving systems in code.

## Why this cut works
- Keeps only understandable, mostly positive effects.
- Leaves inventory/slots economy in place.
- Reduces frustration from hard-negative perks.
- Keeps a clear upgrade ladder (Rusty -> Brass -> Golden), even if Golden is hidden by default in UI for early product testing.

## Toggle Back
- Full original drop tables: set `UsePerksLiteLootTables = false`.
- Full menu: set `UseSimplifiedMenu = false` and/or `HideGoldenCrate = false` in `ItemGUI`.
