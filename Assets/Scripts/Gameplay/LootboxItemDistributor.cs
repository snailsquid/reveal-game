using System;
using System.Collections.Generic;
using RevealGame.Data;

namespace RevealGame.Gameplay
{
    public static class LootboxItemDistributor
    {
        public static IReadOnlyList<List<string>> Distribute(GameConfig config, Random random)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var items = config.items ?? Array.Empty<ItemData>();
            var lootboxCount = config.settings?.nLootbox ?? 1;
            if (lootboxCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(config), "The lootbox count must be positive.");
            if (lootboxCount > items.Length)
                throw new ArgumentException("The lootbox count cannot exceed the number of items.", nameof(config));

            var winnerIds = new List<string>();
            var nonWinnerIds = new List<string>();
            var uniqueIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.id))
                    throw new ArgumentException("Every item must have an ID.", nameof(config));
                if (!uniqueIds.Add(item.id))
                    throw new ArgumentException($"Item ID '{item.id}' is duplicated.", nameof(config));

                if (item.isWinner)
                    winnerIds.Add(item.id);
                else
                    nonWinnerIds.Add(item.id);
            }

            var assignments = new List<List<string>>(lootboxCount);
            for (var index = 0; index < lootboxCount; index++)
                assignments.Add(new List<string>());

            DistributeEvenly(winnerIds, assignments, random);
            DistributeEvenly(nonWinnerIds, assignments, random);

            return assignments;
        }

        private static void DistributeEvenly(List<string> itemIds, IReadOnlyList<List<string>> assignments, Random random)
        {
            for (var index = itemIds.Count - 1; index > 0; index--)
            {
                var swapIndex = random.Next(index + 1);
                (itemIds[index], itemIds[swapIndex]) = (itemIds[swapIndex], itemIds[index]);
            }

            for (var index = 0; index < itemIds.Count; index++)
                assignments[index % assignments.Count].Add(itemIds[index]);
        }
    }
}
