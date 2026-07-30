using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RevealGame.Data;
using RevealGame.Gameplay;
using RevealGame.Importing;

namespace RevealGame.Tests
{
    [TestFixture]
    public class LootboxStateTests
    {
        [Test]
        public void Constructor_InitializesCorrectly()
        {
            var state = new LootboxState(5);

            Assert.AreEqual(5, state.index);
            Assert.IsFalse(state.isOpened);
            Assert.IsEmpty(state.winnerItemIds);
            Assert.IsEmpty(state.completedHintIds);
            Assert.IsNull(state.currentHintId);
        }

        [Test]
        public void CompleteHint_AddsHintId()
        {
            var state = new LootboxState(0);

            state.CompleteHint("hint_1");

            Assert.Contains("hint_1", state.completedHintIds);
            Assert.IsNull(state.currentHintId);
        }

        [Test]
        public void CompleteHint_DoesNotDuplicate()
        {
            var state = new LootboxState(0);

            state.CompleteHint("hint_1");
            state.CompleteHint("hint_1");

            Assert.AreEqual(1, state.completedHintIds.Count);
        }

        [Test]
        public void Open_SetsIsOpenedAndWinners()
        {
            var state = new LootboxState(0);
            var winners = new List<string> { "item_1", "item_2" };

            state.Open(winners);

            Assert.IsTrue(state.isOpened);
            Assert.AreEqual(2, state.winnerItemIds.Count);
            Assert.Contains("item_1", state.winnerItemIds);
            Assert.Contains("item_2", state.winnerItemIds);
        }

        [Test]
        public void Open_NullWinners_EmptyList()
        {
            var state = new LootboxState(0);

            state.Open(null);

            Assert.IsTrue(state.isOpened);
            Assert.IsNotNull(state.winnerItemIds);
            Assert.IsEmpty(state.winnerItemIds);
        }

        [Test]
        public void AllHintsCompleted_ReturnsCorrectly()
        {
            var state = new LootboxState(0);
            state.completedHintIds.Add("hint_1");
            state.completedHintIds.Add("hint_2");

            Assert.IsTrue(state.AllHintsCompleted(2));
            Assert.IsFalse(state.AllHintsCompleted(3));
        }

        [Test]
        public void GetRandomUncompletedHint_ReturnsAvailableHint()
        {
            var state = new LootboxState(0);
            var hints = new List<HintData>
            {
                new HintData { id = "hint_1" },
                new HintData { id = "hint_2" },
                new HintData { id = "hint_3" }
            };

            state.completedHintIds.Add("hint_2");
            var rng = new System.Random(42); // Fixed seed for deterministic test

            string result = state.GetRandomUncompletedHint(hints, rng);

            Assert.IsNotNull(result);
            Assert.AreNotEqual("hint_2", result);
        }

        [Test]
        public void GetRandomUncompletedHint_AllCompleted_ReturnsNull()
        {
            var state = new LootboxState(0);
            var hints = new List<HintData>
            {
                new HintData { id = "hint_1" },
                new HintData { id = "hint_2" }
            };

            state.completedHintIds.Add("hint_1");
            state.completedHintIds.Add("hint_2");
            var rng = new System.Random();

            string result = state.GetRandomUncompletedHint(hints, rng);

            Assert.IsNull(result);
        }
    }

    [TestFixture]
    public class HintDataTests
    {
        [Test]
        public void IsAnswerCorrect_CaseSensitive_ExactMatch()
        {
            var hint = new HintData
            {
                answer = "Mario",
                isCaseSensitive = true
            };

            Assert.IsTrue(hint.IsAnswerCorrect("Mario"));
            Assert.IsFalse(hint.IsAnswerCorrect("mario"));
            Assert.IsFalse(hint.IsAnswerCorrect("MARIO"));
        }

        [Test]
        public void IsAnswerCorrect_CaseInsensitive()
        {
            var hint = new HintData
            {
                answer = "Mario",
                isCaseSensitive = false
            };

            Assert.IsTrue(hint.IsAnswerCorrect("Mario"));
            Assert.IsTrue(hint.IsAnswerCorrect("mario"));
            Assert.IsTrue(hint.IsAnswerCorrect("MARIO"));
        }

