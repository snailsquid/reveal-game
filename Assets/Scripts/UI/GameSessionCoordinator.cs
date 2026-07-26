using System;
using RevealGame.Data;
using RevealGame.Gameplay;

namespace RevealGame.UI
{
    /// <summary>
    /// Owns the session-facing gameplay services used by the UI.
    /// Keeps engine construction and opening-strategy selection out of the scene coordinator.
    /// </summary>
    public sealed class GameSessionCoordinator
    {
        private readonly LootboxStrategyRegistry _strategyRegistry = new LootboxStrategyRegistry();
        private GameSession _session;
        private LootboxEngine _lootboxEngine;
        private HintGameEngine _hintGameEngine;
        private ILootboxOpeningStrategy _openingStrategy;
        private bool _enginesInitialized;

        public GameSession Session => _session;

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

        public void InitializeSession(GameSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _openingStrategy = _strategyRegistry.GetRequired(DeclaredWinnersOpeningStrategy.StrategyId);
            _enginesInitialized = false;
        }

        public void InitializeGame(GameConfig config)
        {
            EnsureSession();
            _session.Initialize(config);
            _enginesInitialized = false;
        }

        public void InitializeEngines()
        {
            if (_session?.Config == null)
                return;

            CreateEngines();
        }

        public void SetOpeningStrategy(ILootboxOpeningStrategy strategy)
        {
            if (strategy == null)
                throw new ArgumentNullException(nameof(strategy));

            _strategyRegistry.Register(strategy);
            _openingStrategy = _strategyRegistry.GetRequired(strategy.Id);
            _enginesInitialized = false;
        }

        public void SetOpeningStrategy(string strategyId)
        {
            _openingStrategy = _strategyRegistry.GetRequired(strategyId);
            _enginesInitialized = false;
        }

        private void EnsureSession()
        {
            if (_session == null)
                throw new InvalidOperationException("GameSessionCoordinator: Session has not been initialized.");
        }

        private void EnsureEngines()
        {
            EnsureSession();
            if (_enginesInitialized && _lootboxEngine != null && _hintGameEngine != null)
                return;

            if (_session.Config == null)
            {
                throw new InvalidOperationException(
                    "GameSessionCoordinator: GameSession not initialized. Call InitializeGame first.");
            }

            CreateEngines();
        }

        private void CreateEngines()
        {
            _lootboxEngine = new LootboxEngine(_session, _openingStrategy);
            _hintGameEngine = new HintGameEngine(_session);
            _enginesInitialized = true;
        }
    }
}
