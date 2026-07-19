using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using RevealGame.Data;
using RevealGame.Gameplay;
using UnityEngine;

namespace RevealGame.Importing
{
    public sealed class GameArchiveLoader
    {
        private const long MaxArchiveContentBytes = 256L * 1024L * 1024L;

        public bool TryLoad(string archivePath, out GameConfig config, out string error)
        {
            config = null;
            error = null;
            if (string.IsNullOrWhiteSpace(archivePath) || !File.Exists(archivePath))
            {
                error = "Choose an existing ZIP file.";
                return false;
            }
            if (!string.Equals(Path.GetExtension(archivePath), ".zip", StringComparison.OrdinalIgnoreCase))
            {
                error = "The selected file must have a .zip extension.";
                return false;
            }

            try
            {
                using var archiveStream = File.OpenRead(archivePath);
                using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);
                var entries = GetSafeEntries(archive, out error);
                if (entries == null)
                    return false;

                var yamlEntries = entries.Values.Where(entry =>
                    string.Equals(Path.GetExtension(entry.FullName), ".yaml", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(Path.GetExtension(entry.FullName), ".yml", StringComparison.OrdinalIgnoreCase)).ToList();
                if (yamlEntries.Count != 1)
                {
                    error = "The ZIP must contain exactly one .yaml or .yml configuration file.";
                    return false;
                }

                string yaml;
                using (var reader = new StreamReader(yamlEntries[0].Open(), Encoding.UTF8, true))
                    yaml = reader.ReadToEnd();
                if (!YamlGameConfigParser.TryParse(yaml, out config, out error) || !ValidateMediaPaths(config, entries, out error))
                    return false;

                var importDirectory = CreateImportDirectory();
                try
                {
                    foreach (var pair in entries)
                    {
                        var outputPath = Path.Combine(importDirectory, pair.Key.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                        using var input = pair.Value.Open();
                        using var output = File.Create(outputPath);
                        input.CopyTo(output);
                    }
                    ResolveMediaPaths(config, importDirectory);
                    return true;
                }
                catch
                {
                    if (Directory.Exists(importDirectory))
                        Directory.Delete(importDirectory, true);
                    throw;
                }
            }
            catch (InvalidDataException exception)
            {
                error = $"The ZIP file is invalid: {exception.Message}";
                return false;
            }
            catch (IOException exception)
            {
                error = $"The archive could not be read: {exception.Message}";
                return false;
            }
            catch (UnauthorizedAccessException exception)
            {
                error = $"The archive could not be imported: {exception.Message}";
                return false;
            }
        }

        private static Dictionary<string, ZipArchiveEntry> GetSafeEntries(ZipArchive archive, out string error)
        {
            error = null;
            var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.OrdinalIgnoreCase);
            long totalBytes = 0;
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;
                if (!TryNormalizePath(entry.FullName, out var path))
                {
                    error = $"The ZIP contains an unsafe path: '{entry.FullName}'.";
                    return null;
                }
                if (!entries.TryAdd(path, entry))
                {
                    error = $"The ZIP contains duplicate paths: '{path}'.";
                    return null;
                }
                totalBytes += entry.Length;
                if (totalBytes > MaxArchiveContentBytes)
                {
                    error = "The ZIP expands beyond the 256 MB import limit.";
                    return null;
                }
            }
            return entries;
        }

        private static bool ValidateMediaPaths(GameConfig config, IReadOnlyDictionary<string, ZipArchiveEntry> entries, out string error)
        {
            error = null;
            foreach (var item in config.items)
            {
                if (string.IsNullOrWhiteSpace(item.mediaPath))
                    continue;
                if (!TryValidateMediaPath(item.mediaPath, entries, false, out error))
                    return false;
            }
            foreach (var hint in config.hints)
            {
                foreach (var mediaPath in hint.mediaPaths)
                {
                    if (!TryValidateMediaPath(mediaPath, entries, true, out error))
                        return false;
                }
            }
            return true;
        }

        private static bool TryValidateMediaPath(string path, IReadOnlyDictionary<string, ZipArchiveEntry> entries, bool imageOnly, out string error)
        {
            error = null;
            if (!TryNormalizePath(path, out var normalizedPath))
            {
                error = $"Media path '{path}' is unsafe.";
                return false;
            }
            if (!entries.ContainsKey(normalizedPath))
            {
                error = $"Media file '{path}' was not found in the ZIP.";
                return false;
            }
            if (imageOnly && !MediaDisplayController.IsImageFile(normalizedPath))
            {
                error = $"Hint media '{path}' must be an image file.";
                return false;
            }
            if (!imageOnly && !MediaDisplayController.IsImageFile(normalizedPath) && !MediaDisplayController.IsVideoFile(normalizedPath))
            {
                error = $"Item media '{path}' must be an image or video file.";
                return false;
            }
            return true;
        }

        private static void ResolveMediaPaths(GameConfig config, string importDirectory)
        {
            foreach (var item in config.items)
            {
                if (!string.IsNullOrWhiteSpace(item.mediaPath))
                    item.mediaPath = ToAbsolutePath(item.mediaPath, importDirectory);
            }
            foreach (var hint in config.hints)
            {
                for (var index = 0; index < hint.mediaPaths.Length; index++)
                    hint.mediaPaths[index] = ToAbsolutePath(hint.mediaPaths[index], importDirectory);
            }
        }

        private static string CreateImportDirectory()
        {
            var directory = Path.Combine(Application.persistentDataPath, "RevealGame", "Imports", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return directory;
        }

        private static string ToAbsolutePath(string path, string importDirectory)
        {
            TryNormalizePath(path, out var normalizedPath);
            return Path.GetFullPath(Path.Combine(importDirectory, normalizedPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private static bool TryNormalizePath(string value, out string normalizedPath)
        {
            normalizedPath = null;
            if (string.IsNullOrWhiteSpace(value))
                return false;
            var candidate = value.Replace('\\', '/').Trim();
            if (candidate.StartsWith("/", StringComparison.Ordinal) || Path.IsPathRooted(candidate))
                return false;
            var parts = candidate.Split('/');
            if (parts.Any(part => string.IsNullOrWhiteSpace(part) || part == "." || part == ".."))
                return false;
            normalizedPath = string.Join("/", parts);
            return true;
        }
    }
}
