using RevealGame.UI;
using UnityEditor;
using UnityEngine;

namespace RevealGame.Editor
{
    /// <summary>
    /// Provides an editor-only shortcut for testing the Discord correct-answer flow.
    /// </summary>
    public sealed class DiscordCorrectAnswerSimulatorWindow : EditorWindow
    {
        private const string DefaultUsername = "Discord Test User";

        private string _username = DefaultUsername;

        [MenuItem("Tools/Discord/Simulate Correct Answer")]
        public static void ShowWindow()
        {
            GetWindow<DiscordCorrectAnswerSimulatorWindow>("Discord Simulator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Discord Correct Answer Simulator", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "While the game is playing and a hint is open, submit its correct answer as a simulated Discord user.",
                MessageType.Info);

            _username = EditorGUILayout.TextField("Discord Username", _username);

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode and open a hint before simulating an answer.", MessageType.Warning);
                return;
            }

            DiscordAnswerListener listener = FindFirstObjectByType<DiscordAnswerListener>();
            if (listener == null)
            {
                EditorGUILayout.HelpBox("No Discord answer listener is active in the current scene.", MessageType.Warning);
                return;
            }

            if (!listener.HasPendingHintForEditor)
            {
                EditorGUILayout.HelpBox("Open a hint and wait for its Discord listener to start.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Simulate Correct Discord Answer", GUILayout.Height(32)))
            {
                string username = string.IsNullOrWhiteSpace(_username) ? DefaultUsername : _username;
                if (!listener.SimulateCorrectAnswer(username))
                {
                    Debug.LogWarning("[DiscordCorrectAnswerSimulator] No pending hint was available to simulate.");
                    return;
                }

                Debug.Log($"[DiscordCorrectAnswerSimulator] Simulated a correct Discord answer from '{username}'.");
            }
        }
    }
}
