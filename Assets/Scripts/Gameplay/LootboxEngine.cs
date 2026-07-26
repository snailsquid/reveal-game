using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RevealGame.Gameplay
{
    /// <summary>
    /// Manages lootbox selection, opening, and winner picking.
    /// Coordinates between GameSession and WinnerDetector.
    /// </summary>
    public class LootboxEngine
    {
        private readonly Data.GameSession _session;
        private readonly ILootboxOpeningStrategy _openingStrategy;
        private readonly System.Random _rng;

        public LootboxEngine(Data.GameSession session, ILootboxOpeningStrategy openingStrategy = null, System.Random rng = null)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _openingStrategy = openingStrategy ?? new DeclaredWinnersOpeningStrategy();
            _rng = rng ?? new System.Random();
        }

        public string OpeningStrategyId => _openingStrategy.Id;

        /// <summary>
        /// Gets the total number of lootboxes.
        /// </summary>
        public int GetLootboxCount()
        {
            return _session.LootboxStates?.Count ?? 0;
        }

        /// <summary>
        /// Gets all lootbox states.
        /// </summary>
        public IReadOnlyList<Data.LootboxState> GetAllLootboxStates()
        {
            return _session.LootboxStates;
        }

        /// <summary>
        /// Selects a lootbox by index to interact with.
        /// </summary>
        public Data.LootboxState SelectLootbox(int index)
        {
            return _session.SelectLootbox(index);
        }

        /// <summary>
        /// Gets the currently selected lootbox.
        /// </summary>
        public Data.LootboxState GetCurrentLootbox()
        {
            return _session.CurrentLootbox;
        }

        /// <summary>
        /// Opens the current lootbox and returns its assigned winner item IDs.
        /// </summary>
        public List<string> OpenLootbox()
        {
            var lootbox = _session.CurrentLootbox;
            if (lootbox == null)
                throw new InvalidOperationException("No lootbox selected");

            if (lootbox.isOpened)
                throw new InvalidOperationException($"Lootbox {lootbox.index} is already opened");

            var winnerIds = _openingStrategy.SelectRewardItemIds(_session.Config, lootbox, _rng).ToList();
            _session.OpenCurrentLootbox(winnerIds);

            Debug.Log($"[LootboxEngine] Lootbox {lootbox.index} opened with {winnerIds.Count} winner items");
            return winnerIds;
        }

        /// <summary>
        /// Gets the winner items for the current lootbox.
        /// </summary>
        public List<Data.ItemData> GetCurrentWinners()
        {
            var lootbox = _session.CurrentLootbox;
            if (lootbox == null || !lootbox.isOpened)
                return new List<Data.ItemData>();

            return lootbox.winnerItemIds
                .Select(GetItemById)
                .Where(item => item != null)
                .ToList();
        }

        /// <summary>
        /// Gets all items assigned to the currently selected lootbox, before it opens.
        /// </summary>
        public List<Data.ItemData> GetCurrentContents()
        {
            var lootbox = _session.CurrentLootbox;
            if (lootbox == null || lootbox.assignedItemIds == null)
                return new List<Data.ItemData>();

            return lootbox.assignedItemIds
                .Select(GetItemById)
                .Where(item => item != null)
                .ToList();
        }

        /// <summary>
        /// Checks if a specific lootbox has been opened.
        /// </summary>
        public bool IsLootboxOpened(int index)
        {
            if (index < 0 || index >= _session.LootboxStates.Count)
                return false;

            return _session.LootboxStates[index].isOpened;
        }

        /// <summary>
        /// Gets the number of unopened lootboxes.
        /// </summary>
        public int GetUnopenedCount()
        {
            return _session.GetUnopenedLootboxes().Count;
        }

        /// <summary>
        /// Checks if the game is complete (all lootboxes opened).
        /// </summary>
        public bool IsGameComplete()
        {
            return _session.IsGameComplete();
        }

        /// <summary>
        /// Gets all items in the game.
        /// </summary>
        public List<Data.ItemData> GetAllItems()
        {
            return _session.Config.items?.ToList() ?? new List<Data.ItemData>();
        }

        /// <summary>
        /// Gets a specific item by ID.
        /// </summary>
        public Data.ItemData GetItemById(string itemId)
        {
            return _session.Config.items?.FirstOrDefault(i => i.id == itemId);
        }

        /// <summary>
        /// Resets the lootbox engine (for testing).
        /// </summary>
        public void Reset()
        {
            _session.Reset();
        }
    }
}