        [Test]
        public void IsAnswerCorrect_NullOrEmpty_ReturnsFalse()
        {
            var hint = new HintData { answer = "Mario" };

            Assert.IsFalse(hint.IsAnswerCorrect(null));
            Assert.IsFalse(hint.IsAnswerCorrect(""));
            Assert.IsFalse(hint.IsAnswerCorrect("   "));
        }

        [Test]
        public void IsAnswerCorrect_WithWhitespace()
        {
            var hint = new HintData
            {
                answer = "Mario",
                isCaseSensitive = false
            };

            Assert.IsFalse(hint.IsAnswerCorrect(" Mario"));
            Assert.IsFalse(hint.IsAnswerCorrect("Mario "));
            Assert.IsTrue(hint.IsAnswerCorrect("Mario"));
        }
    }

    [TestFixture]
    public class GameConfigSOTests
    {
        private static GameConfigSO CreateValidConfigSO()
        {
            var so = UnityEngine.ScriptableObject.CreateInstance<GameConfigSO>();
            var serialized = new UnityEditor.SerializedObject(so);
            serialized.FindProperty("_nLootbox").intValue = 1;

            var items = serialized.FindProperty("_items");
            items.arraySize = 2;
            items.GetArrayElementAtIndex(0).FindPropertyRelative("title").stringValue = "First Item";
            items.GetArrayElementAtIndex(1).FindPropertyRelative("title").stringValue = "Second Item";

            var hints = serialized.FindProperty("_hints");
            hints.arraySize = 1;
            hints.GetArrayElementAtIndex(0).FindPropertyRelative("answer").stringValue = "answer";

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        [Test]
        public void Validate_ValidConfig_ReturnsTrue()
        {
            var so = CreateValidConfigSO();

            Assert.IsTrue(so.Validate(out var error), error);
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void Validate_LootboxExceedsItems_ReturnsError()
        {
            var so = CreateValidConfigSO();
            new UnityEditor.SerializedObject(so).FindProperty("_nLootbox").intValue = 3;

            Assert.IsFalse(so.Validate(out var error));
            Assert.That(error, Does.Contain("cannot exceed the number of items"));
            UnityEngine.Object.DestroyImmediate(so);
        }

        [Test]
        public void ToGameConfig_MapsAllFields()
        {
            var so = CreateValidConfigSO();

            var config = ScriptableObjectConfigAdapter.ToGameConfig(so);

            Assert.AreEqual(1, config.settings.nLootbox);
            Assert.AreEqual(2, config.items.Length);
            Assert.AreEqual("item_0", config.items[0].id);
            Assert.AreEqual("item_1", config.items[1].id);
            Assert.AreEqual(1, config.hints.Length);
            Assert.AreEqual("hint_0", config.hints[0].id);
            UnityEngine.Object.DestroyImmediate(so);
        }
    }

    [TestFixture]
    public class WinnerDetectorTests
    {
        private GameConfig CreateTestConfig()
        {
            return new GameConfig
            {
                items = new ItemData[]
                {
                    new ItemData { id = "item_1", title = "Sword", isWinner = true },
                    new ItemData { id = "item_2", title = "Shield", isWinner = false },
                    new ItemData { id = "item_3", title = "Potion", isWinner = true },
                    new ItemData { id = "item_4", title = "Ring", isWinner = false }
                },
                hints = new HintData[0],
                settings = new GameSettings { nLootbox = 2 }
            };
        }

        [Test]
        public void GetWinners_ReturnsOnlyWinners()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            var winners = detector.GetWinners();

            Assert.AreEqual(2, winners.Count);
            Assert.IsTrue(winners.All(w => w.isWinner));
        }

        [Test]
        public void GetWinnerIds_ReturnsCorrectIds()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            var ids = detector.GetWinnerIds();

            Assert.AreEqual(2, ids.Count);
            Assert.Contains("item_1", ids);
            Assert.Contains("item_3", ids);
        }

        [Test]
        public void IsWinner_ReturnsCorrectly()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            Assert.IsTrue(detector.IsWinner("item_1"));
            Assert.IsFalse(detector.IsWinner("item_2"));
            Assert.IsTrue(detector.IsWinner("item_3"));
            Assert.IsFalse(detector.IsWinner("item_4"));
            Assert.IsFalse(detector.IsWinner("nonexistent"));
        }

