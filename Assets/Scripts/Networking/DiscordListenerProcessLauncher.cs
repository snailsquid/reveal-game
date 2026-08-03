using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace RevealGame.UI
{
    public sealed class DiscordListenerProcessLauncher : MonoBehaviour
    {
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private string _listenerDirectory;
        [SerializeField] private string _nodeExecutable = "node";
        [SerializeField, Min(1f)] private float _restartDelay = 3f;
        [SerializeField] private int _maxRestartAttempts = 5;

        private Process _listenerProcess;
        private bool _startedByThisComponent;
        private bool _intentionalStop;
        private Coroutine _restartCoroutine;

        private string _lastApiKey;
        private string _lastServerUrl;
        private string[] _lastChannelIds;
        private int _restartAttempts;

        public bool IsRunning => _listenerProcess != null && !_listenerProcess.HasExited;

        public event Action ListenerCrashed;

        public void StartListener(string gameApiKey, string serverUrl, string[] channelIds)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            _lastApiKey = gameApiKey;
            _lastServerUrl = serverUrl;
            _lastChannelIds = channelIds;
            _intentionalStop = false;

            if (!_autoStart || !IsLocalUrl(serverUrl) || IsRunning)
                return;

            if (string.IsNullOrWhiteSpace(gameApiKey))
            {
                UnityEngine.Debug.LogWarning("[DiscordListenerProcessLauncher] A game API key is required to start the local listener.");
                return;
            }

            string listenerDirectory = PrepareListenerDirectory(ResolveListenerDirectory());
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
                _restartAttempts = 0;
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

        public void RestartListener()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            StopListenerInternal();
            _intentionalStop = false;
            _restartAttempts = 0;
            StartListener(_lastApiKey, _lastServerUrl, _lastChannelIds);
#endif
        }

        public void StopListener()
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_STANDALONE_LINUX
            _intentionalStop = true;
            CancelRestart();
            StopListenerInternal();
#endif
        }

        private void StopListenerInternal()
        {
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
            string editorDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "discord-listener"));
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

        private string PrepareListenerDirectory(string listenerDirectory)
        {
            string bundledModulesArchive = Path.Combine(listenerDirectory, "node_modules.bytes");
            if (!File.Exists(bundledModulesArchive))
                bundledModulesArchive = Path.Combine(listenerDirectory, "node_modules.zip");
            string bundledModulesDirectory = Path.Combine(listenerDirectory, "modules");
            if (Directory.Exists(Path.Combine(listenerDirectory, "node_modules"))
                || (!File.Exists(bundledModulesArchive) && !Directory.Exists(bundledModulesDirectory)))
                return listenerDirectory;

            string extractedDirectory = Path.Combine(Application.persistentDataPath, "discord-listener");
            string extractedModules = Path.Combine(extractedDirectory, "node_modules");
            if (!Directory.Exists(extractedModules))
            {
                if (Directory.Exists(extractedDirectory))
                    Directory.Delete(extractedDirectory, true);

                Directory.CreateDirectory(extractedDirectory);
                CopyDirectory(listenerDirectory, extractedDirectory);
                if (Directory.Exists(bundledModulesDirectory))
                {
                    CopyDirectory(bundledModulesDirectory, extractedModules);
                }
                else
                {
                    Directory.CreateDirectory(extractedModules);
                    ZipFile.ExtractToDirectory(bundledModulesArchive, extractedModules);
                }
            }

            return extractedDirectory;
        }

        private static void CopyDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);
            foreach (string file in Directory.GetFiles(sourceDirectory))
            {
                string fileName = Path.GetFileName(file);
                if (string.Equals(fileName, "node_modules.bytes", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(fileName, "node_modules.zip", StringComparison.OrdinalIgnoreCase))
                    continue;

                File.Copy(file, Path.Combine(destinationDirectory, fileName), true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDirectory))
            {
                CopyDirectory(directory, Path.Combine(destinationDirectory, Path.GetFileName(directory)));
            }
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
            DisposeProcess();

            if (_intentionalStop)
                return;

            UnityEngine.Debug.LogWarning("[DiscordListenerProcessLauncher] Local listener exited unexpectedly.");
            ListenerCrashed?.Invoke();
            ScheduleRestart();
        }

        private void ScheduleRestart()
        {
            if (_restartAttempts >= _maxRestartAttempts)
            {
                UnityEngine.Debug.LogError($"[DiscordListenerProcessLauncher] Max restart attempts ({_maxRestartAttempts}) reached. Use RestartListener() to retry manually.");
                return;
            }

            _restartAttempts++;
            CancelRestart();
            _restartCoroutine = StartCoroutine(RestartAfterDelay());
        }

        private IEnumerator RestartAfterDelay()
        {
            UnityEngine.Debug.Log($"[DiscordListenerProcessLauncher] Restarting listener in {_restartDelay}s (attempt {_restartAttempts}/{_maxRestartAttempts})...");
            yield return new WaitForSecondsRealtime(_restartDelay);
            _restartCoroutine = null;

            if (!_intentionalStop && !IsRunning)
                StartListener(_lastApiKey, _lastServerUrl, _lastChannelIds);
        }

        private void CancelRestart()
        {
            if (_restartCoroutine != null)
            {
                StopCoroutine(_restartCoroutine);
                _restartCoroutine = null;
            }
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
