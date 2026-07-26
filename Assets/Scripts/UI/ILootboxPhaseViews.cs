using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RevealGame.UI
{
    public interface IItemPreviewView
    {
        void Setup(GameManager gameManager);
        void Show(IReadOnlyList<Data.ItemData> items);
        void Clear();
        void SetVisible(bool visible);
        void SetPreviousInteractable(bool interactable);
        event Action NextRequested;
        event Action PreviousRequested;
    }

    public interface IHintGameView
    {
        void Setup(GameManager gameManager);
        void ShowHint(Data.HintData hint);
        void Clear();
        void SetVisible(bool visible);
        void SetInteractable(bool interactable);
        event Action<string> AnswerSubmitted;
        event Action BackRequested;
    }

    public interface IAnswerPopupView
    {
        void Setup(GameManager gameManager);
        void Show(string messageText, float displayDuration = 3f);
        void Hide();
        bool IsVisible { get; }
        event Action Dismissed;
    }

    public interface IRouletteView
    {
        void Setup(GameManager gameManager);
        IEnumerator Play(IReadOnlyList<string> rewardItemIds);
        IReadOnlyList<string> CollectedRewardTitles { get; }
        void ShowSummary(IReadOnlyList<string> rewardTitles);
        void ShowFailure(string message);
        void Dispose();
        event Action ContinueRequested;
    }
}
