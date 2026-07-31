using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RevealGame.Data;
using RevealGame.Importing;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Drives the isolated roulette display test scene with rewards from a GameConfigSO.
    /// </summary>
    public class RouletteDisplayTestController : MonoBehaviour
    {
        [SerializeField] private GameConfigSO _configSO;
        [SerializeField] private RouletteDisplay _rouletteDisplay;
        [SerializeField] private bool _playOnStart = true;

        private GameConfig _config;
        private Coroutine _playRoutine;

        private void Awake()
        {
            if (_configSO == null || _rouletteDisplay == null)
            {
                Debug.LogError("[RouletteDisplayTestController] Assign a config and roulette display.");
                return;
            }

            _config = ScriptableObjectConfigAdapter.ToGameConfig(_configSO);
            _rouletteDisplay.SetupTestItems(_config.items);
        }

        private void Start()
        {
            if (_playOnStart)
                QueuePlay();
        }

        private void QueuePlay()
        {
            if (_playRoutine == null && _config != null)
                _playRoutine = StartCoroutine(PlayConfiguredRewards());
        }

        private IEnumerator PlayConfiguredRewards()
        {
            var rewardIds = _config.items
                .Where(item => item != null && item.isWinner)
                .Select(item => item.id)
                .ToList();

            if (rewardIds.Count == 0 && _config.items.Length > 0)
                rewardIds.Add(_config.items[0].id);

            yield return StartCoroutine(_rouletteDisplay.Play(rewardIds));
            _playRoutine = null;
        }
    }
}
