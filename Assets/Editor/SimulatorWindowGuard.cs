#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class SimulatorWindowGuard
{
    static SimulatorWindowGuard()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        CloseSimulatorWindowIfOpen();
    }

    static void CloseSimulatorWindowIfOpen()
    {
        Type simulatorType = Type.GetType("UnityEditor.DeviceSimulation.SimulatorWindow, Unity.DeviceSimulator.Editor");
        if (simulatorType == null) return;

        UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(simulatorType);
        if (windows == null || windows.Length == 0) return;

        for (int i = 0; i < windows.Length; i++)
        {
            EditorWindow window = windows[i] as EditorWindow;
            if (window == null) continue;
            window.Close();
        }

        Debug.LogWarning("SimulatorWindowGuard: closed Device Simulator window in Play Mode to avoid URP IsNormalized assert spam.");
    }
}
#endif
