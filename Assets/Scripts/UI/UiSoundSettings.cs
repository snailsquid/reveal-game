using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Holds the shared UI and roulette sound effects loaded by <see cref="UiSoundPlayer"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "UiSoundSettings", menuName = "Reveal Game/UI Sound Settings")]
    public sealed class UiSoundSettings : ScriptableObject
    {
        [Header("Buttons")]
        [SerializeField] private AudioClip _buttonHover;
        [SerializeField] private AudioClip _buttonClick;

        [Header("Roulette")]
        [SerializeField] private AudioClip _rouletteTick;

        public AudioClip ButtonHover => _buttonHover;
        public AudioClip ButtonClick => _buttonClick;
        public AudioClip RouletteTick => _rouletteTick;
    }
}
