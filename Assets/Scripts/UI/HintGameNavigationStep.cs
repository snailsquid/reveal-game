using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Describes one ordered UI step in the lootbox-opening flow.
    /// The navigation controller owns the order; this component only describes
    /// how the step is shown and whether it permits moving backwards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HintGameNavigationStep : MonoBehaviour
    {
        [SerializeField] private int _order;
        [SerializeField] private bool _allowPrevious;
        [SerializeField] private bool _useCanvasGroup;
        private CanvasGroup _canvasGroup;

        public int Order => _order;
        public bool CanGoPrevious => _allowPrevious;
        internal void Configure(int order, bool allowPrevious, bool useCanvasGroup)
        {
            _order = order;
            _allowPrevious = allowPrevious;
            _useCanvasGroup = useCanvasGroup;
            if (_useCanvasGroup && _canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void SetActive(bool isActive)
        {
            if (_useCanvasGroup)
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                    if (_canvasGroup == null)
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                _canvasGroup.alpha = isActive ? 1f : 0f;
                _canvasGroup.interactable = isActive;
                _canvasGroup.blocksRaycasts = isActive;
                return;
            }

            if (gameObject.activeSelf != isActive)
                gameObject.SetActive(isActive);
        }
    }
}
