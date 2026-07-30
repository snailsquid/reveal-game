using System;
using System.Collections;
using RevealGame.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace RevealGame.UI
{
    public sealed class DiscordAnswerListener : MonoBehaviour
    {
        private const int RequestTimeoutSeconds = 8;

        private string _baseUrl;
        private string _apiKey;
        private float _healthCheckInterval;
        private float _sessionPollInterval;
        private Coroutine _healthMonitor;
        private Coroutine _sessionPoll;
        private string _activeSessionId;
        private HintData _pendingHint;
        private bool _isStartingSession;
        private int _sessionGeneration;

        public bool IsBackendConnected { get; private set; }
        public bool IsChannelVisible { get; private set; }
        public bool IsListening { get; private set; }
        public bool IsStartingSession => _isStartingSession;
        public string StatusMessage { get; private set; } = "Discord server is not configured.";

        public event Action StatusChanged;
        public event Action<DiscordAnswerMatch> AnswerMatched;

        public void Configure(string baseUrl, string apiKey, float healthCheckInterval, float sessionPollInterval)
        {
            _baseUrl = NormalizeBaseUrl(baseUrl);
            _apiKey = apiKey?.Trim();
            _healthCheckInterval = Mathf.Max(1f, healthCheckInterval);
            _sessionPollInterval = Mathf.Max(0.25f, sessionPollInterval);

            if (_healthMonitor != null)
                StopCoroutine(_healthMonitor);

            _healthMonitor = StartCoroutine(MonitorHealth());
        }

        public void BeginListening(HintData hint)
        {
            if (hint == null)
                throw new ArgumentNullException(nameof(hint));

            StopListening();
            _pendingHint = hint;
            TryStartPendingHint();
        }

        public void StopListening()
        {
            _pendingHint = null;
            _isStartingSession = false;
            _sessionGeneration++;

            if (_sessionPoll != null)
            {
                StopCoroutine(_sessionPoll);
                _sessionPoll = null;
            }

            if (!string.IsNullOrEmpty(_activeSessionId))
            {
                string sessionToCancel = _activeSessionId;
                _activeSessionId = null;
                StartCoroutine(DeleteSession(sessionToCancel));
            }

            if (IsListening)
            {
                IsListening = false;
                UpdateStatus(IsBackendConnected ? "Discord server connected — not listening." : "Discord server unavailable.");
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Raises a successful Discord answer using the answer for the hint currently being listened for.
        /// This is intended for the Unity editor's Discord answer simulator.
        /// </summary>
        /// <param name="username">The simulated Discord username.</param>
        /// <returns>True when a pending hint was available to simulate.</returns>
        public bool SimulateCorrectAnswer(string username)
        {
            if (_pendingHint == null)
                return false;

            string answer = _pendingHint.answer;
            StopListening();
            UpdateStatus("Discord answer received.");
            AnswerMatched?.Invoke(new DiscordAnswerMatch(username?.Trim(), answer));
            return true;
        }

        public bool HasPendingHintForEditor => _pendingHint != null;
#endif

        private IEnumerator MonitorHealth()
        {
            while (true)
            {
                yield return CheckHealth();
                yield return new WaitForSecondsRealtime(_healthCheckInterval);
            }
        }

        private IEnumerator CheckHealth()
        {
            if (!IsConfigured())
            {
                IsBackendConnected = false;
                IsChannelVisible = false;
                UpdateStatus("Discord server is not configured.");
                yield break;
            }

            using (var request = UnityWebRequest.Get(BuildUrl("/health")))
            {
                ApplyAuthentication(request);
                request.timeout = RequestTimeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    IsBackendConnected = false;
                    IsChannelVisible = false;
                    UpdateStatus("Discord server unavailable.");
                    yield break;
                }

                var health = JsonUtility.FromJson<HealthResponse>(request.downloadHandler.text);
                IsBackendConnected = health != null
                    && health.api == "ready"
                    && health.discord != null
                    && health.discord.connected;
                IsChannelVisible = IsBackendConnected && health.discord.channelVisible;
                if (!IsBackendConnected)
                {
                    UpdateStatus("Discord listener is not ready.");
                    yield break;
                }

                if (IsListening)
                    UpdateStatus("Listening for Discord answers...");
                else if (_isStartingSession)
                    UpdateStatus("Starting Discord listener...");
                else
                    UpdateStatus("Discord server connected — not listening.");

                if (IsBackendConnected)
                    TryStartPendingHint();
            }
        }

        private void TryStartPendingHint()
        {
            if (_pendingHint == null || !IsBackendConnected || _isStartingSession || IsListening)
                return;

            _isStartingSession = true;
            int generation = _sessionGeneration;
            StartCoroutine(CreateSession(_pendingHint, generation));
        }

        private IEnumerator CreateSession(HintData hint, int generation)
        {
            UpdateStatus("Starting Discord listener...");
            var requestBody = JsonUtility.ToJson(new CreateSessionRequest
            {
                answer = hint.answer,
                isCaseSensitive = hint.isCaseSensitive,
                hintId = hint.id
            });

            using (var request = CreateJsonRequest(BuildUrl("/sessions"), UnityWebRequest.kHttpVerbPOST, requestBody))
            {
                yield return request.SendWebRequest();

                if (generation != _sessionGeneration)
                    yield break;

                _isStartingSession = false;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    IsBackendConnected = false;
                    UpdateStatus("Discord listener could not start.");
                    yield break;
                }

                var session = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
                if (session == null || string.IsNullOrEmpty(session.sessionId))
                {
                    UpdateStatus("Discord listener returned an invalid session.");
                    yield break;
                }

                _activeSessionId = session.sessionId;
                IsListening = true;
                UpdateStatus("Listening for Discord answers...");
                _sessionPoll = StartCoroutine(PollSession(session.sessionId, generation));
            }
        }

        private IEnumerator PollSession(string sessionId, int generation)
        {
            while (generation == _sessionGeneration && sessionId == _activeSessionId)
            {
                yield return new WaitForSecondsRealtime(_sessionPollInterval);
                using (var request = UnityWebRequest.Get(BuildUrl("/sessions/" + sessionId)))
                {
                    ApplyAuthentication(request);
                    request.timeout = RequestTimeoutSeconds;
                    yield return request.SendWebRequest();

                    if (generation != _sessionGeneration)
                        yield break;

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        IsBackendConnected = false;
                        IsChannelVisible = false;
                        IsListening = false;
                        _activeSessionId = null;
                        UpdateStatus("Discord listener disconnected.");
                        yield break;
                    }

                    var session = JsonUtility.FromJson<SessionResponse>(request.downloadHandler.text);
                    if (session != null && session.status == "matched" && session.matchedBy != null)
                    {
                        IsListening = false;
                        _activeSessionId = null;
                        _pendingHint = null;
                        UpdateStatus("Discord answer received.");
                        AnswerMatched?.Invoke(new DiscordAnswerMatch(session.matchedBy.username, session.matchedBy.guess));
                        yield break;
                    }
                }
            }
        }

        private IEnumerator DeleteSession(string sessionId)
        {
            using (var request = UnityWebRequest.Delete(BuildUrl("/sessions/" + sessionId)))
            {
                ApplyAuthentication(request);
                request.timeout = RequestTimeoutSeconds;
                yield return request.SendWebRequest();
            }
        }

        private UnityWebRequest CreateJsonRequest(string url, string method, string body)
        {
            var request = new UnityWebRequest(url, method)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = RequestTimeoutSeconds
            };
            request.SetRequestHeader("Content-Type", "application/json");
            ApplyAuthentication(request);
            return request;
        }

        private void ApplyAuthentication(UnityWebRequest request)
        {
            request.SetRequestHeader("X-Game-Api-Key", _apiKey);
        }

        private bool IsConfigured()
        {
            return !string.IsNullOrEmpty(_baseUrl) && !string.IsNullOrEmpty(_apiKey);
        }

        private string BuildUrl(string path)
        {
            return _baseUrl + path;
        }

        private void UpdateStatus(string message)
        {
            StatusMessage = message;
            StatusChanged?.Invoke();
        }

        private static string NormalizeBaseUrl(string baseUrl)
        {
            return string.IsNullOrWhiteSpace(baseUrl) ? string.Empty : baseUrl.TrimEnd('/');
        }

        private void OnDestroy()
        {
            StopListening();
        }

        [Serializable]
        private sealed class CreateSessionRequest
        {
            public string answer;
            public bool isCaseSensitive;
            public string hintId;
        }

        [Serializable]
        private sealed class HealthResponse
        {
            public string api;
            public DiscordHealth discord;
        }

        [Serializable]
        private sealed class DiscordHealth
        {
            public bool connected;
            public bool channelVisible;
        }

        [Serializable]
        private sealed class SessionResponse
        {
            public string sessionId;
            public string status;
            public MatchedBy matchedBy;
        }

        [Serializable]
        private sealed class MatchedBy
        {
            public string username;
            public string guess;
        }
    }

    public readonly struct DiscordAnswerMatch
    {
        public DiscordAnswerMatch(string username, string guess)
        {
            Username = username;
            Guess = guess;
        }

        public string Username { get; }
        public string Guess { get; }
    }
}
