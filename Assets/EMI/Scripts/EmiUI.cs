using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EMI
{
    public class EmiUI : MonoBehaviour
    {
        [Header("Emi API")]
        public EmiApiClient api;
        public EmiServerLauncher launcher;

        [Header("UI")]
        public TMP_InputField inputField;
        public Button sendButton;

        [Header("Output (optional)")]
        public TMP_Text chatText;           
        public ScrollRect scrollRect;      

        [Header("Session")]
        public string sessionId = "unity_player_1";

        [Header("Behavior")]
        public bool clearInputAfterSend = true;
        public bool disableSendWhileBusy = true;

        private void Awake()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(SendMessage);

            if (inputField != null)
                inputField.onSubmit.AddListener(_ => SendMessage()); 

            if (api != null)
            {
                api.SetSession(sessionId);
                api.OnChatStarted += OnChatStarted;
                api.OnChatReply += OnChatReply;
                api.OnChatError += OnChatError;
            }

            if (launcher != null)
            {
                launcher.OnServerReady += OnServerReady;
                launcher.OnServerFailed += OnServerFailed;
            }
        }

        private void OnDestroy()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(SendMessage);

            if (inputField != null)
                inputField.onSubmit.RemoveAllListeners();

            if (api != null)
            {
                api.OnChatStarted -= OnChatStarted;
                api.OnChatReply -= OnChatReply;
                api.OnChatError -= OnChatError;
            }

            if (launcher != null)
            {
                launcher.OnServerReady -= OnServerReady;
                launcher.OnServerFailed -= OnServerFailed;
            }
        }

        private void Start()
        {
            if (launcher != null && launcher.autoStartOnPlay)
            {
                AppendSystem("Starting EMI backend...");
                SetInteractable(false);
                return;
            }

            if (api != null)
            {
                SetInteractable(false);
                api.CheckHealth(ok =>
                {
                    AppendSystem(ok ? "EMI server online." : "EMI server offline.");
                    SetInteractable(ok);
                }, err =>
                {
                    AppendSystem("Health error: " + err);
                    SetInteractable(true); 
                });
            }
        }

        public void SendMessage()
        {
            if (api == null)
            {
                AppendSystem("EmiApiClient is not assigned.");
                return;
            }

            if (launcher != null && !launcher.IsReady)
            {
                AppendSystem("Server is not ready yet...");
                return;
            }

            string msg = inputField != null ? inputField.text : "";
            msg = (msg ?? "").Trim();

            if (string.IsNullOrEmpty(msg))
                return;

            AppendUser(msg);

            if (clearInputAfterSend && inputField != null)
            {
                inputField.text = "";
                inputField.ActivateInputField();
            }

            if (disableSendWhileBusy) SetInteractable(false);

            api.SendMessage(
                msg,
                reply =>
                {
                    AppendAssistant(reply);
                    if (disableSendWhileBusy) SetInteractable(true);
                },
                err =>
                {
                    AppendSystem("Error: " + err);
                    if (disableSendWhileBusy) SetInteractable(true);
                }
            );
        }

        // ---------------- Events from launcher ----------------
        private void OnServerReady()
        {
            AppendSystem("EMI backend ready.");
            if (api != null && launcher != null)
            {
                api.SetBaseUrl(launcher.baseUrl);
                api.SetSession(sessionId);
            }
            SetInteractable(true);
        }

        private void OnServerFailed(string err)
        {
            AppendSystem("Backend failed: " + err);
            SetInteractable(true);
        }

        // ---------------- Events from API (optional) ----------------
        private void OnChatStarted()
        {
            if (disableSendWhileBusy) SetInteractable(false);
        }

        private void OnChatReply(string reply)
        {

        }

        private void OnChatError(string err)
        {
            // same note as above
        }

        // ---------------- UI helpers ----------------
        private void SetInteractable(bool on)
        {
            if (sendButton != null)
                sendButton.interactable = on && (api == null || !api.IsBusy);

            if (inputField != null)
                inputField.interactable = on;
        }

        private void AppendUser(string text) => AppendLine($"<b>You:</b> {Escape(text)}");
        private void AppendAssistant(string text) => AppendLine($"<b>Emi:</b> {Escape(text)}");
        private void AppendSystem(string text) => AppendLine($"<i>{Escape(text)}</i>");

        private void AppendLine(string line)
        {
            if (chatText == null)
            {
                Debug.Log(line);
                return;
            }

            if (string.IsNullOrEmpty(chatText.text))
                chatText.text = line;
            else
                chatText.text += "\n\n" + line;

            if (scrollRect != null)
                Canvas.ForceUpdateCanvases(); 
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private static string Escape(string s)
        {
            return (s ?? "").Replace("<", "&lt;").Replace(">", "&gt;");
        }
    }
}
