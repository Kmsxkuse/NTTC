using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Conversion
{
    public static class LocalizationLoad
    {
        public static Dictionary<string, string> Main()
        {
            var outputDictionary = new Dictionary<string, string>();

            foreach (var localFilePath in Directory.EnumerateFiles(Path.Combine(Application.streamingAssetsPath,
                "Localization")))
            {
                foreach (var rawLine in File.ReadLines(localFilePath))
                {
                    if (LoadMethods.CommentDetector(rawLine, out var line))
                        continue;

                    // Dumping non-english sections. America!
                    var raw = Regex.Match(line, @"^.*?(?=\;)");
                    var key = raw.Value.Replace(";", "").Trim();
                    if (key == string.Empty)
                        continue;

                    var value = Regex.Match(line.Substring(raw.Length + 1), @"^.*?(?=\;|$)").Value.Trim();
                    if (value == string.Empty)
                        continue;

                    // Capitalizing
                    value = Regex.Replace(value, @"(\b[a-z])", Capitalizing);

                    if (outputDictionary.TryGetValue(key, out _))
                        outputDictionary.Remove(key);

                    outputDictionary.Add(key, value);
                }

                string Capitalizing(Match m)
                {
                    return m.Groups[1].Value.ToUpper();
                }
            }

            return outputDictionary;
        }
    }
}