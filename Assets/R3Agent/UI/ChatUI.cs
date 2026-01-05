using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3Chat.Core;

namespace R3Chat.UI
{
    [DisallowMultipleComponent]
    public class ChatUI : MonoBehaviour
    {
        [Header("Wiring")]
        public ConversationOrchestrator orchestrator;

        [Header("UI")]
        public TMP_InputField inputField;
        public Button sendButton;

        [Tooltip("Текст для лога чата (обычно TextMeshProUGUI внутри ScrollView/Viewport/Content).")]
        public TMP_Text chatText;

        [Tooltip("ScrollRect от ScrollView (чтобы автоскроллить вниз).")]
        public ScrollRect scrollRect;

        [Header("Behavior")]
        public bool sendOnEnter = true;
        public int maxCharsInLog = 20000;

        private readonly StringBuilder _sb = new StringBuilder(4096);

        private void Awake()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            if (inputField != null)
                inputField.onSubmit.AddListener(_ => OnSubmit());
        }

        private void OnEnable()
        {
            if (orchestrator != null)
            {
                orchestrator.OnAssistantResponse += HandleAssistant;
                orchestrator.OnSystemMessage += HandleSystem;
            }
        }

        private void OnDisable()
        {
            if (orchestrator != null)
            {
                orchestrator.OnAssistantResponse -= HandleAssistant;
                orchestrator.OnSystemMessage -= HandleSystem;
            }
        }

        private void OnSubmit()
        {
            // TMP_InputField OnSubmit иногда вызывается при Enter
            if (sendOnEnter)
                OnSendClicked();
        }

        public void OnSendClicked()
        {
            if (orchestrator == null || inputField == null) return;

            string userText = inputField.text;
            if (string.IsNullOrWhiteSpace(userText))
                return;

            AppendLine($"YOU: {userText.Trim()}");

            inputField.text = "";
            inputField.ActivateInputField();

            orchestrator.SendUserMessage(userText);
        }

        private void HandleAssistant(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = "(empty)";

            AppendLine($"AI: {text.Trim()}");
        }

        private void HandleSystem(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            AppendLine($"SYS: {text.Trim()}");
        }

        private void AppendLine(string line)
        {
            _sb.AppendLine(line);
            _sb.AppendLine();

            // ограничим лог, чтобы UI не лагал
            if (_sb.Length > maxCharsInLog)
            {
                // режем начало (простая стратегия)
                _sb.Remove(0, _sb.Length - maxCharsInLog);
            }

            if (chatText != null)
                chatText.text = _sb.ToString();

            AutoScrollToBottom();
        }

        private void AutoScrollToBottom()
        {
            if (scrollRect == null) return;

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // вниз
            Canvas.ForceUpdateCanvases();
        }
    }
}
