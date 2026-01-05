// EmiApiClient.cs

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class EmiChatRequest
{
    public string session_id = "default";
    public string message = "";
    public bool reset = false;
}

[Serializable]
public class EmiChatResponse
{
    public string reply;
}

public class EmiApiClient : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Example: http://127.0.0.1:8000")]
    public string baseUrl = "http://127.0.0.1:8000";

    [Header("Session")]
    [Tooltip("Use a stable ID per player to preserve history.")]
    public string sessionId = "unity_player_1";

    [Header("Timeouts")]
    [Tooltip("Health request timeout (seconds).")]
    public int healthTimeout = 3;

    [Tooltip("Chat request timeout (seconds). Model generation can be slow.)")]
    public int chatTimeout = 90;

    [Header("Behavior")]
    public bool logRawResponses = false;
    public bool preventParallelRequests = true;

    public bool IsBusy => _busy;

    // Optional events (handy for UI)
    public event Action OnChatStarted;
    public event Action<string> OnChatReply;
    public event Action<string> OnChatError;

    private bool _busy = false;

    // --------------------- Public helpers ---------------------

    public void SetBaseUrl(string url)
    {
        baseUrl = (url ?? "").Trim();
    }

    public void SetSession(string sid)
    {
        sessionId = string.IsNullOrWhiteSpace(sid) ? "default" : sid.Trim();
    }

    /// <summary>
    /// Checks GET /api/health and returns ok=true/false
    /// </summary>
    public void CheckHealth(Action<bool> onOk, Action<string> onError = null)
    {
        StartCoroutine(HealthCoroutine(onOk, onError));
    }

    /// <summary>
    /// Sends user message to POST /api/chat and returns reply string.
    /// </summary>
    public void SendMessage(string message, Action<string> onReply, Action<string> onError = null)
    {
        StartCoroutine(ChatCoroutine(message, onReply, onError, reset: false));
    }

    /// <summary>
    /// Resets server-side session memory for this sessionId.
    /// </summary>
    public void ResetSession(Action<string> onReply = null, Action<string> onError = null)
    {
        // Your backend returns "Okay — fresh start."
        StartCoroutine(ChatCoroutine("", onReply, onError, reset: true));
    }

    // --------------------- Coroutines ---------------------

    private IEnumerator HealthCoroutine(Action<bool> onOk, Action<string> onError)
    {
        string url = $"{baseUrl.TrimEnd('/')}/api/health";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = Mathf.Max(1, healthTimeout);

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool failed = (req.result != UnityWebRequest.Result.Success);
#else
            bool failed = req.isNetworkError || req.isHttpError;
#endif

            if (failed)
            {
                onError?.Invoke($"Health failed: {req.error}");
                onOk?.Invoke(false);
                yield break;
            }

            string body = req.downloadHandler?.text ?? "";
            // Quick check. If you want strict JSON parsing, tell me and I’ll add a tiny parser.
            bool ok = body.Contains("\"ok\"") && body.Contains("true");
            onOk?.Invoke(ok);
        }
    }

    private IEnumerator ChatCoroutine(string message, Action<string> onReply, Action<string> onError, bool reset)
    {
        if (preventParallelRequests && _busy)
        {
            onError?.Invoke("EMI client is busy (previous request still running).");
            yield break;
        }

        _busy = true;
        OnChatStarted?.Invoke();

        string url = $"{baseUrl.TrimEnd('/')}/api/chat";

        EmiChatRequest payload = new EmiChatRequest
        {
            session_id = string.IsNullOrWhiteSpace(sessionId) ? "default" : sessionId,
            message = message ?? "",
            reset = reset
        };

        string json = JsonUtility.ToJson(payload);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = Mathf.Max(3, chatTimeout);

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool failed = (req.result != UnityWebRequest.Result.Success);
#else
            bool failed = req.isNetworkError || req.isHttpError;
#endif

            string respText = req.downloadHandler?.text ?? "";

            if (logRawResponses)
                Debug.Log($"[EMI] Raw response: {respText}");

            if (failed)
            {
                string err = $"Chat failed: {req.error}";
                if (!string.IsNullOrEmpty(respText)) err += $"\nBody: {respText}";

                onError?.Invoke(err);
                OnChatError?.Invoke(err);

                _busy = false;
                yield break;
            }

            EmiChatResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<EmiChatResponse>(respText);
            }
            catch (Exception e)
            {
                string err = $"JSON parse error: {e.Message}\nRaw: {respText}";
                onError?.Invoke(err);
                OnChatError?.Invoke(err);

                _busy = false;
                yield break;
            }

            if (resp == null || resp.reply == null)
            {
                string err = $"Invalid response payload.\nRaw: {respText}";
                onError?.Invoke(err);
                OnChatError?.Invoke(err);

                _busy = false;
                yield break;
            }

            onReply?.Invoke(resp.reply);
            OnChatReply?.Invoke(resp.reply);

            _busy = false;
        }
    }
}