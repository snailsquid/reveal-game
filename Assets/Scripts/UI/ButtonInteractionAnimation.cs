using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Gives a uGUI button responsive hover and press-scale feedback.
    /// The component is added automatically by <see cref="ButtonInteractionAnimationInstaller"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonInteractionAnimation : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [Header("Scale")]
        [SerializeField, Range(1f, 1.2f)] private float _hoverScaleMultiplier = 1.06f;
        [SerializeField, Range(0.8f, 1f)] private float _pressedScaleMultiplier = 0.94f;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _hoverDuration = 0.12f;
        [SerializeField, Min(0f)] private float _pressDuration = 0.06f;
        [SerializeField] private Ease _hoverEase = Ease.OutBack;
        [SerializeField] private Ease _pressEase = Ease.OutQuad;

        private Button _button;
        private Vector3 _restScale;
        private Tween _scaleTween;
        private bool _isPointerOver;

        /// <summary>
        /// Adds the interaction animation to a button when it does not already have one.
        /// </summary>
        public static ButtonInteractionAnimation EnsureAttached(Button button)
        {
            if (button == null)
                return null;

            var animation = button.GetComponent<ButtonInteractionAnimation>();
            return animation != null ? animation : button.gameObject.AddComponent<ButtonInteractionAnimation>();
        }

        private void Awake()
        {
            _button = GetComponent<Button>();
            _restScale = transform.localScale;
        }

        private void OnDisable()
        {
            _isPointerOver = false;
            KillScaleTween();
        }

        private void OnDestroy()
        {
            KillScaleTween();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isPointerOver = true;

            if (CanAnimate())
                AnimateTo(_restScale * _hoverScaleMultiplier, _hoverDuration, _hoverEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isPointerOver = false;

            if (CanAnimate())
                AnimateTo(_restScale, _hoverDuration, _hoverEase);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (CanAnimate())
                AnimateTo(_restScale * _pressedScaleMultiplier, _pressDuration, _pressEase);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanAnimate())
                return;

            Vector3 targetScale = _isPointerOver
                ? _restScale * _hoverScaleMultiplier
                : _restScale;
            AnimateTo(targetScale, _hoverDuration, _hoverEase);
        }

        private bool CanAnimate()
        {
            return _button != null && _button.interactable && gameObject.activeInHierarchy;
        }

        private void AnimateTo(Vector3 targetScale, float duration, Ease ease)
        {
            KillScaleTween();
            _scaleTween = transform
                .DOScale(targetScale, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        }

        private void KillScaleTween()
        {
            if (_scaleTween == null)
                return;

            _scaleTween.Kill();
            _scaleTween = null;
        }
    }
}
