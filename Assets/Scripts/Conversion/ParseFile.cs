using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Conversion
{
    public static class ParseFile
    {
        public static void Main(string target, List<(string, object)> fileTree)
        {
            var currentBranch = fileTree;
            var currentChain = new List<int>();
            var parent = string.Empty;
            const string skipString = "skipping!";

            foreach (var rawLine in File.ReadLines(target))
            {
                if (CommentDetector(rawLine, out var line))
                    continue;

                var curEquals = SplitByFirstEquals(line);

                while (true)
                {
                    CheckClosingBrackets(ref curEquals[0], ref currentBranch);

                    if (DetectBranchType(curEquals, out var curLevel))
                    {
                        if (CheckOpenBrackets(ref curEquals[1]))
                        {
                            // Updating trackers
                            currentChain.Add(currentBranch.Count);
                            parent = curLevel;

                            var newBranch = new List<(string, object)>();
                            currentBranch.Add((curLevel, newBranch));
                            currentBranch = newBranch;
                        }
                        else
                        {
                            currentBranch.Add((curLevel, InlineSplitter(ref curEquals[1])));
                        }
                    }

                    if (string.IsNullOrEmpty(curEquals[0]) || string.Equals(curLevel, skipString))
                        break;

                    curEquals = SplitByFirstEquals(curEquals[1]);
                }
            }

            bool DetectBranchType(IList<string> raw, out string parsed)
            {
                if (string.IsNullOrEmpty(raw[0]) || string.IsNullOrEmpty(raw[1]))
                {
                    parsed = string.Empty;
                    return false;
                }

                parsed = raw[0].Trim();

                // Color overrides
                if (!string.Equals(parsed, "color"))
                    return true;

                // TODO: add checker in case color line contains an exit bracket. IE: color = { 0 0 0 } >}<
                if (string.IsNullOrEmpty(raw[1]))
                    return false;

                currentBranch.Add((parsed, raw[1]));

                parsed = skipString;
                return false;
            }

            bool CheckOpenBrackets(ref string raw)
            {
                /* LIMITATION:
             * Open brackets MUST BE IN THE SAME LINE as value declaration.
             * So no:
             * Example =
             * { # No.
             *     Hello = World
             * }
             * This is because of IEnumerable limitations.
             * Sure, I can do ReadAllLines but that requires loading the entire file into memory.
             * This mainly concerns technology in common, Army_Tech open bracket is on new line.
             */
                var indexBracket = raw.IndexOf('{');
                var indexEqual = raw.IndexOf('=');

                if (indexBracket == -1)
                    return false;

                if (indexEqual > -1 && indexBracket > indexEqual)
                    return false;

                raw = raw.Remove(indexBracket, 1);
                return true;
            }

            string InlineSplitter(ref string raw)
            {
                var value = Regex.Match(raw,
                    raw[0].Equals('"')
                        ? @"^.+?\"""
                        : @"^.+?(?![a-zA-Z0-9\._])");

                if (!value.Success)
                    throw new Exception("Value not found. " + raw);

                raw = raw.Substring(value.Length).Trim();
                return value.Value;
            }

            void CheckClosingBrackets(ref string raw, ref List<(string Key, object Value)> deltaBranch)
            {
                var numClosed = raw.Count(c => c == '}');

                if (numClosed == 0)
                    return;

                raw = raw.Replace("}", "");

                var min = currentChain.Count - numClosed;
                if (min < 0)
                {
                    numClosed += min;
                    min = 0;
                }

                currentChain.RemoveRange(min, numClosed);
                deltaBranch = fileTree;
                foreach (var newChain in currentChain)
                {
                    // Updating parent
                    parent = deltaBranch[newChain].Key;
                    deltaBranch = (List<(string, object)>) deltaBranch[newChain].Value;
                }

                if (currentChain.Count != 0)
                    return;

                parent = string.Empty;
            }

            string[] SplitByFirstEquals(string input)
            {
                input = input.Trim();
                var equals = Regex.Match(input, @"^.+?(?=\=)");

                var output = new[] {input, string.Empty};
                if (equals.Success)
                    output = new[] {equals.Value.Trim(), input.Substring(equals.Length + 1).Trim()};

                return output;
            }
            
            bool CommentDetector(string line, out string sliced)
            {
                // Comment Detector. Will also lowercase everything. Throwing away comments.
                sliced = line.ToLowerInvariant().Split(new[] {"#"}, StringSplitOptions.None)[0].Trim();
                return sliced.Length == 0;
            }
        }
    }
}
