using System;
using System.Collections;
using System.Collections.Generic;
using RevealGame.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    internal sealed class LootboxRevealSequence
    {
        private const int ReelLength = 24;
        private const int LandingCardIndex = 18;
        private const float CardSpacing = 0.35f;
        private const float ReelStartOffset = 0.33f;
        private const float SpinDuration = 4.2f;
        private const float LandingHoldDuration = 1.1f;

        private readonly LootboxEngine _lootboxEngine;
        private readonly MediaDisplayController _mediaDisplay;
        private readonly Text _resultText;
        private readonly IReadOnlyList<Data.ItemData> _allItems;
        private readonly List<RawImage> _reelCards = new List<RawImage>();
        private readonly RectTransform _viewport;
        private readonly Image _selectionMarker;
        private readonly List<string> _collectedRewardTitles = new List<string>();
        private readonly Data.GameSettings _settings;

        public LootboxRevealSequence(GameManager gameManager, Text resultText, Transform popupTransform)
        {
            if (gameManager == null)
                throw new ArgumentNullException(nameof(gameManager));

            _lootboxEngine = gameManager.LootboxEngine;
            _mediaDisplay = gameManager.MediaDisplay;
            _settings = gameManager.Session?.Config?.settings;
            _resultText = resultText;
            _allItems = _lootboxEngine.GetAllItems();
            _viewport = CreateViewport(popupTransform ?? throw new ArgumentNullException(nameof(popupTransform)));
            _selectionMarker = CreateSelectionMarker(_viewport);
            _selectionMarker.transform.SetAsLastSibling();
        }

        public IReadOnlyList<string> CollectedRewardTitles => _collectedRewardTitles;

        public IEnumerator Play(IReadOnlyList<string> rewardItemIds)
        {
            if (rewardItemIds == null)
                throw new ArgumentNullException(nameof(rewardItemIds));

            for (var rewardIndex = 0; rewardIndex < rewardItemIds.Count; rewardIndex++)
            {
                yield return RevealReward(rewardItemIds[rewardIndex], rewardIndex, rewardItemIds.Count);
            }
        }

        private IEnumerator RevealReward(string rewardItemId, int rewardIndex, int rewardCount)
        {
            var reward = _lootboxEngine.GetItemById(rewardItemId);
            var rewardTitle = GetDisplayTitle(rewardItemId, reward?.title);

            BuildReel(rewardIndex, reward);
            SetText(_settings != null
                ? _settings.FormatRewardSpinning(rewardIndex + 1, rewardCount)
                : Data.GameSettings.DefaultRewardSpinningFormat);
            yield return ScrollReel();

            yield return new WaitForSecondsRealtime(LandingHoldDuration);
            _collectedRewardTitles.Add(rewardTitle);
            SetText(BuildRewardText(rewardIndex, rewardCount, rewardTitle));
            yield return new WaitForSecondsRealtime(rewardIndex == rewardCount - 1 ? 1.2f : 0.65f);
        }

        public void Dispose()
        {
            ClearReelCards();
            UnityEngine.Object.Destroy(_viewport.gameObject);
        }

        private static RectTransform CreateViewport(Transform popupTransform)
        {
            var viewportObject = new GameObject("RevealCarouselViewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(popupTransform, false);
            var viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = new Vector2(0.02f, 0.24f);
            viewport.anchorMax = new Vector2(0.98f, 0.62f);
            viewport.offsetMin = Vector2.zero;
            viewport.offsetMax = Vector2.zero;
            viewportObject.GetComponent<Image>().color = Color.clear;
            return viewport;
        }

        private static Image CreateSelectionMarker(Transform viewport)
        {
            var markerObject = new GameObject("RevealSelectionMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            markerObject.transform.SetParent(viewport, false);
            var markerRect = markerObject.GetComponent<RectTransform>();
            markerRect.anchorMin = new Vector2(0.49f, 0f);
            markerRect.anchorMax = new Vector2(0.51f, 1f);
            markerRect.offsetMin = Vector2.zero;
            markerRect.offsetMax = Vector2.zero;
            var marker = markerObject.GetComponent<Image>();
            marker.color = new Color(1f, 1f, 1f, 0.85f);
            marker.raycastTarget = false;
            return marker;
        }

        private void BuildReel(int rewardIndex, Data.ItemData reward)
        {
            ClearReelCards();

            for (var cardIndex = 0; cardIndex < ReelLength; cardIndex++)
            {
                var cardObject = new GameObject($"RevealCard_{cardIndex}", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                cardObject.transform.SetParent(_viewport, false);

                var card = cardObject.GetComponent<RawImage>();
                card.color = Color.white;
                card.raycastTarget = false;
                SetCardTexture(card, GetReelItem(rewardIndex, cardIndex, reward));
                SetCardSlot(card.rectTransform, cardIndex * CardSpacing - ReelStartOffset);
                _reelCards.Add(card);
            }

            _selectionMarker.transform.SetAsLastSibling();
            _selectionMarker.gameObject.SetActive(true);
        }

        private Data.ItemData GetReelItem(int rewardIndex, int cardIndex, Data.ItemData reward)
        {
            if (cardIndex == LandingCardIndex)
                return reward;

            return _allItems.Count == 0
                ? null
                : _allItems[(rewardIndex + cardIndex) % _allItems.Count];
        }

        private IEnumerator ScrollReel()
        {
            var elapsed = 0f;
            var landingOffset = LandingCardIndex * CardSpacing - 0.68f;
            while (elapsed < SpinDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                var elapsedRatio = Mathf.Clamp01(elapsed / SpinDuration);
                var decelerationRatio = 1f - Mathf.Pow(1f - elapsedRatio, 3f);
                SetReelPositions(landingOffset * decelerationRatio);
                yield return null;
            }
        }

        private string BuildRewardText(int rewardIndex, int rewardCount, string rewardTitle)
        {
            return _settings != null
                ? _settings.FormatRewardRevealed(rewardIndex + 1, rewardCount, rewardTitle)
                : Data.GameSettings.DefaultRewardRevealedFormat;
        }

        private static string GetDisplayTitle(string fallback, string title)
        {
            return string.IsNullOrWhiteSpace(title) ? fallback : title;
        }

        private void SetText(string text)
        {
            if (_resultText != null)
                _resultText.text = text;
        }

        private void SetCardTexture(RawImage card, Data.ItemData item)
        {
            var sprite = _mediaDisplay == null ? null : _mediaDisplay.ResolveItemSprite(item);
            card.texture = sprite == null ? null : sprite.texture;
        }

        private void SetReelPositions(float scrollOffset)
        {
            for (var cardIndex = 0; cardIndex < _reelCards.Count; cardIndex++)
                SetCardSlot(_reelCards[cardIndex].rectTransform, cardIndex * CardSpacing - ReelStartOffset - scrollOffset);
        }

        private void ClearReelCards()
        {
            foreach (var card in _reelCards)
                UnityEngine.Object.Destroy(card.gameObject);

            _reelCards.Clear();
        }

        private static void SetCardSlot(RectTransform cardRect, float minX)
        {
            cardRect.anchorMin = new Vector2(minX, 0f);
            cardRect.anchorMax = new Vector2(minX + 0.3f, 1f);
            cardRect.offsetMin = Vector2.zero;
            cardRect.offsetMax = Vector2.zero;
        }
    }
}
