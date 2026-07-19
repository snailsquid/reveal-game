using System;
using System.Collections.Generic;
using System.Linq;
using RevealGame.Data;

namespace RevealGame.Gameplay
{
    public interface ILootboxOpeningStrategy
    {
        string Id { get; }

        IReadOnlyList<string> SelectRewardItemIds(GameConfig config, LootboxState lootbox, Random random);
    }

    public sealed class DeclaredWinnersOpeningStrategy : ILootboxOpeningStrategy
    {
        public const string StrategyId = "declared-winners";

        public string Id => StrategyId;

        public IReadOnlyList<string> SelectRewardItemIds(GameConfig config, LootboxState lootbox, Random random)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            return (config.items ?? Array.Empty<ItemData>())
                .Where(item => item != null && item.isWinner && lootbox.assignedItemIds.Contains(item.id))
                .Select(item => item.id)
                .ToArray();
        }
    }

    public sealed class RandomWinnerOpeningStrategy : ILootboxOpeningStrategy
    {
        public const string StrategyId = "random-winner";

        public string Id => StrategyId;

        public IReadOnlyList<string> SelectRewardItemIds(GameConfig config, LootboxState lootbox, Random random)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (random == null)
                throw new ArgumentNullException(nameof(random));

            var winnerIds = (config.items ?? Array.Empty<ItemData>())
                .Where(item => item != null && item.isWinner && lootbox.assignedItemIds.Contains(item.id))
                .Select(item => item.id)
                .ToArray();

            return winnerIds.Length == 0
                ? Array.Empty<string>()
                : new[] { winnerIds[random.Next(winnerIds.Length)] };
        }
    }

    public sealed class LootboxStrategyRegistry
    {
        private readonly Dictionary<string, ILootboxOpeningStrategy> _strategies =
            new Dictionary<string, ILootboxOpeningStrategy>(StringComparer.OrdinalIgnoreCase);

        public LootboxStrategyRegistry()
        {
            Register(new DeclaredWinnersOpeningStrategy());
            Register(new RandomWinnerOpeningStrategy());
        }

        public void Register(ILootboxOpeningStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));
            if (string.IsNullOrWhiteSpace(strategy.Id))
                throw new ArgumentException("A lootbox strategy must provide an ID.", nameof(strategy));

            _strategies[strategy.Id] = strategy;
        }

        public ILootboxOpeningStrategy GetRequired(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || !_strategies.TryGetValue(id, out var strategy))
                throw new KeyNotFoundException($"No lootbox opening strategy is registered with ID '{id}'.");

            return strategy;
        }
    }
}
