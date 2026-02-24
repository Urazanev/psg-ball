# Simple Pinball: Detailed Guide to Mechanics, Score, Items, and Achievements

This document is based on the actual project logic (scripts and prefab data), not assumptions.

## 1) How the gameplay loop works

1. Pressing `Play` starts `GameStart()`:
- grants the `Getting Started` achievement;
- sets `Tilt = false`;
- sets `Lives = 4`;
- sets `Score = 0`.

2. The first ball is not spawned instantly by pressing `Play`.
- Ball spawn happens in the player's `Update()` when the field is empty for about `0.8` seconds.

3. Ball loss:
- when the ball leaves the field trigger, it is destroyed;
- if there are still no balls on the field after `0.8` sec, 1 life is removed (`Lives -= 1`), current item is unequipped, then a new ball is spawned (if lives are not below zero).

4. End of game:
- if after another loss `Lives < 0`, the `Game Over` screen is shown.

## 2) Important life-count nuance

- The game starts with `Lives = 4`, but one active ball already exists in the round.
- In practice, this gives 5 ball attempts (current ball + 4 "in reserve"), then `Lives` goes to `-1` and the game ends.

## 3) Controls

- `Space` (hold/release): plunger (charge/launch).
- `A` or `Left Arrow`: left flipper.
- `D` or `Right Arrow`: right flipper.
- `Left Shift`: table nudge to the left.
- `Right Shift`: table nudge to the right.
- `Left Ctrl` (or `Left Cmd` on macOS): use the next item from inventory.
- `Esc`: pause.

If `Tilt` is triggered, flipper/nudge/item-use controls are blocked until the current ball is lost (`Tilt` resets on next respawn).

## 4) Score system

Score is granted on collisions with objects that have `ScoringObject`:

- `Slingshot`: `+25`
- `Bumper`: `+50`
- `Flag`: `+61`

Score add formula:

```text
Add = floor(ObjectBaseScore * CurrentMultiplier)
```

Example:
- Bumper hit at `x1.0` => `+50`
- Bumper hit at `x2.3` => `+115`

## 5) Score multiplier

Default behavior:
- every `10` seconds, while there is at least one moving ball on the field, multiplier increases by `+0.1`.

Multiplier reset:
- multiplier is reset to `1.0` every time `Lives` changes.

This happens:
- on game start (`Lives = 4`),
- on ball death (`Lives -= 1`),
- on any effect that adds/changes lives (for example `Health Bonus`, `Curse of Anubis`).

## 6) Table nudge and Tilt

Parameters:
- nudge force: `2` (applied to all balls on the field);
- base Tilt chance: `1 out of 5` (20%) per nudge.

Logic:
- on `LSHIFT/RSHIFT`, all balls receive impulse left/right;
- then Tilt is rolled randomly.

If Tilt happens:
- `Player.Tilt = true`;
- controls (except plunger) are blocked until ball loss.

## 7) Plunger (`Space`)

Parameters:
- `MaxForce = 16`
- `MinForce = 0`
- `IncreasingFactor = 0.2` per hold frame

Behavior:
- while holding `Space`, force grows;
- if force crosses `MaxForce`, fail sound plays and force is reduced by a random factor around `0.5..0.7`;
- on `Space` release, accumulated force is applied forward to balls inside the plunger zone.

## 8) Tickets and economy

### 8.1 How tickets are earned

A threshold logic based on score and multiplier:

```text
If Score > lastTicketIncrement + (500 * Multiplier)
=> Tickets += 1
=> lastTicketIncrement += 500 * Multiplier
```

Details:
- strict check (`>`), not `>=`;
- max 1 ticket per score update;
- tickets are persisted between sessions (`PlayerPrefs` key `ticketCount`).

### 8.2 Crate shop

Prices:
- Rusty Crate: `3` tickets
- Brass Crate: `6` tickets
- Golden Crate: `10` tickets

Purchase fails if:
- not enough tickets;
- all 3 inventory slots are occupied.

On failed purchase, tickets are not spent.

