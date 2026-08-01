using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Represents one pending or revealed reward in the roulette's winner row.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RouletteWinnerSlot : MonoBehaviour
    {
        [SerializeField] private GameObject _placeholderImage;
        [SerializeField] private Image _rewardImage;

        /// <summary>
        /// Resets this slot to its unrevealed placeholder state.
        /// </summary>
        public void ShowPlaceholder()
        {
            if (_placeholderImage != null)
                _placeholderImage.SetActive(true);

            if (_rewardImage != null)
            {
                _rewardImage.sprite = null;
                _rewardImage.enabled = false;
            }
        }

        /// <summary>
        /// Replaces the placeholder with the selected reward image.
        /// </summary>
        public void Reveal(Sprite rewardSprite)
        {
            if (_placeholderImage != null)
                _placeholderImage.SetActive(false);

            if (_rewardImage == null)
                return;

            _rewardImage.sprite = rewardSprite;
            _rewardImage.enabled = rewardSprite != null;
        }
    }
}
