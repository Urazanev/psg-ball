#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class IsNormalizedDirDiagnostics
{
    const string AssertMarker = "IsNormalized(dir, 0.0001f)";
    const double DumpCooldownSeconds = 2.0;
    const int MaxRowsPerSection = 24;

    static readonly string DumpPath = Path.Combine(Directory.GetCurrentDirectory(), "Library/IsNormalizedDirDiagnostics.log");

    static double lastDumpAt;
    static int totalAssertsSeen;
    static bool dumpPending;

    static string lastRenderedCameraPath = "<none>";
    static float lastRenderedCameraForwardMagnitude = 1f;
    static bool lastRenderedCameraWorldToCameraFinite = true;
    static bool lastRenderedCameraProjectionFinite = true;

    static IsNormalizedDirDiagnostics()
    {
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.Full);
        Application.logMessageReceivedThreaded += OnLogMessageReceivedThreaded;
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        EditorApplication.quitting += OnEditorQuitting;
    }

    static void OnEditorQuitting()
    {
        Application.logMessageReceivedThreaded -= OnLogMessageReceivedThreaded;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
        EditorApplication.quitting -= OnEditorQuitting;
    }

    static void OnBeginCameraRendering(ScriptableRenderContext _, Camera camera)
    {
        if (camera == null) return;

        lastRenderedCameraPath = BuildPath(camera.transform);
        lastRenderedCameraForwardMagnitude = camera.transform.forward.magnitude;
        lastRenderedCameraWorldToCameraFinite = IsFinite(camera.worldToCameraMatrix);
        lastRenderedCameraProjectionFinite = IsFinite(camera.projectionMatrix);
    }

    static void OnLogMessageReceivedThreaded(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Assert) return;
        if (string.IsNullOrEmpty(condition)) return;
        if (condition.IndexOf(AssertMarker, StringComparison.Ordinal) < 0) return;

        totalAssertsSeen++;

        double now = EditorApplication.timeSinceStartup;
        if (dumpPending) return;
        if (now - lastDumpAt < DumpCooldownSeconds) return;

        dumpPending = true;
        string stackTraceCopy = stackTrace;
        EditorApplication.delayCall += () =>
        {
            dumpPending = false;
            lastDumpAt = EditorApplication.timeSinceStartup;
            WriteDiagnosticDump(stackTraceCopy);
        };
    }

    static void WriteDiagnosticDump(string stackTrace)
    {
        try
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.AppendLine($"[{DateTime.Now:O}] Assert: {AssertMarker}");
            sb.AppendLine($"totalAssertsSeen={totalAssertsSeen}");
            sb.AppendLine($"isPlaying={EditorApplication.isPlaying}, isPaused={EditorApplication.isPaused}");
            sb.AppendLine($"activeScene={SceneManager.GetActiveScene().path}");
            sb.AppendLine($"lastRenderedCamera={lastRenderedCameraPath}");
            sb.AppendLine($"lastCamera.forwardMagnitude={lastRenderedCameraForwardMagnitude:F6}");
            sb.AppendLine($"lastCamera.worldToCameraFinite={lastRenderedCameraWorldToCameraFinite}");
            sb.AppendLine($"lastCamera.projectionFinite={lastRenderedCameraProjectionFinite}");

            AppendCameraSection(sb);
            AppendLightSection(sb);
            AppendTransformAnomalies(sb);
            AppendColliderAnomalies(sb);
            AppendPerkOverlaySection(sb);

            if (!string.IsNullOrWhiteSpace(stackTrace))
            {
                sb.AppendLine("stackTrace:");
                sb.AppendLine(stackTrace);
            }

            sb.AppendLine(new string('-', 80));

            string dir = Path.GetDirectoryName(DumpPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.AppendAllText(DumpPath, sb.ToString());

            Debug.LogWarning($"IsNormalized diagnostics dumped: {DumpPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"IsNormalized diagnostics failed: {ex}");
        }
    }

    static void AppendCameraSection(StringBuilder sb)
    {
        Camera[] cameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"cameras={cameras.Length}");

        int rows = 0;
        for (int i = 0; i < cameras.Length; i++)
        {
            if (rows >= MaxRowsPerSection) break;
            Camera cam = cameras[i];
            if (cam == null) continue;

            Transform t = cam.transform;
            Vector3 matrixForward = t.localToWorldMatrix.GetColumn(2);
            float matrixForwardMag = matrixForward.magnitude;

            sb.AppendLine(
                $"  CAM {BuildPath(t)} active={cam.gameObject.activeInHierarchy} enabled={cam.enabled} " +
                $"forwardMag={t.forward.magnitude:F6} matrixForwardMag={matrixForwardMag:F6} " +
                $"lossyScale={Format(t.lossyScale)} worldToCamFinite={IsFinite(cam.worldToCameraMatrix)} projFinite={IsFinite(cam.projectionMatrix)}");
            rows++;
        }
    }

    static void AppendLightSection(StringBuilder sb)
    {
        Light[] lights = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"lights={lights.Length} renderSettings.sun={(RenderSettings.sun != null ? BuildPath(RenderSettings.sun.transform) : "<none>")}");

        int rows = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            if (rows >= MaxRowsPerSection) break;
            Light light = lights[i];
            if (light == null) continue;

            Transform t = light.transform;
            Vector3 matrixForward = t.localToWorldMatrix.GetColumn(2);
            float matrixForwardMag = matrixForward.magnitude;

            sb.AppendLine(
                $"  LGT {BuildPath(t)} type={light.type} enabled={light.enabled} active={light.gameObject.activeInHierarchy} " +
                $"shadows={light.shadows} intensity={light.intensity:F3} forwardMag={t.forward.magnitude:F6} " +
                $"matrixForwardMag={matrixForwardMag:F6} lossyScale={Format(t.lossyScale)} rotNorm={QuaternionNorm(t.rotation):F6}");
            rows++;
        }
    }

    static void AppendTransformAnomalies(StringBuilder sb)
    {
        Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int issues = 0;
        sb.AppendLine("transformAnomalies:");

        for (int i = 0; i < transforms.Length; i++)
        {
            if (issues >= MaxRowsPerSection) break;
            Transform t = transforms[i];
            if (t == null) continue;

            bool badPosition = !IsFinite(t.position);
            bool badScale = !IsFinite(t.lossyScale);
            float qNorm = QuaternionNorm(t.rotation);
            bool badRotation = !float.IsFinite(qNorm) || qNorm < 0.5f || qNorm > 1.5f;

            if (!badPosition && !badScale && !badRotation) continue;

            sb.AppendLine(
                $"  XFM {BuildPath(t)} badPosition={badPosition} badScale={badScale} badRotation={badRotation} " +
                $"pos={Format(t.position)} lossyScale={Format(t.lossyScale)} rot={Format(t.rotation)} rotNorm={qNorm:F6}");
            issues++;
        }

        if (issues == 0)
            sb.AppendLine("  none");
    }

    static void AppendColliderAnomalies(StringBuilder sb)
    {
        BoxCollider[] boxes = UnityEngine.Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int issues = 0;
        sb.AppendLine("boxColliderAnomalies:");

        for (int i = 0; i < boxes.Length; i++)
        {
            if (issues >= MaxRowsPerSection) break;
            BoxCollider box = boxes[i];
            if (box == null) continue;

            Vector3 size = box.size;
            Vector3 lossy = box.transform.lossyScale;
            bool nonPositiveSize = size.x <= 0f || size.y <= 0f || size.z <= 0f;
            bool negativeScale = lossy.x < 0f || lossy.y < 0f || lossy.z < 0f;
            bool nonFinite = !IsFinite(size) || !IsFinite(lossy);

            if (!nonPositiveSize && !negativeScale && !nonFinite) continue;

            sb.AppendLine(
                $"  COL {BuildPath(box.transform)} enabled={box.enabled} nonPositiveSize={nonPositiveSize} " +
                $"negativeScale={negativeScale} nonFinite={nonFinite} size={Format(size)} lossyScale={Format(lossy)}");
            issues++;
        }

        if (issues == 0)
            sb.AppendLine("  none");
    }

    static void AppendPerkOverlaySection(StringBuilder sb)
    {
        PerkSlots3DOverlay[] overlays = UnityEngine.Object.FindObjectsByType<PerkSlots3DOverlay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        sb.AppendLine($"perkOverlays={overlays.Length}");

        int rows = 0;
        for (int i = 0; i < overlays.Length; i++)
        {
            if (rows >= MaxRowsPerSection) break;
            PerkSlots3DOverlay overlay = overlays[i];
            if (overlay == null) continue;

            Transform t = overlay.transform;
            sb.AppendLine(
                $"  OVL {BuildPath(t)} active={overlay.gameObject.activeInHierarchy} enabled={overlay.enabled} " +
                $"pos={Format(t.position)} lossyScale={Format(t.lossyScale)}");
            rows++;
        }
    }

    static string BuildPath(Transform transform)
    {
        if (transform == null) return "<null>";

        StringBuilder sb = new StringBuilder(transform.name);
        Transform current = transform.parent;
        while (current != null)
        {
            sb.Insert(0, "/");
            sb.Insert(0, current.name);
            current = current.parent;
        }

        return sb.ToString();
    }

    static float QuaternionNorm(Quaternion q)
    {
        float sum = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
        return Mathf.Sqrt(sum);
    }

    static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    static bool IsFinite(Quaternion q)
    {
        return float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w);
    }

    static bool IsFinite(Matrix4x4 m)
    {
        for (int i = 0; i < 16; i++)
        {
            if (!float.IsFinite(m[i]))
                return false;
        }

        return true;
    }

    static string Format(Vector3 v)
    {
        return $"({v.x:F5}, {v.y:F5}, {v.z:F5})";
    }

    static string Format(Quaternion q)
    {
        return $"({q.x:F5}, {q.y:F5}, {q.z:F5}, {q.w:F5})";
    }
}
#endif
