# Android Signing (PSG1)

This project does not store private signing keys in git.

## 1) Create a release keystore (outside repository)

Example:

```bash
keytool -genkeypair \
  -v \
  -keystore ~/keystores/simple-pinball-psg1.jks \
  -alias simple-pinball-psg1 \
  -keyalg RSA \
  -keysize 2048 \
  -validity 10000
```

Keep the keystore outside the project folder, for example:
- `~/keystores/simple-pinball-psg1.jks`

## 2) Configure Unity manually (Editor)

Open:
- `Project Settings > Player > Android > Publishing Settings`

Set:
- `Custom Keystore` = enabled
- `Keystore` = absolute path to your `.jks/.keystore`
- `Keystore password`
- `Key alias`
- `Key password`

Then build signed `APK` or `AAB`.

## 3) Configure batch signing (optional)

For scripted builds (`Assets/Editor/AndroidBuild.cs`), export:

```bash
export PSG1_KEYSTORE_PATH="$HOME/keystores/simple-pinball-psg1.jks"
export PSG1_KEYSTORE_PASS="***"
export PSG1_KEY_ALIAS="simple-pinball-psg1"
export PSG1_KEY_ALIAS_PASS="***"
```

Then run Unity with:
- `AndroidBuild.BuildSignedReleaseApk` or
- `AndroidBuild.BuildSignedReleaseAab`

## 4) Security notes

- Do not commit `.jks/.keystore` files.
- Do not commit passwords or env files with secrets.
- Use CI secret storage for automated release builds.
