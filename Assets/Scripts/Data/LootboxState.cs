using System;
using System.Collections.Generic;

namespace RevealGame.Data
{
    /// <summary>
    /// Mutable runtime state for a single lootbox.
    /// Tracks whether it's been opened, which items won, and which hints have been solved.
    /// </summary>
    [Serializable]
    public class LootboxState
    {
        public int index; // Lootbox index (0-based)
        public bool isOpened; // Whether this lootbox has been opened
        public List<string> assignedItemIds;
        public List<string> winnerItemIds;
        public List<string> completedHintIds; // IDs of hints the player has solved for this box
        public string currentHintId; // ID of hint currently being solved (null if none)

        public LootboxState(int index)
        {
            this.index = index;
            isOpened = false;
            assignedItemIds = new List<string>();
            winnerItemIds = new List<string>();
            completedHintIds = new List<string>();
            currentHintId = null;
        }

        /// <summary>
        /// Marks a hint as completed for this lootbox.
        /// </summary>
        public void CompleteHint(string hintId)
        {
            if (!completedHintIds.Contains(hintId))
            {
                completedHintIds.Add(hintId);
            }
            currentHintId = null;
        }

        /// <summary>
        /// Marks this lootbox as opened with the given winners.
        /// </summary>
        public void Open(List<string> winners)
        {
            isOpened = true;
            winnerItemIds = winners ?? new List<string>();
        }

        public void AssignItemIds(List<string> itemIds)
        {
            assignedItemIds = itemIds ?? new List<string>();
        }

        public void Open()
        {
            isOpened = true;
        }

        /// <summary>
        /// Checks if all hints have been completed.
        /// </summary>
        public bool AllHintsCompleted(int totalHints)
        {
            return completedHintIds.Count >= totalHints;
        }

        /// <summary>
        /// Gets a random uncompleted hint ID from the available hints.
        /// </summary>
        public string GetRandomUncompletedHint(List<HintData> allHints, Random rng)
        {
            var available = new List<string>();
            foreach (var hint in allHints)
            {
                if (!completedHintIds.Contains(hint.id))
                {
                    available.Add(hint.id);
                }
            }

            if (available.Count == 0)
                return null;

            return available[rng.Next(available.Count)];
        }
    }
}
