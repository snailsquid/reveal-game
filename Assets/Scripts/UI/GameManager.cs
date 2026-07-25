using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RevealGame.Data;
using RevealGame.Gameplay;

namespace RevealGame.UI
{
    /// <summary>
    /// Main game orchestrator. Initializes engines, manages screen transitions,
    /// and coordinates the game flow: Title -> Lootbox Selection -> Hint Game -> Result.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Screen References")]
        [SerializeField] private GameObject _titleScreen;
        [SerializeField] private GameObject _lootboxScreen;
        [SerializeField] private GameObject _hintGameScreen;

        [Header("Dependencies")]
        [SerializeField] private MediaDisplayController _mediaDisplay;
        [SerializeField] private Image _botStatusImage;
        [SerializeField] private TMP_Text _botStatusText;

        [Header("Discord Answer Server")]
        [SerializeField] private string _discordServerUrl = "http:" + "/" + "/localhost:3000";
        [SerializeField] private string _discordServerApiKey;
        [SerializeField, Min(1f)] private float _discordHealthCheckInterval = 2f;
        [SerializeField, Min(0.25f)] private float _discordSessionPollInterval = 1f;
        [SerializeField] private DiscordListenerProcessLauncher _discordListenerProcessLauncher;
        [SerializeField] private string[] _channelIds;

        private GameSession _session;
        private LootboxEngine _lootboxEngine;
        private HintGameEngine _hintGameEngine;
        private readonly LootboxStrategyRegistry _strategyRegistry = new LootboxStrategyRegistry();
        private ILootboxOpeningStrategy _openingStrategy;
        private bool _enginesInitialized;
        private DiscordAnswerListener _discordAnswerListener;
        private Coroutine _botStatusRefreshRoutine;

        // Public accessors - lazily create engines after config is set
        public LootboxEngine LootboxEngine
        {
            get
            {
                EnsureEngines();
                return _lootboxEngine;
            }
        }
        public HintGameEngine HintGameEngine
        {
            get
            {
                EnsureEngines();
                return _hintGameEngine;
            }
        }
        public GameSession Session => _session;
        public MediaDisplayController MediaDisplay => _mediaDisplay;
        public DiscordAnswerListener DiscordAnswerListener => _discordAnswerListener;
        public DiscordListenerProcessLauncher DiscordListenerProcessLauncher => _discordListenerProcessLauncher;

        private void Awake()
        {
            _session = GameSession.Instance;
            _openingStrategy = _strategyRegistry.GetRequired(DeclaredWinnersOpeningStrategy.StrategyId);

            _discordAnswerListener = GetComponent<DiscordAnswerListener>();
            if (_discordAnswerListener == null)
                _discordAnswerListener = gameObject.AddComponent<DiscordAnswerListener>();

            if (_discordListenerProcessLauncher == null)
                _discordListenerProcessLauncher = GetComponent<DiscordListenerProcessLauncher>();
            if (_discordListenerProcessLauncher == null)
                _discordListenerProcessLauncher = gameObject.AddComponent<DiscordListenerProcessLauncher>();

            _discordListenerProcessLauncher.StartListener(_discordServerApiKey, _discordServerUrl, _channelIds);

            _discordAnswerListener.Configure(
                _discordServerUrl,
                _discordServerApiKey,
                _discordHealthCheckInterval,
                _discordSessionPollInterval);
            _discordAnswerListener.StatusChanged += UpdateBotStatusImage;
            UpdateBotStatusImage();
            _botStatusRefreshRoutine = StartCoroutine(RefreshBotStatus());

            if (GetComponent<ResponsiveUiLayout>() == null)
                gameObject.AddComponent<ResponsiveUiLayout>();
        }

        private void UpdateBotStatusImage()
        {
            if (_botStatusImage != null)
            {
                if (_discordAnswerListener == null || !_discordAnswerListener.IsBackendConnected)
                    _botStatusImage.color = Color.red;
                else if (!_discordAnswerListener.IsChannelVisible)
                    _botStatusImage.color = Color.yellow;
                else
                    _botStatusImage.color = Color.green;
            }

            if (_botStatusText == null)
                return;

            if (_discordAnswerListener == null)
            {
                _botStatusText.text = _session?.Config?.settings?.FormatDiscordUnavailable() ?? Data.GameSettings.DefaultDiscordUnavailableFormat;
                _botStatusText.color = new Color(1f, 0.54f, 0.54f);
                return;
            }

            _botStatusText.text = _discordAnswerListener.StatusMessage;
            _botStatusText.color = _discordAnswerListener.IsBackendConnected
                ? _discordAnswerListener.IsListening ? new Color(0.22f, 0.85f, 0.59f) : Color.white
                : new Color(1f, 0.54f, 0.54f);
        }

