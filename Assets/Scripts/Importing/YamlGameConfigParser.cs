using System;
using System.Collections.Generic;
using System.Globalization;
using RevealGame.Data;

namespace RevealGame.Importing
{
    public static class YamlGameConfigParser
    {
        private sealed class Line
        {
            public int Number;
            public int Indent;
            public string Text;
        }

        private sealed class Entry
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, List<string>> Lists = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public static bool TryParse(string yaml, out GameConfig config, out string error)
        {
            config = null;
            error = null;
            if (string.IsNullOrWhiteSpace(yaml))
            {
                error = "The YAML file is empty.";
                return false;
            }

            var lines = ReadLines(yaml, out error);
            if (lines == null)
                return false;

            var sections = ReadSections(lines, out error);
            if (sections == null)
                return false;

            if (!sections.TryGetValue("items", out var itemEntries) || itemEntries.Count == 0)
            {
                error = "YAML must contain at least one item under 'items'.";
                return false;
            }

            if (!sections.TryGetValue("settings", out var settingEntries) || settingEntries.Count != 1)
            {
                error = "YAML must contain a 'settings' mapping.";
                return false;
            }

            if (!TryReadPositiveInt(settingEntries[0], "nLootbox", out var lootboxCount, out error))
                return false;

            var items = new List<ItemData>(itemEntries.Count);
            var itemIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in itemEntries)
            {
                if (!TryGetRequired(entry, "id", out var id, out error) ||
                    !TryGetRequired(entry, "title", out var title, out error) ||
                    !TryGetRequired(entry, "description", out var description, out error))
                    return false;

                if (!itemIds.Add(id))
                {
                    error = $"Item id '{id}' is duplicated.";
                    return false;
                }

                if (!TryReadBool(entry, "isWinner", false, out var isWinner, out error))
                    return false;

                items.Add(new ItemData
                {
                    id = id,
                    title = title,
                    description = description,
                    mediaPath = GetFirst(entry, "mediaPath", "imagePath", "videoPath", "path", "video/image path") ?? string.Empty,
                    isWinner = isWinner
                });
            }

            if (lootboxCount > items.Count)
            {
                error = $"'nLootbox' ({lootboxCount}) cannot exceed the number of items ({items.Count}).";
                return false;
            }

            var hints = new List<HintData>();
            if (sections.TryGetValue("hints", out var hintEntries))
            {
                for (var index = 0; index < hintEntries.Count; index++)
                {
                    var entry = hintEntries[index];
                    if (!TryGetRequired(entry, "hintType", out var hintTypeText, out error) ||
                        !TryGetRequired(entry, "answer", out var answer, out error))
                        return false;

                    if (!string.Equals(hintTypeText, "image", StringComparison.OrdinalIgnoreCase))
                    {
                        error = $"Hint {index + 1} has unsupported hintType '{hintTypeText}'. Only 'image' is supported.";
                        return false;
                    }

                    if (!TryReadBool(entry, "isCaseSensitive", false, out var isCaseSensitive, out error))
                        return false;

                    var paths = GetList(entry, "mediaPaths", "imagePaths", "paths", "video/image paths");
                    if (paths.Count == 0)
                    {
                        error = $"Hint {index + 1} must contain at least one image path.";
                        return false;
                    }

                    hints.Add(new HintData
                    {
                        id = GetFirst(entry, "id") ?? $"hint_{index + 1}",
                        title = GetFirst(entry, "title") ?? string.Empty,
                        hintType = HintType.Image,
                        answer = answer,
                        isCaseSensitive = isCaseSensitive,
                        mediaPaths = paths.ToArray()
                    });
                }
            }

            if (hints.Count == 0)
            {
                error = "YAML must contain at least one hint under 'hints'.";
                return false;
            }

            config = new GameConfig
            {
                items = items.ToArray(),
                hints = hints.ToArray(),
                settings = new GameSettings { nLootbox = lootboxCount }
            };
            return true;
        }