        [Test]
        public void GetWinnerById_ReturnsCorrectItem()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            var winner = detector.GetWinnerById("item_1");

            Assert.IsNotNull(winner);
            Assert.AreEqual("item_1", winner.id);
            Assert.IsTrue(winner.isWinner);
        }

        [Test]
        public void GetWinnerById_NonWinner_ReturnsNull()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            var winner = detector.GetWinnerById("item_2");

            Assert.IsNull(winner);
        }

        [Test]
        public void GetNonWinners_ReturnsCorrectItems()
        {
            var detector = new WinnerDetector(CreateTestConfig());

            var nonWinners = detector.GetNonWinners();

            Assert.AreEqual(2, nonWinners.Count);
            Assert.IsTrue(nonWinners.All(w => !w.isWinner));
        }

        [Test]
        public void HasWinners_ReturnsTrueWhenWinnersExist()
        {
            var detector = new WinnerDetector(CreateTestConfig());
            Assert.IsTrue(detector.HasWinners());
        }

        [Test]
        public void WinnerCount_ReturnsCorrectCount()
        {
            var detector = new WinnerDetector(CreateTestConfig());
            Assert.AreEqual(2, detector.WinnerCount());
        }
    }

    [TestFixture]
    public class HintGameEngineTests
    {
        private GameConfig CreateTestConfig()
        {
            return new GameConfig
            {
                items = new ItemData[]
                {
                    new ItemData { id = "item_1", title = "Sword", isWinner = true },
                    new ItemData { id = "item_2", title = "Shield", isWinner = false }
                },
                hints = new HintData[]
                {
                    new HintData { id = "hint_1", answer = "Mario", isCaseSensitive = false },
                    new HintData { id = "hint_2", answer = "Luigi", isCaseSensitive = true }
                },
                settings = new GameSettings { nLootbox = 1 }
            };
        }

        private GameSession CreateTestSession()
        {
            var go = new UnityEngine.GameObject("TestGameSession");
            var session = go.AddComponent<GameSession>();
            session.Initialize(CreateTestConfig());
            session.SelectLootbox(0);
            return session;
        }

        [Test]
        public void CheckAnswer_CorrectAnswer_ReturnsTrue()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            engine.AssignNewHint();
            var hint = engine.GetCurrentHint();
            bool result = engine.CheckAnswer(hint.answer);

            Assert.IsTrue(result);
        }

        [Test]
        public void CheckAnswer_WrongAnswer_ReturnsFalse()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            engine.AssignNewHint();
            bool result = engine.CheckAnswer("WrongAnswer");

            Assert.IsFalse(result);
        }

        [Test]
        public void GetOrAssignHint_WhenHintIsAlreadyActive_ReusesTheSameHint()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            var assignedHint = engine.AssignNewHint();
            var reusedHint = engine.GetOrAssignHint();

            Assert.AreSame(assignedHint, reusedHint);
        }

        [Test]
        public void SubmitAnswer_CorrectAnswer_CompletesHint()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            engine.AssignNewHint();
            var hint = engine.GetCurrentHint();
            var result = engine.SubmitAnswer(hint.answer);

            Assert.IsTrue(result.IsCorrect);
            Assert.AreEqual(1, engine.GetCompletedHintCount());
        }

        [Test]
        public void SubmitAnswer_IncorrectAnswer_DoesNotCompleteHint()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            engine.AssignNewHint();
            var result = engine.SubmitAnswer("WrongAnswer");

            Assert.IsFalse(result.IsCorrect);
            Assert.AreEqual(0, engine.GetCompletedHintCount());
        }

        [Test]
        public void AreAllHintsCompleted_ReturnsCorrectly()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            Assert.IsFalse(engine.AreAllHintsCompleted());

            engine.AssignNewHint();
            var hint1 = engine.GetCurrentHint();
            engine.SubmitAnswer(hint1.answer);

            Assert.IsFalse(engine.AreAllHintsCompleted());

            engine.AssignNewHint();
            var hint2 = engine.GetCurrentHint();
            engine.SubmitAnswer(hint2.answer);

            Assert.IsTrue(engine.AreAllHintsCompleted());
        }

        [Test]
        public void AssignNewHint_AllCompleted_ReturnsNull()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            // Complete all hints
            engine.AssignNewHint();
            var hint1 = engine.GetCurrentHint();
            engine.SubmitAnswer(hint1.answer);

            engine.AssignNewHint();
            var hint2 = engine.GetCurrentHint();
            engine.SubmitAnswer(hint2.answer);

            // Try to assign another hint
            var result = engine.AssignNewHint();

            Assert.IsNull(result);
        }

        [Test]
        public void GetTotalHintCount_ReturnsCorrectCount()
        {
            var session = CreateTestSession();
            var engine = new HintGameEngine(session);

            Assert.AreEqual(2, engine.GetTotalHintCount());
        }
    }

    [TestFixture]
    public class LootboxEngineTests
    {
        private GameConfig CreateTestConfig()
        {
            return new GameConfig
            {
                items = new ItemData[]
                {
                    new ItemData { id = "winner_a", title = "Sword", isWinner = true },
                    new ItemData { id = "winner_b", title = "Crown", isWinner = true },
                    new ItemData { id = "common_a", title = "Shield", isWinner = false },
                    new ItemData { id = "common_b", title = "Potion", isWinner = false }
                },
                hints = new HintData[]
                {
                    new HintData { id = "hint_1", answer = "Mario", isCaseSensitive = false }
                },
                settings = new GameSettings { nLootbox = 2 }
            };
        }

        private GameSession CreateTestSession()
        {
            var go = new UnityEngine.GameObject("TestGameSession");
            var session = go.AddComponent<GameSession>();
            session.Initialize(CreateTestConfig());
            return session;
        }

        [Test]
        public void GetLootboxCount_ReturnsCorrectCount()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            Assert.AreEqual(2, engine.GetLootboxCount());
        }

        [Test]
        public void SelectLootbox_ReturnsCorrectState()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            var state = engine.SelectLootbox(0);

            Assert.IsNotNull(state);
            Assert.AreEqual(0, state.index);
            Assert.IsFalse(state.isOpened);
        }

        [Test]
        public void OpenLootbox_SetsOpenedAndWinners()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            engine.SelectLootbox(0);
            var winners = engine.OpenLootbox();

            Assert.IsNotNull(winners);
            Assert.AreEqual(1, winners.Count);
            CollectionAssert.Contains(new[] { "winner_a", "winner_b" }, winners[0]);
            Assert.IsTrue(engine.IsLootboxOpened(0));
        }

        [Test]
        public void GetCurrentWinners_AfterOpen_ReturnsWinners()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            engine.SelectLootbox(0);
            engine.OpenLootbox();

            var currentWinners = engine.GetCurrentWinners();

            Assert.IsNotNull(currentWinners);
            Assert.AreEqual(1, currentWinners.Count);
            Assert.IsTrue(currentWinners[0].isWinner);
        }

        [Test]
        public void OpeningAllLootboxes_AssignsEveryItemExactlyOnce()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);
            var assignedItemIds = new List<string>();

            for (var index = 0; index < engine.GetLootboxCount(); index++)
            {
                engine.SelectLootbox(index);
                assignedItemIds.AddRange(engine.OpenLootbox());
            }

            CollectionAssert.AreEquivalent(new[] { "winner_a", "winner_b" }, assignedItemIds);
            Assert.AreEqual(assignedItemIds.Count, assignedItemIds.Distinct().Count());
        }

        [Test]
        public void ItemDistribution_WhenTwentyItemsUseTenLootboxes_AssignsTwoItemsPerLootbox()
        {
            var config = new GameConfig
            {
                items = Enumerable.Range(1, 20)
                    .Select(index => new ItemData { id = $"item_{index}" })
                    .ToArray(),
                settings = new GameSettings { nLootbox = 10 }
            };

            var assignments = LootboxItemDistributor.Distribute(config, new Random(42));

            Assert.AreEqual(10, assignments.Count);
            Assert.IsTrue(assignments.All(assignment => assignment.Count == 2));
            CollectionAssert.AreEquivalent(config.items.Select(item => item.id), assignments.SelectMany(assignment => assignment));
        }

        [Test]
        public void ItemDistribution_WhenWinnersAndNonWinnersHaveDifferentCounts_BalancesEachGroupIndependently()
        {
            var config = new GameConfig
            {
                items = new[]
                {
                    new ItemData { id = "winner_1", isWinner = true },
                    new ItemData { id = "winner_2", isWinner = true },
                    new ItemData { id = "winner_3", isWinner = true },
                    new ItemData { id = "winner_4", isWinner = true },
                    new ItemData { id = "winner_5", isWinner = true },
                    new ItemData { id = "common_1", isWinner = false },
                    new ItemData { id = "common_2", isWinner = false },
                    new ItemData { id = "common_3", isWinner = false },
                    new ItemData { id = "common_4", isWinner = false }
                },
                settings = new GameSettings { nLootbox = 3 }
            };

            var assignments = LootboxItemDistributor.Distribute(config, new Random(42));
            var itemsById = config.items.ToDictionary(item => item.id);
            var winnerCounts = assignments.Select(box => box.Count(itemId => itemsById[itemId].isWinner));
            var nonWinnerCounts = assignments.Select(box => box.Count(itemId => !itemsById[itemId].isWinner));

            CollectionAssert.AreEquivalent(new[] { 1, 2, 2 }, winnerCounts);
            CollectionAssert.AreEquivalent(new[] { 1, 1, 2 }, nonWinnerCounts);
        }

        [Test]
        public void IsGameComplete_WhenAllOpened_ReturnsTrue()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            Assert.IsFalse(engine.IsGameComplete());

            engine.SelectLootbox(0);
            engine.OpenLootbox();

            engine.SelectLootbox(1);
            engine.OpenLootbox();

            Assert.IsTrue(engine.IsGameComplete());
        }

        [Test]
        public void GetUnopenedCount_ReturnsCorrectCount()
        {
            var session = CreateTestSession();
            var engine = new LootboxEngine(session);

            Assert.AreEqual(2, engine.GetUnopenedCount());

            engine.SelectLootbox(0);
            engine.OpenLootbox();

            Assert.AreEqual(1, engine.GetUnopenedCount());
        }
    }

    [TestFixture]
    public class LootboxOpeningStrategyTests
    {
        [Test]
        public void DeclaredWinnersStrategy_RevealsOnlyWinnersAssignedToTheLootbox()
        {
            var config = new GameConfig
            {
                items = new[]
                {
                    new ItemData { id = "common", isWinner = false },
                    new ItemData { id = "winner_a", isWinner = true },
                    new ItemData { id = "winner_b", isWinner = true }
                }
            };

            var lootbox = new LootboxState(0);
            lootbox.AssignItemIds(new List<string> { "common", "winner_a" });
            var rewards = new DeclaredWinnersOpeningStrategy()
                .SelectRewardItemIds(config, lootbox, new Random(1));

            CollectionAssert.AreEquivalent(new[] { "winner_a" }, rewards);
        }

        [Test]
        public void RandomWinnerStrategy_RevealsOneDeclaredWinner()
        {
            var config = new GameConfig
            {
                items = new[]
                {
                    new ItemData { id = "winner_a", isWinner = true },
                    new ItemData { id = "winner_b", isWinner = true }
                }
            };

            var lootbox = new LootboxState(0);
            lootbox.AssignItemIds(new List<string> { "winner_b" });
            var rewards = new RandomWinnerOpeningStrategy()
                .SelectRewardItemIds(config, lootbox, new Random(1));

            Assert.AreEqual(1, rewards.Count);
            Assert.AreEqual("winner_b", rewards[0]);
        }

        [Test]
        public void StrategyRegistry_AllowsCustomStrategiesToBeRegistered()
        {
            var registry = new LootboxStrategyRegistry();
            registry.Register(new TestStrategy());

            Assert.AreEqual("test", registry.GetRequired("test").Id);
        }

        private sealed class TestStrategy : ILootboxOpeningStrategy
        {
            public string Id => "test";

            public IReadOnlyList<string> SelectRewardItemIds(GameConfig config, LootboxState lootbox, Random random)
            {
                return new[] { "custom" };
            }
        }
    }
}
