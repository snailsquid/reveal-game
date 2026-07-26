using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RevealGame.Data;

namespace RevealGame.UI
{
    /// <summary>
    /// Coordinates Discord listener setup and keeps its connection state reflected in the UI.
    /// </summary>
    public sealed class DiscordStatusCoordinator
    {
        private readonly MonoBehaviour _host;
        private readonly GameSession _session;
        private readonly Image _statusImage;
        private readonly TMP_Text _statusText;
        private readonly string _serverUrl;
        private readonly string _apiKey;
        private readonly float _healthCheckInterval;
        private readonly float _sessionPollInterval;
        private readonly string[] _channelIds;
        private readonly DiscordListenerProcessLauncher _configuredProcessLauncher;

        private Coroutine _refreshRoutine;

        public DiscordAnswerListener AnswerListener { get; private set; }
        public DiscordListenerProcessLauncher ProcessLauncher { get; private set; }

        public DiscordStatusCoordinator(
            MonoBehaviour host,
            GameSession session,
            Image statusImage,
            TMP_Text statusText,
            string serverUrl,
            string apiKey,
            float healthCheckInterval,
            float sessionPollInterval,
            DiscordListenerProcessLauncher processLauncher,
            string[] channelIds)
        {
            _host = host;
            _session = session;
            _statusImage = statusImage;
            _statusText = statusText;
            _serverUrl = serverUrl;
            _apiKey = apiKey;
            _healthCheckInterval = healthCheckInterval;
            _sessionPollInterval = sessionPollInterval;
            _configuredProcessLauncher = processLauncher;
            _channelIds = channelIds;
        }

        public void Initialize()
        {
            AnswerListener = _host.GetComponent<DiscordAnswerListener>();
            if (AnswerListener == null)
                AnswerListener = _host.gameObject.AddComponent<DiscordAnswerListener>();

            ProcessLauncher = _configuredProcessLauncher ?? _host.GetComponent<DiscordListenerProcessLauncher>();
            if (ProcessLauncher == null)
                ProcessLauncher = _host.gameObject.AddComponent<DiscordListenerProcessLauncher>();

            ProcessLauncher.StartListener(_apiKey, _serverUrl, _channelIds);

            AnswerListener.Configure(
                _serverUrl,
                _apiKey,
                _healthCheckInterval,
                _sessionPollInterval);
            AnswerListener.StatusChanged += UpdateStatusView;
            UpdateStatusView();
            _refreshRoutine = _host.StartCoroutine(RefreshStatus());
        }

        public void StopListening()
        {
            AnswerListener?.StopListening();
        }

        public void RestartListener()
        {
            ProcessLauncher?.RestartListener();
        }

        public void Dispose()
        {
            if (_refreshRoutine != null)
            {
                _host.StopCoroutine(_refreshRoutine);
                _refreshRoutine = null;
            }

            if (AnswerListener != null)
                AnswerListener.StatusChanged -= UpdateStatusView;
        }

        private void UpdateStatusView()
        {
            if (_statusImage != null)
            {
                if (AnswerListener == null || !AnswerListener.IsBackendConnected)
                    _statusImage.color = Color.red;
                else if (!AnswerListener.IsChannelVisible)
                    _statusImage.color = Color.yellow;
                else
                    _statusImage.color = Color.green;
            }

            if (_statusText == null)
                return;

            if (AnswerListener == null)
            {
                _statusText.text = _session?.Config?.settings?.FormatDiscordUnavailable()
                    ?? GameSettings.DefaultDiscordUnavailableFormat;
                _statusText.color = new Color(1f, 0.54f, 0.54f);
                return;
            }

            _statusText.text = AnswerListener.StatusMessage;
            _statusText.color = AnswerListener.IsBackendConnected
                ? AnswerListener.IsListening ? new Color(0.22f, 0.85f, 0.59f) : Color.white
                : new Color(1f, 0.54f, 0.54f);
        }

        private IEnumerator RefreshStatus()
        {
            var delay = new WaitForSecondsRealtime(0.25f);
            while (true)
            {
                UpdateStatusView();
                yield return delay;
            }
        }
    }
}
