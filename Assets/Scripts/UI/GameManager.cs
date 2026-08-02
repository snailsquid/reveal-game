using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RevealGame.Data;
using RevealGame.Gameplay;

namespace RevealGame.UI
{
    /// <summary>
    /// Main game coordinator. Keeps the public game-flow API used by UI controllers
    /// while delegating session, navigation, and Discord concerns to focused services.
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

        private GameSessionCoordinator _gameSessionCoordinator;
        private ScreenNavigator _screenNavigator;
        private DiscordStatusCoordinator _discordStatusCoordinator;

        public LootboxEngine LootboxEngine => _gameSessionCoordinator?.LootboxEngine;
        public HintGameEngine HintGameEngine => _gameSessionCoordinator?.HintGameEngine;
        public GameSession Session => _gameSessionCoordinator?.Session;
        public MediaDisplayController MediaDisplay => _mediaDisplay;
        public DiscordAnswerListener DiscordAnswerListener => _discordStatusCoordinator?.AnswerListener;
        public DiscordListenerProcessLauncher DiscordListenerProcessLauncher => _discordStatusCoordinator?.ProcessLauncher;

        private void Awake()
        {
            ButtonInteractionAnimationInstaller.EnsureInstalled();
            _gameSessionCoordinator = new GameSessionCoordinator();
            _gameSessionCoordinator.InitializeSession(GameSession.Instance);

            _screenNavigator = new ScreenNavigator(_titleScreen, _lootboxScreen, _hintGameScreen);
            _discordStatusCoordinator = new DiscordStatusCoordinator(
                this,
                Session,
                _botStatusImage,
                _botStatusText,
                _discordServerUrl,
                _discordServerApiKey,
                _discordHealthCheckInterval,
                _discordSessionPollInterval,
                _discordListenerProcessLauncher,
                _channelIds);
            _discordStatusCoordinator.Initialize();

            if (GetComponent<ResponsiveUiLayout>() == null)
                gameObject.AddComponent<ResponsiveUiLayout>();
        }

        private void OnDestroy()
        {
            _discordStatusCoordinator?.Dispose();
        }

        private void Start()
        {
            EnsureScreenNavigator();
            _screenNavigator.ResolveScreenReferences(transform.root);
            SyncScreenReferences();

            if (_mediaDisplay == null)
                _mediaDisplay = FindAnyObjectByType<MediaDisplayController>();

            ShowTitleScreen();
        }

        private void EnsureScreenNavigator()
        {
            if (_screenNavigator == null)
                _screenNavigator = new ScreenNavigator(_titleScreen, _lootboxScreen, _hintGameScreen);
        }

        private void SyncScreenReferences()
        {
            _titleScreen = _screenNavigator.TitleScreen;
            _lootboxScreen = _screenNavigator.LootboxScreen;
            _hintGameScreen = _screenNavigator.HintGameScreen;
        }

        /// <summary>
        /// Shows only the title screen, hiding all others.
        /// </summary>
        public void ShowTitleScreen()
        {
            EnsureScreenNavigator();
            _discordStatusCoordinator?.StopListening();
            _screenNavigator.ShowTitleScreen();
            UiSoundPlayer.PlayDefaultMusic();
        }

        /// <summary>
        /// Shows the lootbox selection screen and refreshes its grid.
        /// </summary>
        public void ShowLootboxScreen()
        {
            EnsureScreenNavigator();
            _discordStatusCoordinator?.StopListening();
            _screenNavigator.ShowLootboxScreen();
            UiSoundPlayer.PlayDefaultMusic();

            var controller = _screenNavigator.LootboxScreen?.GetComponent<LootboxScreenController>();
            if (controller != null)
                controller.RefreshGrid();
        }

        /// <summary>
        /// Shows the hint game screen for the selected lootbox.
        /// </summary>
        public void ShowHintGameScreen()
        {
            EnsureScreenNavigator();
            _screenNavigator.ShowHintGameScreen();
            UiSoundPlayer.PlayDefaultMusic();
        }

        public IReadOnlyList<string> OpenLootboxForReveal()
        {
            var winners = LootboxEngine.OpenLootbox();
            Debug.Log($"[GameManager] Lootbox opened with {winners.Count} items");
            return winners;
        }

        public void CompleteLootboxOpening()
        {
            _discordStatusCoordinator?.StopListening();
            ShowLootboxScreen();
        }

        public void CancelLootboxOpening()
        {
            _discordStatusCoordinator?.StopListening();
            Session?.ClearCurrentLootboxSelection();
            ShowLootboxScreen();
        }

        /// <summary>
        /// Initializes a new game session with config data.
        /// Call this after parsing a YAML/ZIP file.
        /// </summary>
        public void InitializeGame(GameConfig config)
        {
            _gameSessionCoordinator.InitializeGame(config);
            Debug.Log("[GameManager] Game initialized with config");
        }

        /// <summary>
        /// Initializes gameplay engines after a configuration has been loaded.
        /// </summary>
        public void InitializeEngines()
        {
            _gameSessionCoordinator.InitializeEngines();
        }

        public void RestartDiscordListener()
        {
            _discordStatusCoordinator?.RestartListener();
        }

        public void SetOpeningStrategy(ILootboxOpeningStrategy strategy)
        {
            _gameSessionCoordinator.SetOpeningStrategy(strategy);
        }

        public void SetOpeningStrategy(string strategyId)
        {
            _gameSessionCoordinator.SetOpeningStrategy(strategyId);
        }
    }
}
