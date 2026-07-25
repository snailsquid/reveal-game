using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace RevealGame.UI
{
    public sealed class DiscordListenerProcessLauncher : MonoBehaviour
    {
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private string _listenerDirectory;
        [SerializeField] private string _nodeExecutable = "node";

        private Process _listenerProcess;
        private bool _startedByThisComponent;

        public bool IsRunning => _listenerProcess != null && !_listenerProcess.HasExited;

        public void StartListener(string gameApiKey, string serverUrl, string[] channelIds)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            if (!_autoStart || !IsLocalUrl(serverUrl) || IsRunning)
                return;

            if (string.IsNullOrWhiteSpace(gameApiKey))
            {
                UnityEngine.Debug.LogWarning("[DiscordListenerProcessLauncher] A game API key is required to start the local listener.");
                return;
            }

            string listenerDirectory = ResolveListenerDirectory();
            if (!Directory.Exists(listenerDirectory))
            {
                UnityEngine.Debug.LogWarning($"[DiscordListenerProcessLauncher] Listener directory was not found: {listenerDirectory}");
                return;
            }

            string entryPoint = Path.Combine(listenerDirectory, "src", "index.js");
            if (!File.Exists(entryPoint))
            {
                UnityEngine.Debug.LogWarning($"[DiscordListenerProcessLauncher] Listener entry point was not found: {entryPoint}");
                return;
            }

            string nodeExecutable = ResolveNodeExecutable(listenerDirectory);
            var startInfo = new ProcessStartInfo
            {
                FileName = nodeExecutable,
                Arguments = "--env-file-if-exists=.env src/index.js",
                WorkingDirectory = listenerDirectory,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            startInfo.Environment["GAME_API_KEY"] = gameApiKey;

            startInfo.Environment["DISCORD_ALLOWED_CHANNEL_IDS"] = string.Join(",", channelIds ?? Array.Empty<string>());
            try
            {
                _listenerProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _listenerProcess.OutputDataReceived += OnListenerOutput;
                _listenerProcess.ErrorDataReceived += OnListenerError;
                _listenerProcess.Exited += OnListenerExited;

                if (!_listenerProcess.Start())
                {
                    _listenerProcess.Dispose();
                    _listenerProcess = null;
                    return;
                }

                _startedByThisComponent = true;
                _listenerProcess.BeginOutputReadLine();
                _listenerProcess.BeginErrorReadLine();
                UnityEngine.Debug.Log($"[DiscordListenerProcessLauncher] Started local listener using {nodeExecutable}.");
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogError($"[DiscordListenerProcessLauncher] Could not start local listener: {error.Message}");
                DisposeProcess();
            }
#else
            UnityEngine.Debug.LogWarning("[DiscordListenerProcessLauncher] Local listener startup is supported only in the Unity Editor and desktop standalone builds.");
#endif
        }

        public void StopListener()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            if (!_startedByThisComponent || _listenerProcess == null)
                return;

            try
            {
                if (!_listenerProcess.HasExited)
                    _listenerProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception error)
            {
                UnityEngine.Debug.LogWarning($"[DiscordListenerProcessLauncher] Could not stop local listener: {error.Message}");
            }
            finally
            {
                DisposeProcess();
            }
#endif
        }

        private void OnApplicationQuit()
        {
            StopListener();
        }

        private void OnDestroy()
        {
            StopListener();
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
        private string ResolveListenerDirectory()
        {
            if (!string.IsNullOrWhiteSpace(_listenerDirectory))
            {
                return Path.GetFullPath(Path.IsPathRooted(_listenerDirectory)
                    ? _listenerDirectory
                    : Path.Combine(Application.dataPath, _listenerDirectory));
            }

#if UNITY_EDITOR
            string editorDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "discord-listener"));
            if (Directory.Exists(editorDirectory))
                return editorDirectory;
#endif

            string bundledDirectory = Path.Combine(Application.streamingAssetsPath, "discord-listener");
            if (Directory.Exists(bundledDirectory))
                return bundledDirectory;

            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "discord-listener"));
        }

        private string ResolveNodeExecutable(string listenerDirectory)
        {
            if (!string.IsNullOrWhiteSpace(_nodeExecutable) && !string.Equals(_nodeExecutable, "node", StringComparison.OrdinalIgnoreCase))
            {
                return Path.IsPathRooted(_nodeExecutable)
                    ? _nodeExecutable
                    : Path.GetFullPath(Path.Combine(listenerDirectory, _nodeExecutable));
            }

            string bundledNodeName = Application.platform == RuntimePlatform.WindowsPlayer ? "node.exe" : "node";
            string bundledNode = Path.Combine(listenerDirectory, "runtime", bundledNodeName);
            return File.Exists(bundledNode) ? bundledNode : _nodeExecutable;
        }

        private static bool IsLocalUrl(string serverUrl)
        {
            if (!Uri.TryCreate(serverUrl, UriKind.Absolute, out var uri))
                return false;

            return uri.Scheme == Uri.UriSchemeHttp &&
                (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                 uri.Host == "127.0.0.1" ||
                 uri.Host == "[::1]");
        }

        private void OnListenerOutput(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
                UnityEngine.Debug.Log($"[DiscordListener] {args.Data}");
        }

        private void OnListenerError(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
                UnityEngine.Debug.LogWarning($"[DiscordListener] {args.Data}");
        }

        private void OnListenerExited(object sender, EventArgs args)
        {
            UnityEngine.Debug.LogWarning("[DiscordListenerProcessLauncher] Local listener exited.");
            DisposeProcess();
        }

        private void DisposeProcess()
        {
            if (_listenerProcess == null)
                return;

            _listenerProcess.OutputDataReceived -= OnListenerOutput;
            _listenerProcess.ErrorDataReceived -= OnListenerError;
            _listenerProcess.Exited -= OnListenerExited;
            _listenerProcess.Dispose();
            _listenerProcess = null;
            _startedByThisComponent = false;
        }
#endif
    }
}
