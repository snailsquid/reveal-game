using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Holds the shared UI, music, and roulette sounds loaded by <see cref="UiSoundPlayer"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSoundSettings", menuName = "Reveal Game/UI Sound Settings")]
    public sealed class UiSoundSettings : ScriptableObject
    {
        [Header("Buttons")]
        [SerializeField] private AudioClip _buttonHover;
        [SerializeField] private AudioClip _buttonClick;

        [Header("Roulette")]
        [SerializeField] private AudioClip _rouletteTick;

        [Header("Game Flow Audio")]
        [SerializeField] private AudioClip _defaultMusic;
        [SerializeField] private AudioClip _guessingMusic;
        [SerializeField] private AudioClip _guessCorrect;
        [SerializeField] private AudioClip _intermediateMusic;
        [SerializeField] private AudioClip _spin;

        public AudioClip ButtonHover => _buttonHover;
        public AudioClip ButtonClick => _buttonClick;
        public AudioClip RouletteTick => _rouletteTick;
        public AudioClip DefaultMusic => _defaultMusic;
        public AudioClip GuessingMusic => _guessingMusic;
        public AudioClip GuessCorrect => _guessCorrect;
        public AudioClip IntermediateMusic => _intermediateMusic;
        public AudioClip Spin => _spin;
    }
}