        private System.Collections.IEnumerator RefreshBotStatus()
        {
            var delay = new WaitForSecondsRealtime(0.25f);
            while (true)
            {
                UpdateBotStatusImage();
                yield return delay;
            }
        }

        private void OnDestroy()
        {
            if (_botStatusRefreshRoutine != null)
                StopCoroutine(_botStatusRefreshRoutine);
            if (_discordAnswerListener != null)
                _discordAnswerListener.StatusChanged -= UpdateBotStatusImage;
        }

        private void EnsureEngines()
        {
            if (_enginesInitialized && _lootboxEngine != null && _hintGameEngine != null) return;
            if (_session?.Config == null)
            {
                throw new InvalidOperationException("GameManager: GameSession not initialized. Call InitializeGame first.");
            }
            _lootboxEngine = new LootboxEngine(_session, _openingStrategy);
            _hintGameEngine = new HintGameEngine(_session);
            _enginesInitialized = true;
        }

        /// <summary>
        /// Initializes engines after config is set. Called by GameInitializer or manually.
        /// </summary>
        public void InitializeEngines()
        {
            if (_session?.Config != null)
            {
                _lootboxEngine = new LootboxEngine(_session, _openingStrategy);
                _hintGameEngine = new HintGameEngine(_session);
                _enginesInitialized = true;
            }
        }

        private void Start()
        {
            // Start on the title screen
            ShowTitleScreen();
        }

        /// <summary>
        /// Shows only the title screen, hides all others.
        /// </summary>
        public void ShowTitleScreen()
        {
            _discordAnswerListener?.StopListening();
            _titleScreen.SetActive(true);
            _lootboxScreen.SetActive(false);
            _hintGameScreen.SetActive(false);
        }

        /// <summary>
        /// Shows the lootbox selection screen. Called after Play on title screen.
        /// </summary>
        public void ShowLootboxScreen()
        {
            _discordAnswerListener?.StopListening();
            _titleScreen.SetActive(false);
            _lootboxScreen.SetActive(true);
            _hintGameScreen.SetActive(false);

            // Notify the lootbox screen to refresh its grid
            var controller = _lootboxScreen.GetComponent<LootboxScreenController>();
            if (controller != null)
                controller.RefreshGrid();
        }

        /// <summary>
        /// Shows the hint game screen for a selected lootbox.
        /// </summary>
        public void ShowHintGameScreen()
        {
            _titleScreen.SetActive(false);
            _lootboxScreen.SetActive(false);
            _hintGameScreen.SetActive(true);

            _hintGameEngine.GetOrAssignHint();
            var controller = _hintGameScreen.GetComponent<HintGameUIController>();
            if (controller != null)
                controller.ShowCurrentHint();
        }

        public IReadOnlyList<string> OpenLootboxForReveal()
        {
            var winners = _lootboxEngine.OpenLootbox();
            Debug.Log($"[GameManager] Lootbox opened with {winners.Count} items");
            return winners;
        }

        public void CompleteLootboxOpening()
        {
            _discordAnswerListener?.StopListening();
            ShowLootboxScreen();
        }

        public void CancelLootboxOpening()
        {
            _discordAnswerListener?.StopListening();
            _session?.ClearCurrentLootboxSelection();
            ShowLootboxScreen();
        }

        /// <summary>
        /// Initializes a new game session with config data.
        /// Call this after parsing a YAML/ZIP file.
        /// </summary>
        public void InitializeGame(GameConfig config)
        {
            _session.Initialize(config);
            _enginesInitialized = false;
            Debug.Log("[GameManager] Game initialized with config");
        }

        public void RestartDiscordListener()
        {
            _discordListenerProcessLauncher?.RestartListener();
        }

        public void SetOpeningStrategy(ILootboxOpeningStrategy strategy)
        {
            _strategyRegistry.Register(strategy);
            _openingStrategy = _strategyRegistry.GetRequired(strategy.Id);
            _enginesInitialized = false;
        }

        public void SetOpeningStrategy(string strategyId)
        {
            _openingStrategy = _strategyRegistry.GetRequired(strategyId);
            _enginesInitialized = false;
        }
    }
}
