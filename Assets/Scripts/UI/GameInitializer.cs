using UnityEngine;
using RevealGame.Data;
using RevealGame.Importing;

namespace RevealGame.UI
{
    public class GameInitializer : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _configSO;
        [SerializeField] private GameConfig _testConfig;

        [Header("Dependencies")]
        [SerializeField] private GameManager _gameManager;

        private void Start()
        {
            if (_gameManager == null)
            {
                Debug.LogError("[GameInitializer] GameManager not assigned");
                return;
            }

            var config = ResolveConfig();
            _gameManager.InitializeGame(config);
        }

        private GameConfig ResolveConfig()
        {
            if (_configSO != null)
                return ScriptableObjectConfigAdapter.ToGameConfig(_configSO);

            if (_testConfig != null && _testConfig.items != null && _testConfig.items.Length > 0)
                return _testConfig;

            return CreateDefaultConfig();
        }

        private static GameConfig CreateDefaultConfig()
        {
            var config = new GameConfig();
            config.settings = new GameSettings();
            config.settings.nLootbox = 6;
            config.items = new ItemData[7];
            config.items[0] = new ItemData { id = "common_coin", title = "Gold Coin", description = "A shiny gold coin", mediaPath = "", isWinner = false };
            config.items[1] = new ItemData { id = "common_gem", title = "Crystal Gem", description = "A sparkling gem", mediaPath = "", isWinner = false };
            config.items[2] = new ItemData { id = "common_key", title = "Mystery Key", description = "A rusty old key", mediaPath = "", isWinner = false };
            config.items[3] = new ItemData { id = "rare_ring", title = "Silver Ring", description = "A delicate silver ring", mediaPath = "", isWinner = false };
            config.items[4] = new ItemData { id = "rare_potion", title = "Health Potion", description = "Restores 50 HP", mediaPath = "", isWinner = false };
            config.items[5] = new ItemData { id = "epic_crown", title = "Golden Crown", description = "A magnificent golden crown", mediaPath = "", isWinner = true };
            config.items[6] = new ItemData { id = "legendary_sword", title = "Excalibur", description = "The legendary sword", mediaPath = "", isWinner = true };
            config.hints = new HintData[6];
            config.hints[0] = new HintData { id = "hint_1", title = "I breathe fire and guard treasure.", hintType = HintType.Text, answer = "dragon", isCaseSensitive = false, mediaPaths = new[] { "" } };
            config.hints[1] = new HintData { id = "hint_2", title = "I rise from ashes with fiery wings.", hintType = HintType.Text, answer = "phoenix", isCaseSensitive = false, mediaPaths = new[] { "" } };
            config.hints[2] = new HintData { id = "hint_3", title = "I have an eagle's head and a lion's body.", hintType = HintType.Text, answer = "griffin", isCaseSensitive = false, mediaPaths = new[] { "" } };
            config.hints[3] = new HintData { id = "hint_4", title = "I am a horse with a single magical horn.", hintType = HintType.Text, answer = "unicorn", isCaseSensitive = false, mediaPaths = new[] { "" } };
            config.hints[4] = new HintData { id = "hint_5", title = "I am a mythical creature who lives beneath the sea.", hintType = HintType.Text, answer = "mermaid", isCaseSensitive = false, mediaPaths = new[] { "" } };
            config.hints[5] = new HintData { id = "hint_6", title = "I am a three-headed guardian hound.", hintType = HintType.Text, answer = "cerberus", isCaseSensitive = false, mediaPaths = new[] { "" } };
            return config;
        }
    }
}
