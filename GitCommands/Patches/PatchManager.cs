﻿using System.Diagnostics;
        public static byte[]? GetSelectedLinesAsPatch(string text, int selectionPosition, int selectionLength, bool isIndex, Encoding fileContentEncoding, bool reset, bool isNewFile, bool isRenamed)
            IReadOnlyList<Chunk>? selectedChunks = GetSelectedChunks(text, selectionPosition, selectionLength, out string header);
            // if file is renamed and selected lines are being reset from index then the patch undoes the rename too
            if (isIndex && isRenamed && reset)
            {
                header = CorrectHeaderForRenamedFile(header);
            }

            if (body is null)
        private static string CorrectHeaderForRenamedFile(string header)
        {
            // Expected input:
            //
            // diff --git a/original.txt b/original2.txt
            // similarity index 88%
            // rename from original.txt
            // rename to original2.txt
            // index 0e05069..d4029ea 100644
            // --- a/original.txt
            // +++ b/original2.txt

            // Expected output:
            //
            // diff --git a/original2.txt b/original2.txt
            // index 0e05069..d4029ea 100644
            // --- a/original2.txt
            // +++ b/original2.txt

            string[] headerLines = header.Split(new[] { '\n' });
            string? oldNameWithPrefix = null;
            string? newName = null;
            foreach (string line in headerLines)
            {
                if (line.StartsWith("+++"))
                {
                    // Takes the "original2.txt" part from patch file line: +++ b/original2.txt
                    newName = line[6..];
                }
                else if (line.StartsWith("---"))
                {
                    // Takes the "a/original.txt" part from patch file line: --- a/original.txt
                    oldNameWithPrefix = line[4..];
                }
            }

            StringBuilder sb = new();

            for (int i = 0; i < headerLines.Length; i++)
            {
                string line = headerLines[i];
                if (line.StartsWith("--- "))
                {
                    line = $"--- a/{newName}";
                }
                else if (line.Contains($" {oldNameWithPrefix} "))
                {
                    line = line.Replace($" {oldNameWithPrefix} ", $" a/{newName} ");
                }
                else if (line.StartsWith("rename from ") || line.StartsWith("rename to ") || line.StartsWith("similarity index "))
                {
                    // Note: this logic depends on git not localizing patch file format
                    continue;
                }

                if (i != 0)
                {
                    sb.Append('\n');
                }

                sb.Append(line);
            }

            return sb.ToString();
        }
