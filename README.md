<p align="center">
    <img src="https://github.com/lluckymou/simple-pinball/raw/main/Game/title.png?raw=true"/>
</p>
<h1 align="center">
    <img src="https://github.com/lluckymou/simple-pinball/blob/main/Assets/UI/Achievements/icons8-pinball-80.png?raw=true" height="25"/>
    <a href="https://lluckymou.github.io/simple-pinball/">Play</a>
    •
    <img src="https://github.com/lluckymou/simple-pinball/blob/main/Assets/UI/Achievements/icons8-fantasy-80.png?raw=true" height="25"/>
    <a href="https://github.com/lluckymou/simple-pinball/wiki">Wiki</a>
</h1>

Simple pinball is a game made as a university project aimed at teaching [Unity](https://unity.com) physics. Although the idea was to create just a simple pinball game with a plunger, flippers and an "obstacle", the project grew and now has 12 different power-ups, as well as a couple of achievements that make the game much more fun and enjoyable.

## Installation and usage

Simple Pinball requires the latest **[Unity 2020.3](https://unity3d.com/get-unity/download/archive)** LTS version.
[Visual Studio Code](https://code.visualstudio.com) with the [C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) is also recommended. The recommended setup guide is described below.

### Development setup (July 2021)

Simply follow the instructions to run the project from the source above or on your own fork.

Download the following development kits:
- [.NET Framework SDK 4.7.1](https://dotnet.microsoft.com/download/dotnet-framework/net471)
- [.NET SDK 5.0](https://dotnet.microsoft.com/download/dotnet/5.0)
- [.NET Core SDK 3.1](https://dotnet.microsoft.com/download/dotnet/3.1)

Download and install [Visual Studio Code](https://code.visualstudio.com) for scripting as well as the [The C# Extension](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp).

Download [Unity Hub](https://unity3d.com/get-unity/download) and activate your license there.

Download Unity 2020.3 LTS from their [Archive](https://unity3d.com/get-unity/download/archive) and make sure to install WebGL compiling capabilites to be able to compile Simple Pinball for the web.

> You may also disable the option to download Visual Studio, as this tutorial covers the setup of Visual Studio Code, which is much lighter.

Now clone the repository inside the folder of your choice:

```bash
git clone https://github.com/lluckymou/simple-pinball.git
```

Add the repository folder to Unity Hub and open the project. Once open, you may configure Visual Studio by navigating to:

**Edit** > Preferences > External Tools

And setting Visual Studio Code as your default "External Script Editor".

Now simply open any script and let Visual Studio's C# extension configure itself. If anything fails the console will prompt what is wrong and it wont be hard to fix, from experience it'll probably be some SDK version mismatch.

### Pull requests and changes

Before submitting any changes, make sure to:

- Run your build to check for any compilation/runtime errors;
- Change the *"Game" scene*'s version *Text* component following the format: `1.MAJOR.MINOR.FIXES` (if it's not a fix you can omit the `.0`, such as `1.0.5` instead of `1.0.5.0`);
- Check if the compression format (under *Project Settings > Player*) is listed as **Disabled**.

## PSG1 Port (Stage 1)

This repository now includes a PSG1 adaptation layer that preserves gameplay logic and only changes:
- input integration (PlaySolana + Unity New Input System),
- UI reference resolution for PSG1 screen,
- Android build settings for PSG1 target.

### Added packages

- `com.playsolana.psdk` (from `https://github.com/playsolana/PlaySolana.Unity-SDK.git#main`)
- `com.unity.inputsystem`

`Project Settings > Player > Active Input Handling` is set to `Input System Package (New)` and Android backend is configured for `IL2CPP + ARM64`.

### PSG1 input mapping

Input actions are defined in:
- `Assets/Resources/PSG1Controls.inputactions`

Runtime adapter:
- `Assets/Scripts/Main Scripts/InputAdapter.cs`

| Game Action | PSG1/Gamepad binding | Notes |
| --- | --- | --- |
| LeftFlipper | `leftShoulder` (`L1`) | Equivalent to old `A/LeftArrow` behavior |
| RightFlipper | `rightShoulder` (`R1`) | Equivalent to old `D/RightArrow` behavior |
| Plunger | `buttonSouth` (`A`) | Hold/release semantics preserved (`Space` equivalent) |
| Pause | `start` | Equivalent to `Esc` |
| NudgeLeft | `dpad/left` | Equivalent to `LeftShift` |
| NudgeRight | `dpad/right` | Equivalent to `RightShift` |
| UseItem | `buttonWest` (`X`) | Equivalent to `LeftCtrl` |

Unsupported PSG1 buttons are not used (`Volume +/-`, `Home`, `Fingerprint`, `R2/L2`).

### PSG1 simulator workflow

1. Open the project in Unity.
2. Ensure package `Play Solana Unity SDK` is imported (via `Packages/manifest.json`).
3. Open `Window > General > Device Simulator`.
4. In Device Simulator, select the PSG1 device profile (`PSG1`, 1240x1080) from the device dropdown.
5. Enter Play Mode and test controls with the simulator/gamepad bindings above.

If `Device Simulator` window is unavailable, install Unity's Device Simulator package from Package Manager first.

### Screen/UI target

Canvas scaler in `Assets/Scenes/Game.unity` is configured with:
- `Scale With Screen Size`
- `Reference Resolution: 1240 x 1080`

### Android build (manual via Unity)

1. Open `File > Build Profiles` (or `Build Settings`) and select `Android`.
2. Confirm:
   - `Scripting Backend: IL2CPP`
   - `Target Architectures: ARM64`
3. Debug APK:
   - Build with development options if needed.
4. Signed release APK/AAB:
   - Configure keystore in `Player Settings > Publishing Settings`.
   - Build APK or AAB.

### Android build (optional batch methods)

Editor script:
- `Assets/Editor/AndroidBuild.cs`

Available methods:
- `AndroidBuild.BuildDebugApk`
- `AndroidBuild.BuildSignedReleaseApk`
- `AndroidBuild.BuildSignedReleaseAab`

For signed methods, set env vars before build:
- `PSG1_KEYSTORE_PATH`
- `PSG1_KEYSTORE_PASS`
- `PSG1_KEY_ALIAS`
- `PSG1_KEY_ALIAS_PASS`

Detailed signing instructions:
- `docs/ANDROID_SIGNING.md`

## Wallet Connect (no transactions)

This project includes an in-game `Connect Wallet` button:
- script: `Assets/Scripts/Main Scripts/WalletConnectUI.cs`
- behavior: attempts wallet login via Solana Unity SDK, no transaction flow is included.

### Package dependency

The manifest includes:
- `com.solana.unity-sdk` from `https://github.com/magicblock-labs/Solana.Unity-SDK.git#v1.0.0-preview.31`

If Unity cannot resolve this exact tag, update it to a current tag from the SDK repository.

### How to test

1. Open project and let Unity resolve packages.
2. Open the game scene and enter Play Mode.
3. Press `Connect Wallet` (top-right overlay panel).
4. Approve connection in wallet adapter flow.
5. Check the status label changes from `Wallet: not connected` to a shortened wallet address.
