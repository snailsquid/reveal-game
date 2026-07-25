using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RevealGame.Editor
{
    public class DiscordListenerBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string sourceDirectory = Path.GetFullPath(Path.Combine(projectRoot, "discord-listener"));

            if (!Directory.Exists(sourceDirectory))
            {
                throw new BuildFailedException($"Discord listener directory was not found: {sourceDirectory}");
            }

            string destinationDirectory = Path.Combine(Application.streamingAssetsPath, "discord-listener");
            string zipPath = Path.Combine(destinationDirectory, "node_modules.zip");

            if (Directory.Exists(destinationDirectory))
            {
                Directory.Delete(destinationDirectory, true);
            }

            Directory.CreateDirectory(destinationDirectory);
            CopyDirectory(sourceDirectory, destinationDirectory);

            if (Directory.Exists(Path.Combine(destinationDirectory, "node_modules")))
            {
                Directory.Delete(Path.Combine(destinationDirectory, "node_modules"), true);
            }

            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }

            string sourceNodeModules = Path.Combine(sourceDirectory, "node_modules");
            if (Directory.Exists(sourceNodeModules))
            {
                ZipFile.CreateFromDirectory(sourceNodeModules, zipPath);
            }
            else
            {
                Debug.LogWarning("discord-listener/node_modules not found. Run 'npm install' in discord-listener before building.");
            }
        }

        private static void CopyDirectory(string sourceDir, string destinationDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                if (fileName == ".env" || fileName == ".gitignore")
                    continue;
                File.Copy(file, Path.Combine(destinationDir, fileName));
            }

            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dir);
                if (dirName == "node_modules" || dirName == ".git")
                    continue;
                string destDir = Path.Combine(destinationDir, dirName);
                Directory.CreateDirectory(destDir);
                CopyDirectory(dir, destDir);
            }
        }
    }
}