## 9) Inventory and item activation

- Inventory: 3 slots (queue).
- Always uses the **first** slot.
- After use, queue shifts left.

Usage limits:
- at least 1 ball must be on the field;
- cannot activate a new item while previous one is still equipped.

Effect duration:
- item stays active until next ball loss (when `Inventory.Unequip()` is called).

Applied to balls:
- at activation time, all current balls may receive:
  - trail material,
  - ball visual material,
  - ball physics material.

## 10) Crate loot: exact odds

### 10.1 Rusty Crate (total incidence = 20)

- Fireball: `4/20 = 20%`
- Water Droplet: `4/20 = 20%`
- Camera Flip: `3/20 = 15%`
- Health Bonus: `1/20 = 5%`
- Ping Pong: `3/20 = 15%`
- Rock: `4/20 = 20%`
- Tennis Ball: `1/20 = 5%`

### 10.2 Brass Crate (total incidence = 20)

- Fireball: `10%`
- Water Droplet: `5%`
- Lucky Charm: `20%`
- Curse of Anubis: `5%`
- Angel Wings: `10%`
- Camera Flip: `5%`
- Extra Ball: `15%`
- Health Bonus: `5%`
- Tennis Ball: `10%`
- Ticket Prize: `15%`

### 10.3 Golden Crate (total incidence = 7)

- Curse of Anubis: `1/7 ≈ 14.29%`
- Angel Wings: `2/7 ≈ 28.57%`
- Extra Ball: `2/7 ≈ 28.57%`
- Health Bonus: `1/7 ≈ 14.29%`
- Ticket Prize: `1/7 ≈ 14.29%`

## 11) All items: actual behavior

Below is the behavior that really exists in code.

### 11.1 Fireball

- OnEquip: no additional logic.
- Passive via materials:
  - changes visuals;
  - applies `Fireball` physics material (`bounciness: 0.8`, `bounceCombine: Max`).

### 11.2 Water Droplet

- OnEquip: no additional logic.
- Passive via materials:
  - changes visuals;
  - applies `WaterDroplet` physics material (`bounciness: 0.5`, `bounceCombine: Average`).

### 11.3 Lucky Charm

- OnEquip: no additional logic.
- On every ball `OnCollisionExit`:
  - `Multiplier = Random.Range(0.01f, 3.6f)`.
- Effect is highly unstable: multiplier can sharply rise or drop.

### 11.4 Curse of Anubis

- On activation:
  - `Lives = 0` (resets multiplier to `1.0`),
  - `Score /= 2`,
  - `Multiplier += 2.5` (usually becomes `3.5` right after activation),
  - main light intensity drops to `0.25`.
- On unequip:
  - light intensity returns to `0.8`.
- Also changes physics material/visuals/trail.

### 11.5 Angel Wings

- On activation, for each ball:
  - clears velocity and angular velocity,
  - teleports ball to spawnpoint.
- Useful for recovering from bad ball position.

### 11.6 Camera Flip

- On activation:
  - camera flips (`z = 180`),
  - `Multiplier += 1.5`.
- On unequip:
  - camera returns to normal angle.

### 11.7 Extra Ball

- On activation: spawns an additional ball.
- Important effect: life is removed only when **no** balls remain on the field, so multi-ball increases survivability.

### 11.8 Health Bonus

- On activation: `Lives += 1`.
- On each score event (`OnScoring`):
  - `1/50` chance (2%) to grant another `+1` life.
- Important nuance: any `Lives` change resets multiplier to `1.0`.

### 11.9 Ping Pong

- On activation, boosts flippers:
  - `FlipperMotorVelocity: 1500 -> 2500`
  - `FlipperMotorForce: 150 -> 250`
- On unequip, restores default values.

### 11.10 Rock

- OnEquip: no extra logic.
- Passive via `Rock` physics material:
  - `bounciness: 0`,
  - ball feels "heavier" on rebounds.

### 11.11 Tennis Ball

- On activation:
  - `TiltChance: 5 -> 20`.