        private static List<Line> ReadLines(string yaml, out string error)
        {
            error = null;
            var result = new List<Line>();
            var rawLines = yaml.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            for (var index = 0; index < rawLines.Length; index++)
            {
                var text = StripComment(rawLines[index]).TrimEnd();
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (text.IndexOf('\t') >= 0)
                {
                    error = $"Line {index + 1}: tabs are not supported for YAML indentation.";
                    return null;
                }

                var indent = text.Length - text.TrimStart(' ').Length;
                result.Add(new Line { Number = index + 1, Indent = indent, Text = text.TrimStart(' ') });
            }
            return result;
        }

        private static Dictionary<string, List<Entry>> ReadSections(List<Line> lines, out string error)
        {
            error = null;
            var sections = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < lines.Count;)
            {
                var line = lines[index];
                if (line.Indent != 0 || !TrySplitKeyValue(line.Text, out var sectionName, out var value) || !string.IsNullOrEmpty(value))
                {
                    error = $"Line {line.Number}: expected a top-level section such as 'items:' or 'settings:'.";
                    return null;
                }

                index++;
                if (string.Equals(sectionName, "settings", StringComparison.OrdinalIgnoreCase))
                {
                    var settings = ReadSingleMapping(lines, ref index, line.Indent, out error);
                    if (settings == null)
                        return null;
                    sections[sectionName] = new List<Entry> { settings };
                }
                else
                {
                    var entries = ReadSequence(lines, ref index, line.Indent, out error);
                    if (entries == null)
                        return null;
                    sections[sectionName] = entries;
                }
            }
            return sections;
        }

        private static Entry ReadSingleMapping(List<Line> lines, ref int index, int parentIndent, out string error)
        {
            error = null;
            var entry = new Entry();
            while (index < lines.Count && lines[index].Indent > parentIndent)
            {
                var line = lines[index];
                if (line.Text.StartsWith("-", StringComparison.Ordinal) || !TrySplitKeyValue(line.Text, out var key, out var value) || string.IsNullOrEmpty(value))
                {
                    error = $"Line {line.Number}: expected a settings value.";
                    return null;
                }
                entry.Values[key] = Unquote(value);
                index++;
            }
            return entry;
        }

        private static List<Entry> ReadSequence(List<Line> lines, ref int index, int parentIndent, out string error)
        {
            error = null;
            var entries = new List<Entry>();
            Entry current = null;
            var entryIndent = -1;
            while (index < lines.Count && lines[index].Indent > parentIndent)
            {
                var line = lines[index];
                if (line.Text == "-" || line.Text.StartsWith("- ", StringComparison.Ordinal))
                {
                    if (entryIndent >= 0 && line.Indent != entryIndent)
                    {
                        error = $"Line {line.Number}: list entries must use the same indentation.";
                        return null;
                    }
                    entryIndent = line.Indent;
                    current = new Entry();
                    entries.Add(current);
                    var firstProperty = line.Text == "-" ? string.Empty : line.Text.Substring(2);
                    if (!string.IsNullOrEmpty(firstProperty) && !AddValue(current, firstProperty, line.Number, out error))
                        return null;
                    index++;
                    continue;
                }

                if (current == null || line.Indent <= entryIndent || !TrySplitKeyValue(line.Text, out var key, out var value))
                {
                    error = $"Line {line.Number}: expected a list item property.";
                    return null;
                }

                index++;
                if (!string.IsNullOrEmpty(value))
                {
                    current.Values[key] = Unquote(value);
                    continue;
                }

                var values = ReadScalarList(lines, ref index, line.Indent, out error);
                if (values == null)
                    return null;
                current.Lists[key] = values;
            }
            return entries;
        }

        private static bool AddValue(Entry entry, string text, int lineNumber, out string error)
        {
            error = null;
            if (!TrySplitKeyValue(text, out var key, out var value) || string.IsNullOrEmpty(value))
            {
                error = $"Line {lineNumber}: expected 'key: value' after '-'.";
                return false;
            }
            entry.Values[key] = Unquote(value);
            return true;
        }

        private static List<string> ReadScalarList(List<Line> lines, ref int index, int parentIndent, out string error)
        {
            error = null;
            var values = new List<string>();
            while (index < lines.Count && lines[index].Indent > parentIndent)
            {
                var line = lines[index];
                if (line.Text == "-" || !line.Text.StartsWith("- ", StringComparison.Ordinal))
                {
                    error = $"Line {line.Number}: expected a scalar list item.";
                    return null;
                }
                values.Add(Unquote(line.Text.Substring(2)));
                index++;
            }
            return values;
        }

        private static bool TryGetRequired(Entry entry, string key, out string value, out string error)
        {
            value = GetFirst(entry, key);
            error = null;
            if (!string.IsNullOrWhiteSpace(value))
                return true;
            error = $"A required '{key}' value is missing.";
            return false;
        }

        private static bool TryReadPositiveInt(Entry entry, string key, out int value, out string error)
        {
            value = 0;
            error = null;
            if (!int.TryParse(GetFirst(entry, key), NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value <= 0)
            {
                error = $"'{key}' must be a positive integer.";
                return false;
            }
            return true;
        }

        private static bool TryReadBool(Entry entry, string key, bool fallback, out bool value, out string error)
        {
            value = fallback;
            error = null;
            var rawValue = GetFirst(entry, key);
            if (string.IsNullOrEmpty(rawValue))
                return true;
            if (bool.TryParse(rawValue, out value))
                return true;
            error = $"'{key}' must be true or false.";
            return false;
        }

        private static string GetFirst(Entry entry, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (entry.Values.TryGetValue(key, out var value))
                    return value;
            }
            return null;
        }

        private static List<string> GetList(Entry entry, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (entry.Lists.TryGetValue(key, out var values))
                    return values;
                if (entry.Values.TryGetValue(key, out var inlineValue) && inlineValue.StartsWith("[", StringComparison.Ordinal) && inlineValue.EndsWith("]", StringComparison.Ordinal))
                    return SplitInlineList(inlineValue.Substring(1, inlineValue.Length - 2));
            }
            return new List<string>();
        }

        private static List<string> SplitInlineList(string value)
        {
            var result = new List<string>();
            foreach (var part in value.Split(','))
            {
                var trimmed = Unquote(part.Trim());
                if (!string.IsNullOrEmpty(trimmed))
                    result.Add(trimmed);
            }
            return result;
        }

        private static bool TrySplitKeyValue(string text, out string key, out string value)
        {
            key = null;
            value = null;
            var quote = '\0';
            for (var index = 0; index < text.Length; index++)
            {
                var character = text[index];
                if ((character == '\'' || character == '"') && (index == 0 || text[index - 1] != '\\'))
                    quote = quote == '\0' ? character : quote == character ? '\0' : quote;
                if (character == ':' && quote == '\0')
                {
                    key = Unquote(text.Substring(0, index).Trim());
                    value = text.Substring(index + 1).Trim();
                    return !string.IsNullOrEmpty(key);
                }
            }
            return false;
        }

        private static string StripComment(string value)
        {
            var quote = '\0';
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if ((character == '\'' || character == '"') && (index == 0 || value[index - 1] != '\\'))
                    quote = quote == '\0' ? character : quote == character ? '\0' : quote;
                if (character == '#' && quote == '\0')
                    return value.Substring(0, index);
            }
            return value;
        }

        private static string Unquote(string value)
        {
            value = value.Trim();
            if (value.Length >= 2 && ((value[0] == '"' && value[value.Length - 1] == '"') || (value[0] == '\'' && value[value.Length - 1] == '\'')))
                return value.Substring(1, value.Length - 2);
            return value;
        }
    }
}
