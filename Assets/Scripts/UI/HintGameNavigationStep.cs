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
        private HintGameUIController _navigation;
        private UiTransition _transition;

        public bool CanGoPrevious => _allowPrevious;

        /// <summary>
        /// Requests the next configured step. A step owns the interaction that
        /// triggers this request, while the controller owns the ordered flow.
        /// </summary>
        public void RequestNext() => ResolveNavigation()?.Next();

        /// <summary>
        /// Requests the previous configured step.
        /// </summary>
        public void RequestPrevious() => ResolveNavigation()?.Previous();

        internal void BindNavigation(HintGameUIController navigation)
        {
            _navigation = navigation;
        }

        private HintGameUIController ResolveNavigation()
        {
            if (_navigation == null)
                _navigation = GetComponentInParent<HintGameUIController>(true);

            return _navigation;
        }

        internal void Configure(bool allowPrevious, bool useCanvasGroup)
        {
            _allowPrevious = allowPrevious;
            _useCanvasGroup = useCanvasGroup;
        }

        public void SetActive(bool isActive, System.Action onComplete = null)
        {
            _transition ??= UiTransition.GetOrAdd(gameObject);
            if (isActive)
            {
                _transition.Show(onComplete);
                return;
            }

            _transition.Hide(onComplete);
        }

        internal void SetInactiveImmediately()
        {
            _transition ??= UiTransition.GetOrAdd(gameObject);
            _transition.SetVisibleImmediately(false);
        }
    }
}
