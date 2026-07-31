using TMPro;
using UnityEngine;
using UnityEngine.UI;
using RevealGame.Gameplay;

namespace RevealGame.UI
{
    /// <summary>
    /// A prefab-backed row for one imported item in an <see cref="ItemPreviewView"/>.
    /// </summary>
    public sealed class ItemPreviewRow : MonoBehaviour
    {
        [SerializeField] private Image _itemImage;
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private TMP_Text _descriptionText;

        public void Bind(Data.ItemData item, MediaDisplayController mediaDisplay)
        {
            var sprite = mediaDisplay?.ResolveItemSprite(item);
            _itemImage.sprite = sprite;
            _itemImage.enabled = sprite != null;
            _titleText.text = string.IsNullOrEmpty(item.title) ? item.id : item.title;
            _descriptionText.text = item.description ?? string.Empty;
        }
    }
}
