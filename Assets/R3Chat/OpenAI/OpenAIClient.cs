using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace R3Chat.OpenAI
{
    public sealed class OpenAIClient
    {
        private const string Endpoint = "https://api.openai.com/v1/responses";

        private readonly string _apiKey;
        private readonly string _model;

        public OpenAIClient(string apiKey, string model)
        {
            _apiKey = apiKey ?? "";
            _model = string.IsNullOrWhiteSpace(model) ? "gpt-4o-mini" : model;
        }

        public async Task<(string text, string rawJson)> CompleteTextAsync(
            JArray inputMessages,
            string instructions,
            CancellationToken ct,
            bool store = false,
            int maxOutputTokens = 400)
        {
            var body = new JObject
            {
                ["model"] = _model,
                ["input"] = inputMessages,
                ["instructions"] = instructions ?? "",
                ["store"] = store,
                ["max_output_tokens"] = maxOutputTokens,
                ["text"] = new JObject
                {
                    ["format"] = new JObject { ["type"] = "text" }
                }
            };

            string raw = await PostWithRetryAsync(body, ct);
            string text = ExtractOutputText(raw);
            return (text, raw);
        }

        public async Task<(string jsonText, string rawJson)> CompleteJsonSchemaAsync(
            JArray inputMessages,
            string instructions,
            JObject jsonSchema,          
            CancellationToken ct,
            bool store = false,
            int maxOutputTokens = 600)
        {

            var body = new JObject
            {
                ["model"] = _model,
                ["input"] = inputMessages,
                ["instructions"] = instructions ?? "",
                ["store"] = store,
                ["max_output_tokens"] = maxOutputTokens,
                ["text"] = new JObject
                {
                    ["format"] = new JObject
                    {
                        ["type"] = "json_schema",
                        ["name"] = "nlu_packet",         
                        ["strict"] = true,
                        ["schema"] = jsonSchema
                       
                    }
                }
            };

            string raw = await PostWithRetryAsync(body, ct);
            string text = ExtractOutputText(raw);


            return (text, raw);
        }

        // -------------------- HTTP --------------------

        private async Task<string> PostWithRetryAsync(JObject body, CancellationToken ct)
        {
            Exception last = null;

            for (int attempt = 0; attempt < 4; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await PostOnceAsync(body, ct);
                }
                catch (OpenAIHttpException ex)
                {
                    last = ex;


                    if (!(ex.StatusCode == 429 || ex.StatusCode == 500 || ex.StatusCode == 502 || ex.StatusCode == 503 || ex.StatusCode == 504))
                        throw;

                    int delayMs = 300 * (int)Mathf.Pow(2, attempt); 
                    await Task.Delay(delayMs, ct);
                }
                catch (Exception ex)
                {
                    last = ex;

                    if (attempt >= 1) throw;
                    await Task.Delay(300, ct);
                }
            }

            throw last ?? new Exception("OpenAI request failed (unknown).");
        }

        private async Task<string> PostOnceAsync(JObject body, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new Exception("OpenAI API key is empty. Set it in Inspector.");

            byte[] jsonBytes = Encoding.UTF8.GetBytes(body.ToString(Formatting.None));

            using var req = new UnityWebRequest(Endpoint, "POST");
            req.uploadHandler = new UploadHandlerRaw(jsonBytes);
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Content-Type", "application/json");

            req.SetRequestHeader("Authorization", "Bearer " + _apiKey);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                int code = (int)req.responseCode;
                string err = req.downloadHandler?.text ?? req.error ?? "Unknown error";
                throw new OpenAIHttpException(code, $"OpenAI HTTP error: {code} | {req.error}\n{err}");
            }

            return req.downloadHandler.text;
        }



        public static string ExtractOutputText(string rawJson)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
                return "";

            var root = JObject.Parse(rawJson);
            var output = root["output"] as JArray;
            if (output == null) return "";

            var sb = new StringBuilder();

            foreach (var msg in output)
            {
                if ((string)msg["type"] != "message") continue;

                var content = msg["content"] as JArray;
                if (content == null) continue;

                foreach (var part in content)
                {
                    if ((string)part["type"] == "output_text")
                    {
                        string t = (string)part["text"];
                        if (!string.IsNullOrEmpty(t))
                        {
                            if (sb.Length > 0) sb.Append("\n");
                            sb.Append(t);
                        }
                    }
                }
            }

            return sb.ToString();
        }

        public sealed class OpenAIHttpException : Exception
        {
            public int StatusCode { get; }
            public OpenAIHttpException(int statusCode, string message) : base(message) => StatusCode = statusCode;
        }
    }
}
