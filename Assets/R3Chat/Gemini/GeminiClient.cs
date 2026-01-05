using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace R3Chat.Gemini
{
    public interface IGeminiClient
    {
        Task<string> CompleteAsync(string prompt, CancellationToken ct);

        Task<string> CompleteJsonAsync(string prompt, CancellationToken ct);
    }

    public class GeminiClient : IGeminiClient
    {
        private readonly string _apiKey;
        private readonly string _model;

        private readonly int _maxOutputTokens;
        private readonly float _temperature;

        private readonly int _maxRetries;
        private readonly float _baseDelaySec;

        public GeminiClient(
            string apiKey,
            string model = "gemini-2.5-flash",
            int maxOutputTokens = 512,
            float temperature = 0.4f,
            int maxRetries = 3,
            float baseDelaySec = 0.8f)
        {
            _apiKey = apiKey;
            _model = model;

            _maxOutputTokens = Mathf.Clamp(maxOutputTokens, 32, 4096);
            _temperature = Mathf.Clamp(temperature, 0f, 2f);

            _maxRetries = Mathf.Clamp(maxRetries, 0, 8);
            _baseDelaySec = Mathf.Clamp(baseDelaySec, 0.1f, 10f);
        }

        public Task<string> CompleteAsync(string prompt, CancellationToken ct)
            => CompleteWithRetry(prompt, ct, forceJson: false);

        public Task<string> CompleteJsonAsync(string prompt, CancellationToken ct)
            => CompleteWithRetry(prompt, ct, forceJson: true);

        private async Task<string> CompleteWithRetry(string prompt, CancellationToken ct, bool forceJson)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new Exception("Gemini API key is empty");

            Exception lastEx = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await CompleteOnceAsync(prompt, ct, forceJson);
                }
                catch (GeminiHttpException ex) when (ex.IsTransient && attempt < _maxRetries)
                {
                    lastEx = ex;
                    float wait = ex.RetryAfterSeconds > 0
                        ? ex.RetryAfterSeconds
                        : (_baseDelaySec * Mathf.Pow(2f, attempt));

                    wait += UnityEngine.Random.Range(0f, 0.25f);

                    Debug.LogWarning($"[Gemini] transient {ex.StatusCode} -> retry in {wait:0.00}s (attempt {attempt + 1}/{_maxRetries})");
                    await Task.Delay(TimeSpan.FromSeconds(wait), ct);
                }
                catch (Exception ex) when (attempt < _maxRetries)
                {
                    lastEx = ex;
                    float wait = (_baseDelaySec * Mathf.Pow(2f, attempt)) + UnityEngine.Random.Range(0f, 0.25f);
                    Debug.LogWarning($"[Gemini] exception '{ex.Message}' -> retry in {wait:0.00}s (attempt {attempt + 1}/{_maxRetries})");
                    await Task.Delay(TimeSpan.FromSeconds(wait), ct);
                }
            }

            throw lastEx ?? new Exception("Gemini request failed (unknown)");
        }

        private async Task<string> CompleteOnceAsync(string prompt, CancellationToken ct, bool forceJson)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";

            var reqObj = new GenerateContentRequest
            {
                contents = new[]
                {
                    new Content
                    {
                        role = "user",
                        parts = new[] { new Part { text = prompt } }
                    }
                },
                generationConfig = new GenerationConfig
                {
                    temperature = _temperature,
                    maxOutputTokens = _maxOutputTokens,

                    responseMimeType = forceJson ? "application/json" : null
                }
            };

            string bodyJson = JsonUtility.ToJson(reqObj);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(bodyJson);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("x-goog-api-key", _apiKey);

            var op = req.SendWebRequest();

            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    req.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            string respText = req.downloadHandler?.text ?? "";

            if (req.result != UnityWebRequest.Result.Success)
            {
                float retryAfter = TryGetRetryAfterSeconds(req);
                long code = req.responseCode;
                bool transient = (code == 429 || code == 500 || code == 503 || code == 502 || code == 504);

                string msg = (req.error ?? "Request failed") + (string.IsNullOrWhiteSpace(respText) ? "" : ("\n" + respText));
                throw new GeminiHttpException((int)code, "HTTP_ERROR", msg, transient, retryAfter);
            }

            var resp = JsonUtility.FromJson<GenerateContentResponse>(respText);
            if (resp == null || resp.candidates == null || resp.candidates.Length == 0)
                throw new GeminiHttpException(200, "NO_CANDIDATES", respText, isTransient: false, retryAfterSeconds: 0);

            var cand = resp.candidates[0];
            if (cand.content == null || cand.content.parts == null || cand.content.parts.Length == 0)
                throw new GeminiHttpException(200, "NO_PARTS", respText, isTransient: false, retryAfterSeconds: 0);

            return cand.content.parts[0].text ?? "";
        }

        private static float TryGetRetryAfterSeconds(UnityWebRequest req)
        {
            try
            {
                Dictionary<string, string> headers = req.GetResponseHeaders();
                if (headers == null) return 0f;

                foreach (var kv in headers)
                {
                    if (string.Equals(kv.Key, "Retry-After", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(kv.Value, out float seconds))
                            return Mathf.Clamp(seconds, 0f, 30f);
                    }
                }
            }
            catch { }
            return 0f;
        }



        [Serializable]
        private class GenerateContentRequest
        {
            public Content[] contents;
            public GenerationConfig generationConfig;
        }

        [Serializable]
        private class GenerationConfig
        {
            public float temperature = 0.4f;
            public int maxOutputTokens = 512;

            public string responseMimeType;
        }

        [Serializable]
        private class Content
        {
            public string role;
            public Part[] parts;
        }

        [Serializable]
        private class Part
        {
            public string text;
        }

        [Serializable]
        private class GenerateContentResponse
        {
            public Candidate[] candidates;
        }

        [Serializable]
        private class Candidate
        {
            public Content content;
            public string finishReason;
        }

        private sealed class GeminiHttpException : Exception
        {
            public int StatusCode { get; }
            public string Status { get; }
            public bool IsTransient { get; }
            public float RetryAfterSeconds { get; }

            public GeminiHttpException(int statusCode, string status, string message, bool isTransient, float retryAfterSeconds)
                : base(message)
            {
                StatusCode = statusCode;
                Status = status;
                IsTransient = isTransient;
                RetryAfterSeconds = retryAfterSeconds;
            }
        }
    }
}
