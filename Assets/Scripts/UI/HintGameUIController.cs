using System;
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
            _steps = _steps == null
                ? System.Array.Empty<HintGameNavigationStep>()
                : _steps.Where(step => step != null).OrderBy(step => step.Order).ToArray();
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
