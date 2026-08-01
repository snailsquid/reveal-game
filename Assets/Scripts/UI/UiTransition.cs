using System;
using DG.Tweening;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Provides a consistent DOTween enter/exit animation for UI roots.
    /// The target is non-interactable while it is animating or hidden.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiTransition : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float _enterDuration = 0.25f;
        [SerializeField, Min(0f)] private float _exitDuration = 0.2f;
        [SerializeField] private Ease _enterEase = Ease.OutBack;
        [SerializeField] private Ease _exitEase = Ease.InQuad;
        [SerializeField, Range(0.01f, 1f)] private float _hiddenScaleMultiplier = 0.92f;
        [SerializeField] private bool _ignoreTimeScale = true;

        private CanvasGroup _canvasGroup;
        private Vector3 _visibleScale;
        private Sequence _sequence;
        private bool _hasVisibleScale;

        public bool IsAnimating => _sequence != null && _sequence.IsActive() && _sequence.IsPlaying();

        public static UiTransition GetOrAdd(GameObject target)
        {
            if (target == null)
                return null;

            var transition = target.GetComponent<UiTransition>();
            return transition != null ? transition : target.AddComponent<UiTransition>();
        }

        private void Awake()
        {
            CacheReferences();
        }

        private void OnDisable()
        {
            KillCurrentAnimation();
        }

        private void OnDestroy()
        {
            KillCurrentAnimation();
        }

        public void Show(Action onComplete = null)
        {
            CacheReferences();
            gameObject.SetActive(true);
            KillCurrentAnimation();

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            transform.localScale = GetHiddenScale();

            _sequence = DOTween.Sequence()
                .SetUpdate(_ignoreTimeScale)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(DOTween.To(
                    () => _canvasGroup.alpha,
                    alpha => _canvasGroup.alpha = alpha,
                    1f,
                    _enterDuration).SetEase(_enterEase))
                .Join(transform.DOScale(_visibleScale, _enterDuration).SetEase(_enterEase))
                .OnComplete(() =>
                {
                    _canvasGroup.interactable = true;
                    _canvasGroup.blocksRaycasts = true;
                    _sequence = null;
                    onComplete?.Invoke();
                });
        }

        public void Hide(Action onComplete = null)
        {
            CacheReferences();
            if (!gameObject.activeSelf)
            {
                onComplete?.Invoke();
                return;
            }

            KillCurrentAnimation();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            _sequence = DOTween.Sequence()
                .SetUpdate(_ignoreTimeScale)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
                .Append(DOTween.To(
                    () => _canvasGroup.alpha,
                    alpha => _canvasGroup.alpha = alpha,
                    0f,
                    _exitDuration).SetEase(_exitEase))
                .Join(transform.DOScale(GetHiddenScale(), _exitDuration).SetEase(_exitEase))
                .OnComplete(() =>
                {
                    _sequence = null;
                    gameObject.SetActive(false);
                    onComplete?.Invoke();
                });
        }

        public void SetVisibleImmediately(bool visible)
        {
            CacheReferences();
            KillCurrentAnimation();
            gameObject.SetActive(visible);
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;
            _canvasGroup.blocksRaycasts = visible;
            transform.localScale = visible ? _visibleScale : GetHiddenScale();
        }

        private void CacheReferences()
        {
            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (!_hasVisibleScale)
            {
                _visibleScale = transform.localScale;
                _hasVisibleScale = true;
            }
        }

        private Vector3 GetHiddenScale()
        {
            return _visibleScale * _hiddenScaleMultiplier;
        }

        private void KillCurrentAnimation()
        {
            if (_sequence == null)
                return;

            _sequence.Kill();
            _sequence = null;
        }
    }
}
