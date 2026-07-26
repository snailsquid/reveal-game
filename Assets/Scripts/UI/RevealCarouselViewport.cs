using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    [DisallowMultipleComponent]
    public sealed class RevealCarouselViewport : MonoBehaviour
    {
        [SerializeField] private Image _selectionMarker;
        [SerializeField] private GameObject _cardPrefab;

        public RectTransform ViewportRect { get; private set; }

        public Image SelectionMarker
        {
            get => _selectionMarker;
            set => _selectionMarker = value;
        }

        public GameObject CardPrefab => _cardPrefab;

        private void Awake()
        {
            ViewportRect = (RectTransform)transform;
        }
    }
}
