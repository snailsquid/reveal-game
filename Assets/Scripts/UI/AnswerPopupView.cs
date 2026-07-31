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
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _continueButton;

        [Header("Animation")]
        [SerializeField] private float _autoDismissDelay = 3f;
        [SerializeField] private bool _autoDismiss = true;

        private GameManager _gameManager;
        private Coroutine _autoDismissCoroutine;
        private HintGameNavigationStep _navigationStep;

        private Data.GameSettings Settings
            => _gameManager?.Session?.Config?.settings;

        public event Action Dismissed;

        public bool IsVisible => _popupRoot != null && _popupRoot.activeSelf;

        private void Awake()
        {
            AutoDiscoverUIReferences();
            _navigationStep = GetComponent<HintGameNavigationStep>();

            if (_popupRoot != null)
                _popupRoot.SetActive(false);

            if (_continueButton != null)
                _continueButton.onClick.AddListener(OnContinueClicked);
        }

        private void AutoDiscoverUIReferences()
        {
            if (_popupRoot == null) _popupRoot = gameObject;
            if (_resultText == null) _resultText = GetComponentInChildren<Text>(true);
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

        public void Show(string messageText, float displayDuration = -1f)
        {
            if (_popupRoot == null) return;

            if (_resultText != null)
                _resultText.text = messageText;

            _popupRoot.SetActive(true);

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
            if (_popupRoot != null)
                _popupRoot.SetActive(false);

            if (_autoDismissCoroutine != null)
            {
                StopCoroutine(_autoDismissCoroutine);
                _autoDismissCoroutine = null;
            }
        }

        private void OnContinueClicked()
        {
            Hide();
            _navigationStep?.RequestNext();
            Dismissed?.Invoke();
        }

        private IEnumerator AutoDismissCoroutine(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _autoDismissCoroutine = null;
            Hide();
            _navigationStep?.RequestNext();
            Dismissed?.Invoke();
        }
    }
}