- This means nudge Tilt chance changes from `20%` to `5%`.
- Also changes physics material/visuals.

### 11.12 Ticket Prize

- On activation: instantly grants `Random.Range(3, 6)` => `3..5` tickets.
- On each score event:
  - `1/10` chance (10%) to grant `+1` ticket.

## 12) Achievements

## 12.1 What is important to understand

- Each achievement has `Constraints` (prerequisites).
- If prerequisites are not met, achievement will not unlock even if the "main" condition is completed.
- Achievement icons in UI are often hidden until achievement becomes unlockable.

### 12.2 Achievement list, conditions and dependencies

1. `Getting Started` (Common)
- Condition: start the first game.
- Prerequisites: none.

2. `Ticket Apprentice` (Common)
- Condition: get the first ticket.
- Prerequisites: `Getting Started`.

3. `Ticket Master` (Common)
- Condition: have 10 tickets.
- Prerequisites: `Getting Started`, `Ticket Apprentice`.

4. `Ticket Hoarder` (Rare)
- Condition: have 100 tickets.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Ticket Master`.

5. `Ticket Maniac` (Legendary)
- Condition: have 1000 tickets.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Ticket Master`, `Ticket Hoarder`.

6. `Gambling Newbie` (Common)
- Condition: buy the first crate.
- Prerequisites: `Getting Started`, `Ticket Apprentice`.

7. `Gambling Expert` (Common)
- Condition: buy the first Golden Crate.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`.

8. `Gambling Tycoon` (Common)
- Condition: buy 3 Golden Crates in a row (streak resets if a non-golden crate is bought).
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`, `Gambling Expert`.

9. `Survivalist` (Common)
- Condition: reach multiplier `x3`.
- Prerequisites: `Getting Started`.

10. `Ninja` (Rare)
- Condition: reach multiplier `x5`.
- Prerequisites: `Getting Started`, `Survivalist`.

11. `Jackpot` (Common)
- Condition: 3 identical items in inventory slots (not `NoItem`).
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`.

12. `One of a Kind` (Rare)
- Condition: 3x `Curse of Anubis` in slots.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`, `Jackpot`.

13. `Straight Flush` (Rare)
- Condition: 3x `Health Bonus` in slots.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`, `Jackpot`.

14. `Pinball Wizard` (Legendary)
- Condition: reach `500000` score in one game.
- Prerequisites: `Getting Started`, `Ticket Apprentice`, `Gambling Newbie`, `Jackpot`, `Survivalist`, `Ninja`.

### 12.3 Achievement unlock nuances

- `One of a Kind` and `Straight Flush` in code require `Jackpot` to already be unlocked.
- If you first assemble three identical "special" items, often only `Jackpot` is granted first, and the special achievement must be repeated.

## 13) Progress persistence

Saved in `PlayerPrefs`:
- tickets (`ticketCount`),
- contents of 3 slots (`Item0`, `Item1`, `Item2`),
- achievement state (keys by achievement names).

Not persisted between restarts:
- `Recent Games` table (runtime memory only).

`Reset progress` button:
- runs `PlayerPrefs.DeleteAll()` and reloads scene.

## 14) Practical tips

1. For stable ticket farming, keep the ball moving as long as possible: multiplier grows over time and accelerates score gain.
2. For safer table nudges, use `Tennis Ball` (reduces Tilt chance from 20% to 5%).
3. `Extra Ball` is very strong for survivability: life is lost only when all balls are gone.
4. `Health Bonus` is useful, but often resets multiplier (because lives change).
5. For `Gambling Tycoon`, buy only Golden crates in sequence; any Rusty/Brass purchase resets the streak.
6. For `Pinball Wizard`, first complete prerequisite chain (`Survivalist` + `Ninja` + `Jackpot` + base achievements), otherwise a 500k run may not count immediately.
7. For `One of a Kind` and `Straight Flush`, unlock regular `Jackpot` first, then target the required triples.

---

If needed, I can provide a second version of this guide as a "1-screen quick cheat sheet" (only numbers, formulas, and optimal item builds).
