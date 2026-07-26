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
        [SerializeField] private GameObject _activationTarget;

        private CanvasGroup _canvasGroup;

        public int Order => _order;
        public bool CanGoPrevious => _allowPrevious;
        public GameObject ActivationTarget => _activationTarget != null ? _activationTarget : gameObject;

        internal void Configure(int order, bool allowPrevious, bool useCanvasGroup,
            GameObject activationTarget = null)
        {
            _order = order;
            _allowPrevious = allowPrevious;
            _useCanvasGroup = useCanvasGroup;
            if (activationTarget != null)
                _activationTarget = activationTarget;

            if (_useCanvasGroup && _canvasGroup == null)
            {
                _canvasGroup = ActivationTarget.GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                    _canvasGroup = ActivationTarget.AddComponent<CanvasGroup>();
            }
        }

        public void SetActive(bool isActive)
        {
            if (_useCanvasGroup)
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = ActivationTarget.GetComponent<CanvasGroup>();
                    if (_canvasGroup == null)
                        _canvasGroup = ActivationTarget.AddComponent<CanvasGroup>();
                }

                _canvasGroup.alpha = isActive ? 1f : 0f;
                _canvasGroup.interactable = isActive;
                _canvasGroup.blocksRaycasts = isActive;
                return;
            }

            if (ActivationTarget.activeSelf != isActive)
                ActivationTarget.SetActive(isActive);
        }
    }
}
