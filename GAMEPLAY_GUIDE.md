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
- on any effect that adds/changes lives (for example `Health Bonus`).

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

## 8) Balls and economy

### 8.1 How balls are earned

A threshold logic based on score and multiplier:

```text
If Score > lastTicketIncrement + (500 * Multiplier)
=> Balls += 1
=> lastTicketIncrement += 500 * Multiplier
```

Details:
- strict check (`>`), not `>=`;
- max 1 ball per score update;
- balls are persisted between sessions (`PlayerPrefs` key `ticketCount`, legacy key name).

### 8.2 Daily claim (wallet-linked)

- Daily reward amount: `+10` balls.
- Claim is available once per UTC day.
- Claim is stored per connected wallet address (separate key per wallet).
- If wallet is not connected, button shows `CONNECT FOR DAILY`.
- After claim, button shows `CLAIMED TODAY` until the next UTC day.

### 8.3 Capsule shop (PSG1 flow)

Current shop has exactly 2 capsule types:
- Basic Capsule: `3` balls.
- Premium Capsule: `7` balls.

Technical note:
- Internally these still map to legacy crate enums (`Rusty` and `Brass`), but in gameplay/UI they are capsules.

Purchase fails if:
- not enough balls;
- all 3 inventory slots are occupied.

On failed purchase, balls are not spent.

There is no Golden capsule in the current PSG1 shop flow.

## 9) Inventory and perk activation

- Inventory: 3 slots (queue).
- Always uses the **first** slot.
- After use, queue shifts left.

Usage limits:
- at least 1 ball must be on the field;
- cannot activate a new perk while a previous perk is still equipped.

Effect duration:
- equipped perk stays active until next ball loss (when `Inventory.Unequip()` is called).

Applied to balls:
- at activation time, all current balls may receive:
  - trail material,
  - ball visual material,
  - ball physics material.

## 10) Capsule loot: exact odds

### 10.1 Basic Capsule (`3` balls, total weight = `100`)

- Ball Prize (`TicketPrize`): `40%`
- Ping Pong: `35%`
- Health Bonus: `25%`

### 10.2 Premium Capsule (`7` balls, total weight = `100`)

- Angel Wings: `40%`
- Extra Ball: `35%`
- Health Bonus: `25%`

Note:
- The Premium card text may call this `Elite Health`; runtime drop is still `Health Bonus`.

## 11) All obtainable perks: actual behavior

Below is the behavior that really exists in code for perks obtainable in the current PSG1 capsule shop.

### 11.1 Ping Pong

- On activation, boosts flippers:
  - `FlipperMotorVelocity: 1500 -> 2500`
  - `FlipperMotorForce: 150 -> 250`
- On unequip, restores default values.

### 11.2 Health Bonus

- On activation: `Lives += 1`.
- On each score event (`OnScoring`):
  - `1/50` chance (2%) to grant another `+1` life.
- Any `Lives` change resets multiplier to `1.0`.

### 11.3 Ball Prize (`TicketPrize` item)

- On activation: instantly grants `Random.Range(3, 6)` => `3..5` balls.
- On each score event:
  - `1/10` chance (10%) to grant `+1` ball.

### 11.4 Angel Wings

- On activation, for each ball:
  - clears velocity and angular velocity,
  - teleports ball to spawnpoint.
- Useful for recovering from bad ball position.

### 11.5 Extra Ball

- On activation: spawns an additional ball.
- Life is removed only when **no** balls remain on the field, so multiball increases survivability.

Legacy note:
- Other historical perks still exist in codebase, but are filtered out from PSG1 shop drops and sanitized from saved inventory.

## 12) Achievements (current PSG1 build)

- Runtime achievement progression is disabled (`Achievements.Enabled == false`).
- Achievement UI is hidden in this build.
- Legacy achievement classes/prefabs still exist in the project, but they do not unlock during normal gameplay.

## 13) Progress persistence

Saved in `PlayerPrefs`:
- balls (`ticketCount`, legacy key name),
- contents of 3 slots (`Item0`, `Item1`, `Item2`),
- achievement state (keys by achievement names).

Not persisted between restarts:
- `Recent Games` table (runtime memory only).

`Reset progress` button:
- runs `PlayerPrefs.DeleteAll()` and reloads scene.

## 14) Practical tips

1. For stable ball farming, keep at least one ball moving: multiplier grows over time and speeds up score-based ball income.
2. `Extra Ball` is one of the strongest survival perks because life is reduced only when all active balls are gone.
3. `Health Bonus` helps survivability, but each life change resets multiplier to `x1`.
4. If inventory is full (3 slots), spend/use perks before buying another capsule or purchase will fail.
5. Use Basic Capsule (`3` balls) for faster economy cycling, and Premium Capsule (`7` balls) when you want stronger recovery/control perks (`Angel Wings`, `Extra Ball`).

---

If needed, I can provide a second version of this guide as a "1-screen quick cheat sheet" (only numbers, formulas, and optimal item builds).
