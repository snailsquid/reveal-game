using System;
using System.Collections.Generic;
using System.Linq;
using RevealGame.Gameplay;
using UnityEngine;

namespace RevealGame.Data
{
    /// <summary>
    /// Singleton that persists across scenes via DontDestroyOnLoad.
    /// Holds the current game configuration and mutable lootbox state.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        private static GameSession _instance;

        public static GameSession Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<GameSession>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GameSession");
                        _instance = go.AddComponent<GameSession>();
                    }
                }
                return _instance;
            }
        }

        private GameConfig _config;
        private List<LootboxState> _lootboxStates;
        private int _currentLootboxIndex = -1;
        private System.Random _rng;

        public GameConfig Config => _config;
        public IReadOnlyList<LootboxState> LootboxStates => _lootboxStates;
        public int CurrentLootboxIndex => _currentLootboxIndex;
        public LootboxState CurrentLootbox => _currentLootboxIndex >= 0 && _currentLootboxIndex < _lootboxStates.Count
            ? _lootboxStates[_currentLootboxIndex]
            : null;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
            _rng = new System.Random();
        }

        /// <summary>
        /// Initializes a new game session with the given configuration.
        /// </summary>
        public void Initialize(GameConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            _config = config;
            _lootboxStates = new List<LootboxState>();

            int numBoxes = config.settings?.nLootbox ?? 1;
            var itemAssignments = LootboxItemDistributor.Distribute(config, _rng ??= new System.Random());
            for (int i = 0; i < numBoxes; i++)
            {
                var lootbox = new LootboxState(i);
                lootbox.AssignItemIds(itemAssignments[i]);
                _lootboxStates.Add(lootbox);
            }

            _currentLootboxIndex = -1;
            Debug.Log($"[GameSession] Initialized with {numBoxes} lootboxes, {config.items?.Length ?? 0} items, {config.hints?.Length ?? 0} hints");
        }

        /// <summary>
        /// Selects a lootbox by index to interact with.
        /// </summary>
        public LootboxState SelectLootbox(int index)
        {
            if (index < 0 || index >= _lootboxStates.Count)
                throw new ArgumentOutOfRangeException(nameof(index), $"Lootbox index must be between 0 and {_lootboxStates.Count - 1}");

            if (_lootboxStates[index].isOpened)
                throw new InvalidOperationException($"Lootbox {index} is already opened");

            _currentLootboxIndex = index;
            return _lootboxStates[index];
        }

        public void ClearCurrentLootboxSelection()
        {
            _currentLootboxIndex = -1;
        }

        /// <summary>
        /// Gets the current hint for the active lootbox.
        /// </summary>
        public HintData GetCurrentHint()
        {
            var lootbox = CurrentLootbox;
            if (lootbox == null || string.IsNullOrEmpty(lootbox.currentHintId))
                return null;

            return _config.hints.FirstOrDefault(h => h.id == lootbox.currentHintId);
        }

        /// <summary>
        /// Assigns a new hint to the current lootbox.
        /// </summary>
        public HintData AssignNewHint()
        {
            var lootbox = CurrentLootbox;
            if (lootbox == null)
                throw new InvalidOperationException("No lootbox selected");

            string hintId = lootbox.GetRandomUncompletedHint(_config.hints.ToList(), _rng);
            if (hintId == null)
                return null; // All hints completed

            lootbox.currentHintId = hintId;
            return _config.hints.FirstOrDefault(h => h.id == hintId);
        }

        /// <summary>
        /// Marks the current hint as completed for the active lootbox.
        /// </summary>
        public void CompleteCurrentHint()
        {
            var lootbox = CurrentLootbox;
            if (lootbox == null || string.IsNullOrEmpty(lootbox.currentHintId))
                return;

            lootbox.CompleteHint(lootbox.currentHintId);
        }

        /// <summary>
        /// Opens the current lootbox with the given winners.
        /// </summary>
        public void OpenCurrentLootbox(List<string> winnerItemIds)
        {
            var lootbox = CurrentLootbox;
            if (lootbox == null)
                throw new InvalidOperationException("No lootbox selected");

            lootbox.Open(winnerItemIds);
        }

        public void OpenCurrentLootbox()
        {
            var lootbox = CurrentLootbox;
            if (lootbox == null)
                throw new InvalidOperationException("No lootbox selected");

            lootbox.Open();
        }

        /// <summary>
        /// Gets all items that are winners.
        /// </summary>
        public List<ItemData> GetWinners()
        {
            return _config.items.Where(i => i.isWinner).ToList();
        }

        /// <summary>
        /// Gets all unopened lootboxes.
        /// </summary>
        public List<LootboxState> GetUnopenedLootboxes()
        {
            return _lootboxStates.Where(l => !l.isOpened).ToList();
        }

        /// <summary>
        /// Checks if the game is complete (all lootboxes opened).
        /// </summary>
        public bool IsGameComplete()
        {
            return _lootboxStates.All(l => l.isOpened);
        }

        /// <summary>
        /// Resets the game session (for testing).
        /// </summary>
        public void Reset()
        {
            _config = null;
            _lootboxStates = null;
            _currentLootboxIndex = -1;
            _rng = new System.Random();
        }
    }
}
