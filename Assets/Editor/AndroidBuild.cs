using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class AndroidBuild
{
    const string BuildDirectory = "Builds/Android";
    const string DebugApkName = "simple-pinball-psg1-debug.apk";
    const string ReleaseApkName = "simple-pinball-psg1-release-signed.apk";
    const string ReleaseAabName = "simple-pinball-psg1-release-signed.aab";

    public static void BuildDebugApk()
    {
        ConfigureAndroidBaseSettings();
        PlayerSettings.Android.useCustomKeystore = false;
        EditorUserBuildSettings.buildAppBundle = false;

        Build(DebugApkName, BuildOptions.Development | BuildOptions.AllowDebugging);
    }

    public static void BuildSignedReleaseApk()
    {
        ConfigureAndroidBaseSettings();
        ConfigureSigningFromEnvironment();
        EditorUserBuildSettings.buildAppBundle = false;

        Build(ReleaseApkName, BuildOptions.None);
        ClearSigningValues();
    }

    public static void BuildSignedReleaseAab()
    {
        ConfigureAndroidBaseSettings();
        ConfigureSigningFromEnvironment();
        EditorUserBuildSettings.buildAppBundle = true;

        Build(ReleaseAabName, BuildOptions.None);
        ClearSigningValues();
    }

    static void ConfigureAndroidBaseSettings()
    {
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
    }

    static void ConfigureSigningFromEnvironment()
    {
        string keystorePath = RequireEnvironmentVariable("PSG1_KEYSTORE_PATH");
        string keystorePass = RequireEnvironmentVariable("PSG1_KEYSTORE_PASS");
        string keyAlias = RequireEnvironmentVariable("PSG1_KEY_ALIAS");
        string keyAliasPass = RequireEnvironmentVariable("PSG1_KEY_ALIAS_PASS");

        if (!File.Exists(keystorePath))
            throw new BuildFailedException($"Keystore file not found: {keystorePath}");

        PlayerSettings.Android.useCustomKeystore = true;
        PlayerSettings.Android.keystoreName = keystorePath;
        PlayerSettings.Android.keystorePass = keystorePass;
        PlayerSettings.Android.keyaliasName = keyAlias;
        PlayerSettings.Android.keyaliasPass = keyAliasPass;
    }

    static void ClearSigningValues()
    {
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.keystoreName = string.Empty;
        PlayerSettings.Android.keystorePass = string.Empty;
        PlayerSettings.Android.keyaliasName = string.Empty;
        PlayerSettings.Android.keyaliasPass = string.Empty;
    }

    static string RequireEnvironmentVariable(string name)
    {
        string value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new BuildFailedException($"Missing environment variable: {name}");
        return value;
    }

    static void Build(string outputName, BuildOptions options)
    {
        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new BuildFailedException("No enabled scenes in Build Settings.");

        Directory.CreateDirectory(BuildDirectory);
        string outputPath = Path.Combine(BuildDirectory, outputName);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = options
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"Android build failed: {report.summary.result}");
    }
}
