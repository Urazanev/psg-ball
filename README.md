# PSG Ball

PSG Ball is a handheld-focused arcade pinball game built for the PlaySolana PSG1 device as part of the Matrix hackathon.

The core gameplay and physics are stable and fully playable. This version focuses on proper PSG1 hardware adaptation, Android packaging, and judge-facing documentation.

---

## PSG1 Adaptation

PSG Ball is specifically tuned for the PSG1 handheld experience:

- Physical button mapping for PSG1/gamepad (rear triggers used for flippers)
- Input configuration located at `Assets/Resources/PSG1Controls.inputactions`
- Runtime adapter: `Assets/Scripts/Main Scripts/InputAdapter.cs`
- UI scaled for PSG1 resolution (`1240x1080`)
- Android build configured for `IL2CPP + ARM64`

The rear trigger-based flipper control provides a natural handheld pinball experience compared to touchscreen-based implementations.

---

## Build Instructions (Android)

Open the project in Unity 6.3 LTS (`6000.3.8f1`).

1. Go to `File -> Build Profiles` (or `Build Settings`)
2. Switch platform to `Android`
3. Verify `Player Settings`:
   - `Scripting Backend = IL2CPP`
   - `Target Architectures = ARM64`
4. Build debug APK, or configure signing and build release APK/AAB

For signed builds, see:
`docs/ANDROID_SIGNING.md`

---

## Run in Editor

1. Open `Assets/Scenes/Game.unity`
2. Press Play in Unity Editor

Optional:
`Window -> General -> Device Simulator`
Select PSG1 profile (`1240x1080`) to validate layout and controls.

---

## Matrix Submission Checklist

- Android build succeeds (`IL2CPP + ARM64`)
- PSG1 controls function correctly (`L1/R1/A/Start + D-pad`)
- UI is readable at `1240x1080`
- Core gameplay loop is stable (launch, flippers, scoring, round flow)
- Documentation is up to date

---

## Documentation

- Gameplay guide: [GAMEPLAY_GUIDE.md](GAMEPLAY_GUIDE.md)
- Upstream credits: [docs/UPSTREAM.md](docs/UPSTREAM.md)
- Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
