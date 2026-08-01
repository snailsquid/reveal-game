using DG.Tweening;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Plays a staggered entrance animation for the active direct children of a UI container.
    /// </summary>
    internal static class UiStaggeredListAnimator
    {
        private const float DefaultInitialDelay = 0.05f;
        private const float HiddenScaleMultiplier = 0.9f;

        public static Sequence Play(Transform container, float animationDuration, float staggerDelay)
        {
            if (container == null)
                return null;

            animationDuration = Mathf.Max(0.01f, animationDuration);
            staggerDelay = Mathf.Max(0f, staggerDelay);

            var sequence = DOTween.Sequence()
                .SetUpdate(true)
                .SetLink(container.gameObject, LinkBehaviour.KillOnDestroy);

            int animatedElementIndex = 0;
            foreach (Transform child in container)
            {
                if (!child.gameObject.activeSelf)
                    continue;

                var element = child;
                var canvasGroup = element.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                    canvasGroup = element.gameObject.AddComponent<CanvasGroup>();

                Vector3 visibleScale = element.localScale;
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                element.localScale = visibleScale * HiddenScaleMultiplier;

                Sequence elementSequence = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => canvasGroup.alpha,
                        alpha => canvasGroup.alpha = alpha,
                        1f,
                        animationDuration).SetEase(Ease.OutQuad))
                    .Join(element.DOScale(visibleScale, animationDuration).SetEase(Ease.OutBack))
                    .OnComplete(() =>
                    {
                        canvasGroup.interactable = true;
                        canvasGroup.blocksRaycasts = true;
                    });

                sequence.Insert(
                    DefaultInitialDelay + animatedElementIndex * staggerDelay,
                    elementSequence);
                animatedElementIndex++;
            }

            return sequence;
        }
    }
}
