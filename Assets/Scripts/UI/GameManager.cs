using System;
using System.Collections.Generic;
using UnityEngine;
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

        private GameSession _session;
        private LootboxEngine _lootboxEngine;
        private HintGameEngine _hintGameEngine;
        private readonly LootboxStrategyRegistry _strategyRegistry = new LootboxStrategyRegistry();
        private ILootboxOpeningStrategy _openingStrategy;
        private bool _enginesInitialized;

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

        private void Awake()
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                if (_titleScreen == null)
                {
                    var t = canvas.transform.Find("TitleScreen");
                    if (t != null) _titleScreen = t.gameObject;
                }
                if (_lootboxScreen == null)
                {
                    var l = canvas.transform.Find("LootboxScreen");
                    if (l != null) _lootboxScreen = l.gameObject;
                }
                if (_hintGameScreen == null)
                {
                    var h = canvas.transform.Find("HintGameScreen");
                    if (h != null) _hintGameScreen = h.gameObject;
                }
            }

            if (_mediaDisplay == null)
                _mediaDisplay = FindAnyObjectByType<MediaDisplayController>();

            _session = GameSession.Instance;
            _openingStrategy = _strategyRegistry.GetRequired(DeclaredWinnersOpeningStrategy.StrategyId);

            if (GetComponent<ResponsiveUiLayout>() == null)
                gameObject.AddComponent<ResponsiveUiLayout>();
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
            _titleScreen.SetActive(true);
            _lootboxScreen.SetActive(false);
            _hintGameScreen.SetActive(false);
        }

        /// <summary>
        /// Shows the lootbox selection screen. Called after Play on title screen.
        /// </summary>
        public void ShowLootboxScreen()
        {
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

            // Assign a new hint and notify the hint game UI
            _hintGameEngine.AssignNewHint();
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
            ShowLootboxScreen();
        }

        public void CancelLootboxOpening()
        {
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
