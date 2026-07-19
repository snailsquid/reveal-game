using System;
using System.Collections.Generic;
using System.Linq;

namespace RevealGame.Gameplay
{
    /// <summary>
    /// Determines which items are winners based on isWinner flags in the config.
    /// </summary>
    public class WinnerDetector
    {
        private readonly Data.GameConfig _config;

        public WinnerDetector(Data.GameConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// Gets all items marked as winners.
        /// </summary>
        public List<Data.ItemData> GetWinners()
        {
            return _config.items.Where(i => i.isWinner).ToList();
        }

        /// <summary>
        /// Gets the IDs of all winner items.
        /// </summary>
        public List<string> GetWinnerIds()
        {
            return _config.items.Where(i => i.isWinner).Select(i => i.id).ToList();
        }

        /// <summary>
        /// Checks if a specific item is a winner.
        /// </summary>
        public bool IsWinner(string itemId)
        {
            var item = _config.items.FirstOrDefault(i => i.id == itemId);
            return item?.isWinner ?? false;
        }

        /// <summary>
        /// Gets the winner item data by ID.
        /// </summary>
        public Data.ItemData GetWinnerById(string itemId)
        {
            return _config.items.FirstOrDefault(i => i.id == itemId && i.isWinner);
        }

        /// <summary>
        /// Gets all non-winner items.
        /// </summary>
        public List<Data.ItemData> GetNonWinners()
        {
            return _config.items.Where(i => !i.isWinner).ToList();
        }

        /// <summary>
        /// Checks if there are any winners in the config.
        /// </summary>
        public bool HasWinners()
        {
            return _config.items.Any(i => i.isWinner);
        }

        /// <summary>
        /// Gets the count of winner items.
        /// </summary>
        public int WinnerCount()
        {
            return _config.items.Count(i => i.isWinner);
        }
    }
}
