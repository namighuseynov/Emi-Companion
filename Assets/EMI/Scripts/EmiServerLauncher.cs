using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class EmiServerLauncher : MonoBehaviour
{
    [Header("Server URL")]
    public string baseUrl = "http://127.0.0.1:8000";

    [Header("Launch mode")]
    public bool autoStartOnPlay = true;
    public bool killOnQuit = true;

    [Header("Backend paths (Windows)")]
    [Tooltip("Folder where app.py. Example: C:/EmiProject/backend")]
    public string backendWorkingDir = @"C:\EMI\backend";

    [Tooltip("Full path for python.exe")]
    public string pythonExe = @"C:\EMI\backend\venv\Scripts\python.exe";

    public string uvicornArgs = @"-m uvicorn app:app --host 127.0.0.1 --port 8000";

    [Header("Health check")]
    public float healthTimeoutSeconds = 120f;
    public float healthPollInterval = 0.3f;

    public bool IsReady { get; private set; }

    private Process _proc;

    public Action OnServerReady;
    public Action<string> OnServerFailed;

    private void Start()
    {
        if (autoStartOnPlay)
            StartCoroutine(StartServerAndWait());
    }

    public void StartServer()
    {
        if (_proc != null && !_proc.HasExited) return;

        if (!Directory.Exists(backendWorkingDir))
        {
            Fail($"backendWorkingDir not found: {backendWorkingDir}");
            return;
        }

        if (!File.Exists(pythonExe))
        {
            Fail($"pythonExe not found: {pythonExe}");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = uvicornArgs,
            WorkingDirectory = backendWorkingDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            _proc = new Process();
            _proc.StartInfo = psi;
            _proc.EnableRaisingEvents = true;

            _proc.OutputDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.Log("[EMI SERVER] " + e.Data);
            };
            _proc.ErrorDataReceived += (s, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    Debug.LogWarning("[EMI SERVER ERR] " + e.Data);
            };

            _proc.Start();
            _proc.BeginOutputReadLine();
            _proc.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            Fail("Failed to start server: " + ex.Message);
        }
    }

    public IEnumerator StartServerAndWait()
    {
        IsReady = false;

        yield return StartCoroutine(WaitForHealth(2f));
        if (IsReady) yield break;

        StartServer();

        yield return StartCoroutine(WaitForHealth(healthTimeoutSeconds));

        if (!IsReady)
            Fail("Server did not become ready within timeout.");
    }

    private IEnumerator WaitForHealth(float timeoutSeconds)
    {
        float start = Time.realtimeSinceStartup;

        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            using (var req = UnityWebRequest.Get($"{baseUrl.TrimEnd('/')}/api/health"))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool ok = (req.result == UnityWebRequest.Result.Success);
#else
                bool ok = !(req.isNetworkError || req.isHttpError);
#endif
                if (ok)
                {
                    var text = req.downloadHandler.text ?? "";
                    if (text.Contains("\"ok\"") && text.Contains("true"))
                    {
                        IsReady = true;
                        Debug.Log("EMI server is READY.");
                        OnServerReady?.Invoke();
                        yield break;
                    }
                }
            }

            yield return new WaitForSecondsRealtime(healthPollInterval);
        }
    }

    private void Fail(string reason)
    {
        Debug.LogError("EMI server launch failed: " + reason);
        OnServerFailed?.Invoke(reason);
    }

    private void OnApplicationQuit()
    {
        if (!killOnQuit) return;

        try
        {
            if (_proc != null && !_proc.HasExited)
            {
                _proc.Kill();
                _proc.Dispose();
                _proc = null;
            }
        }
        catch { /* ignore */ }
    }
}
