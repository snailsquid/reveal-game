using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace RevealGame.UI
{
    public class AnswerPopupView : MonoBehaviour, IAnswerPopupView
    {
        [Header("UI References")]
        [SerializeField] private GameObject _popupRoot;
        [SerializeField] private Text _playerText;
        [SerializeField] private Text _answerText;
        [SerializeField] private Button _continueButton;

        [Header("Animation")]
        [SerializeField] private float _autoDismissDelay = 3f;
        [SerializeField] private bool _autoDismiss = true;

        private GameManager _gameManager;
        private Coroutine _autoDismissCoroutine;
        private HintGameNavigationStep _navigationStep;
        private UiTransition _transition;

        private Data.GameSettings Settings
            => _gameManager?.Session?.Config?.settings;

        public event Action Dismissed;

        public bool IsVisible => _popupRoot != null && _popupRoot.activeSelf;

        private void Awake()
        {
            AutoDiscoverUIReferences();
            _navigationStep = GetComponent<HintGameNavigationStep>();

            if (_popupRoot != null)
            {
                _transition = UiTransition.GetOrAdd(_popupRoot);
                _transition.SetVisibleImmediately(false);
            }

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void AutoDiscoverUIReferences()
        {
            if (_popupRoot == null) _popupRoot = gameObject;
            if (_playerText == null) _playerText = FindUIComponent<Text>("Player");
            if (_answerText == null) _answerText = FindUIComponent<Text>("Answer");
            if (_continueButton == null)
                _continueButton = FindUIComponent<Button>("ContinueButton", "Continue");
        }

        private T FindUIComponent<T>(params string[] names) where T : Component
        {
            var components = GetComponentsInChildren<T>(true);
            foreach (var name in names)
            {
                foreach (var comp in components)
                {
                    if (comp.gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return comp;
                }
            }
            return null;
        }

        private void OnDisable()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
        }

        public void Setup(GameManager gameManager)
        {
            _gameManager = gameManager;
        }

        public void Show(string player, string answer, float displayDuration = -1f)
        {
            if (_popupRoot == null) return;

            if (_playerText != null)
                _playerText.text = player ?? string.Empty;
            if (_answerText != null)
                _answerText.text = answer ?? string.Empty;

            _transition ??= UiTransition.GetOrAdd(_popupRoot);
            _transition.Show();

            float dismissDelay = displayDuration > 0f ? displayDuration : _autoDismissDelay;
            if (_autoDismiss && dismissDelay > 0f)
            {
                if (_autoDismissCoroutine != null)
                    StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = StartCoroutine(AutoDismissCoroutine(dismissDelay));
            }
        }

        public void Hide()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }

            _transition?.Hide();
        }

        private void OnContinueClicked()
        {
            DismissAndAdvance();
        }

        private IEnumerator AutoDismissCoroutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _autoDismissCoroutine = null;
            DismissAndAdvance();
        }

        private void DismissAndAdvance()
        {
            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }

            if (_transition == null)
            {
                _navigationStep?.RequestNext();
                Dismissed?.Invoke();
                return;
            }

            _transition.Hide(() =>
            {
                _navigationStep?.RequestNext();
                Dismissed?.Invoke();
            });
        }
    }
}
