using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Describes one UI step in the lootbox-opening flow.
    /// The navigation controller uses its configured step array order; this
    /// component only describes how the step is shown and whether it permits
    /// moving backwards.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HintGameNavigationStep : MonoBehaviour
    {
        [SerializeField] private bool _allowPrevious;
        [SerializeField] private bool _useCanvasGroup;
        private CanvasGroup _canvasGroup;

        public bool CanGoPrevious => _allowPrevious;
        internal void Configure(bool allowPrevious, bool useCanvasGroup)
        {
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
