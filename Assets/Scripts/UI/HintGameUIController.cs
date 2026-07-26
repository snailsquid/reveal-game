using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RevealGame.UI
{
    /// <summary>
    /// Owns only ordered Next/Previous navigation for the hint-game UI.
    /// Phase-specific work is handled by HintGameOpeningCoordinator.
    /// </summary>
    public sealed class HintGameUIController : MonoBehaviour
    {
        [Header("Ordered UI Steps")]
        [SerializeField] private HintGameNavigationStep[] _steps;
        [SerializeField] private bool _allowExitFromFirstStep = true;

        private int _currentIndex;

        public event Action<HintGameNavigationStep> StepChanged;
        public event Action ExitRequested;
        public event Action Finished;

        public HintGameNavigationStep CurrentStep
            => _steps != null && _steps.Length > 0 && _currentIndex >= 0 && _currentIndex < _steps.Length
                ? _steps[_currentIndex]
                : null;

        public bool CanPrevious
            => CurrentStep != null
                && (_currentIndex == 0 ? _allowExitFromFirstStep : CurrentStep.CanGoPrevious);

        private void Awake()
        {
            EnsureSteps();

            if (GetComponent<HintGameOpeningCoordinator>() == null)
                gameObject.AddComponent<HintGameOpeningCoordinator>();
        }

        private void OnEnable()
        {
            if (_steps == null || _steps.Length == 0)
                return;

            _currentIndex = 0;
            SetAllStepsInactive();
            SetCurrentStepActive();
        }

        /// <summary>
        /// Moves to the next configured step, or finishes the flow at the end.
        /// </summary>
        public void Next()
        {
            if (CurrentStep == null)
                return;

            if (_currentIndex >= _steps.Length - 1)
            {
                CurrentStep.SetActive(false);
                Finished?.Invoke();
                return;
            }

            NavigateTo(_currentIndex + 1);
        }

        /// <summary>
        /// Moves to the previous configured step. At the first step it exits the
        /// hint-game screen when exit navigation is enabled.
        /// </summary>
        public void Previous()
        {
            if (CurrentStep == null || !CanPrevious)
                return;

            if (_currentIndex == 0)
            {
                CurrentStep.SetActive(false);
                ExitRequested?.Invoke();
                return;
            }

            NavigateTo(_currentIndex - 1);
        }

        private void EnsureSteps()
        {
            if (_steps != null && _steps.Length > 0
                && _steps.Any(step => step != null && step.GetComponent<HintGameView>() != null))
            {
                _steps = _steps.Where(step => step != null).OrderBy(step => step.Order).ToArray();
                return;
            }

            var configuredSteps = GetComponentsInChildren<HintGameNavigationStep>(true);
            if (configuredSteps.Any(step => step != null && step.GetComponent<HintGameView>() != null))
            {
                _steps = configuredSteps.OrderBy(step => step.Order).ToArray();
                return;
            }

            var discoveredSteps = new List<HintGameNavigationStep>();
            AddDiscoveredStep<PreGamePreviewView>(discoveredSteps, 0, true);
            AddDiscoveredStep<HintGameView>(discoveredSteps, 1, true, useCanvasGroup: true);
            AddDiscoveredStep<AnswerPopupView>(discoveredSteps, 2, false);
            AddDiscoveredStep<RouletteDisplay>(discoveredSteps, 3, false);
            AddDiscoveredStep<PostGamePreviewView>(discoveredSteps, 4, false);

            _steps = discoveredSteps.OrderBy(step => step.Order).ToArray();
        }

        private void AddDiscoveredStep<T>(List<HintGameNavigationStep> steps, int order,
            bool allowPrevious, bool useCanvasGroup = false) where T : Component
        {
            // Hint-game phases are standalone prefabs placed beside HintGameScreen
            // under the canvas, so they are not necessarily descendants of this controller.
            var component = GetComponentInChildren<T>(true)
                ?? FindAnyObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
                return;

            var step = component.GetComponent<HintGameNavigationStep>();
            if (step == null)
                step = component.gameObject.AddComponent<HintGameNavigationStep>();

            step.Configure(order, allowPrevious, useCanvasGroup);
            if (!steps.Contains(step))
                steps.Add(step);
        }

        private void NavigateTo(int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= _steps.Length || targetIndex == _currentIndex)
                return;

            CurrentStep?.SetActive(false);
            _currentIndex = targetIndex;
            SetCurrentStepActive();
        }

        private void SetCurrentStepActive()
        {
            CurrentStep?.SetActive(true);
            StepChanged?.Invoke(CurrentStep);
        }

        private void SetAllStepsInactive()
        {
            foreach (var step in _steps)
                step?.SetActive(false);
        }
    }
}
