using RevealGame.Data;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Positions an item's aspect-fitted image horizontally inside its display area.
    /// </summary>
    public static class ItemImageAlignmentUtility
    {
        public static void Apply(Image image, ItemData item)
        {
            if (image == null || item == null)
                return;

            var imageRect = GetAlignmentRect(image);
            if (imageRect == null)
                return;

            var horizontalAnchor = GetHorizontalAnchor(item.imageAlignment);
            var coverFitter = imageRect.GetComponent<ImageAspectRatioFitter>();

            // Cover layout drives anchors, position, and size but intentionally leaves pivot free,
            // making pivot the stable equivalent of CSS object-position for cropped overflow.
            if (coverFitter != null)
            {
                imageRect.pivot = new Vector2(horizontalAnchor, imageRect.pivot.y);
                imageRect.anchoredPosition = new Vector2(0f, imageRect.anchoredPosition.y);
                return;
            }

            var currentWidth = imageRect.rect.width;
            var sizeDelta = imageRect.sizeDelta;

            imageRect.anchorMin = new Vector2(horizontalAnchor, imageRect.anchorMin.y);
            imageRect.anchorMax = new Vector2(horizontalAnchor, imageRect.anchorMax.y);
            imageRect.pivot = new Vector2(horizontalAnchor, imageRect.pivot.y);
            imageRect.anchoredPosition = new Vector2(0f, imageRect.anchoredPosition.y);

            if (currentWidth > 0f)
                imageRect.sizeDelta = new Vector2(currentWidth, sizeDelta.y);
        }

        private static RectTransform GetAlignmentRect(Image image)
        {
            for (var current = image.transform; current != null; current = current.parent)
            {
                if (current.GetComponent<ImageAspectRatioFitter>() != null)
                    return current as RectTransform;
            }

            return image.rectTransform;
        }

        private static float GetHorizontalAnchor(ItemImageAlignment alignment)
        {
            switch (alignment)
            {
                case ItemImageAlignment.Left:
                    return 0f;
                case ItemImageAlignment.Right:
                    return 1f;
                default:
                    return 0.5f;
            }
        }
    }
}
