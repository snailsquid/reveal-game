using UnityEngine;
using UnityEngine.UI;

namespace RevealGame.UI
{
    /// <summary>
    /// Ensures every scene and runtime-created uGUI button receives the shared interaction animation.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class ButtonInteractionAnimationInstaller : MonoBehaviour
    {
        private const float ScanInterval = 0.25f;

        private float _nextScanTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            EnsureInstalled();
        }

        /// <summary>
        /// Starts the global installer when a game bootstrap is available before scene-load hooks run.
        /// </summary>
        public static void EnsureInstalled()
        {
            if (FindAnyObjectByType<ButtonInteractionAnimationInstaller>() != null)
                return;

            var installer = new GameObject(nameof(ButtonInteractionAnimationInstaller));
            DontDestroyOnLoad(installer);
            installer.AddComponent<ButtonInteractionAnimationInstaller>();
        }

        private void Awake()
        {
            InstallOnAllButtons();
        }

        private void Update()
        {
            if (Time.unscaledTime < _nextScanTime)
                return;

            _nextScanTime = Time.unscaledTime + ScanInterval;
            InstallOnAllButtons();
        }

        private static void InstallOnAllButtons()
        {
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var button in buttons)
                ButtonInteractionAnimation.EnsureAttached(button);
        }
    }
}
