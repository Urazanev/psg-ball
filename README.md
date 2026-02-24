# PSG Ball

PSG Ball is a handheld-focused arcade pinball game for Matrix submission.  
Core gameplay and table physics are preserved; this fork focuses on PSG1 adaptation, packaging, and judge-facing documentation.

## PSG1 Port Scope

- Input mapping for PSG1/gamepad in `Assets/Resources/PSG1Controls.inputactions` with runtime adapter in `Assets/Scripts/Main Scripts/InputAdapter.cs`.
- UI scaling tuned for PSG1 at `1240x1080` (`Scale With Screen Size` in `Assets/Scenes/Game.unity`).
- Android target configured for `IL2CPP` + `ARM64`.

## Build APK

1. Open project in Unity `6000.3.8f1`.
2. Go to `File > Build Profiles` (or `Build Settings`) and switch to `Android`.
3. Verify `Player Settings`:
   - `Scripting Backend = IL2CPP`
   - `Target Architectures = ARM64`
4. Build debug APK, or configure signing and build release APK/AAB.

For signed builds, see `docs/ANDROID_SIGNING.md`.

## Run In Editor

1. Open `Assets/Scenes/Game.unity`.
2. Press Play in Unity Editor.
3. Optional: open `Window > General > Device Simulator` and choose PSG1 profile (`1240x1080`) for UI/control validation.

## Matrix Submission Checklist

- Android build succeeds (`IL2CPP + ARM64`).
- PSG1 controls work (L1/R1/A/Start + D-pad actions).
- UI is readable and aligned at `1240x1080`.
- Core gameplay loop is stable (launch, flippers, scoring, round flow).
- Judge docs are included and up to date.

## Documentation

- Gameplay guide (RU): [GAMEPLAY_GUIDE_RU.md](GAMEPLAY_GUIDE_RU.md)
- Upstream credits: [docs/UPSTREAM.md](docs/UPSTREAM.md)
- Third-party notices: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)
