using System;
using System.IO;
using RevealGame.Importing;
using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    public class TitleScreenController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _uploadButton;
        [SerializeField] private Text _uploadStatusText;

        private GameManager _gameManager;
        private readonly GameArchiveLoader _archiveLoader = new GameArchiveLoader();

        private void Awake()
        {
            _gameManager = FindAnyObjectByType<GameManager>();

            if (_playButton == null)
                _playButton = GetComponentInChildren<Button>(true);

            if (_playButton == null)
            {
                Debug.LogError("[TitleScreenController] PlayButton not found");
                enabled = false;
                return;
            }

            _playButton.onClick.AddListener(OnPlayClicked);
            var label = _playButton.GetComponentInChildren<Text>(true);
            if (label != null) label.color = new Color(0.08f, 0.16f, 0.12f);

            EnsureUploadControls(label);
            if (_uploadButton != null)
                _uploadButton.onClick.AddListener(OnUploadClicked);
        }

        private void Start()
        {
            var archivePath = GetArchivePathFromArguments();
            if (!string.IsNullOrEmpty(archivePath))
                LoadArchiveAtPath(archivePath);
        }

        private void OnPlayClicked()
        {
            _gameManager.ShowLootboxScreen();
        }

        public void LoadArchiveAtPath(string archivePath)
        {
            if (_gameManager == null)
            {
                SetUploadStatus("Game manager is unavailable.", Color.red);
                return;
            }

            if (!_archiveLoader.TryLoad(archivePath, out var config, out var error))
            {
                SetUploadStatus(error, new Color(0.9f, 0.3f, 0.3f));
                return;
            }

            _gameManager.MediaDisplay?.ClearCache();
            _gameManager.InitializeGame(config);
            _gameManager.InitializeEngines();
            SetUploadStatus($"Loaded {Path.GetFileName(archivePath)}", new Color(0.45f, 0.9f, 0.55f));
        }

        private void OnUploadClicked()
        {
#if UNITY_EDITOR
            var archivePath = UnityEditor.EditorUtility.OpenFilePanel("Load Reveal Game ZIP", string.Empty, "zip");
            if (!string.IsNullOrEmpty(archivePath))
                LoadArchiveAtPath(archivePath);
#else
            SetUploadStatus("Launch with --game-archive=<path-to-zip> to load an archive.", new Color(1f, 0.78f, 0.28f));
#endif
        }

        private void EnsureUploadControls(Text playLabel)
        {
            if (_uploadButton == null)
            {
                var uploadObject = Instantiate(_playButton.gameObject, transform);
                uploadObject.name = "UploadZipButton";
                _uploadButton = uploadObject.GetComponent<Button>();
                _uploadButton.onClick.RemoveAllListeners();

                var uploadRect = uploadObject.GetComponent<RectTransform>();
                uploadRect.anchorMin = new Vector2(0.5f, 0.28f);
                uploadRect.anchorMax = new Vector2(0.5f, 0.28f);
                uploadRect.anchoredPosition = Vector2.zero;
                uploadRect.sizeDelta = new Vector2(260f, 50f);
            }

            var uploadLabel = _uploadButton.GetComponentInChildren<Text>(true);
            if (uploadLabel != null)
                uploadLabel.text = "Load Game ZIP";

            if (_uploadStatusText != null)
                return;

            var statusObject = new GameObject("UploadStatus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            statusObject.transform.SetParent(transform, false);
            var statusRect = statusObject.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.2f, 0.15f);
            statusRect.anchorMax = new Vector2(0.8f, 0.22f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            _uploadStatusText = statusObject.GetComponent<Text>();
            _uploadStatusText.font = playLabel != null ? playLabel.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _uploadStatusText.fontSize = 18;
            _uploadStatusText.alignment = TextAnchor.MiddleCenter;
            _uploadStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _uploadStatusText.verticalOverflow = VerticalWrapMode.Overflow;
            SetUploadStatus("Load a ZIP containing one YAML configuration and its media.", Color.white);
        }

        private void SetUploadStatus(string message, Color color)
        {
            if (_uploadStatusText == null)
                return;

            _uploadStatusText.text = message;
            _uploadStatusText.color = color;
        }

        private static string GetArchivePathFromArguments()
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length; index++)
            {
                const string prefix = "--game-archive=";
                if (arguments[index].StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return arguments[index].Substring(prefix.Length);
                if (string.Equals(arguments[index], "--game-archive", StringComparison.OrdinalIgnoreCase) && index + 1 < arguments.Length)
                    return arguments[index + 1];
            }
            return null;
        }
    }
}
