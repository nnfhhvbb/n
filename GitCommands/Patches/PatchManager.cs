        public static byte[]? GetResetWorkTreeLinesAsPatch(GitModule module, string text, int selectionPosition, int selectionLength, Encoding fileContentEncoding)
            if (selectedChunks is null)
            string? body = ToResetWorkTreeLinesPatch(selectedChunks);
            if (header is null || body is null)
        public static byte[]? GetSelectedLinesAsPatch(string text, int selectionPosition, int selectionLength, bool isIndex, Encoding fileContentEncoding, bool isNewFile)
            if (selectedChunks is null || header is null)
            string? body = ToIndexPatch(selectedChunks, isIndex, isWholeFile: false);
            if (header is null || body is null)
        private static string CorrectHeaderForNewFile(string header)
            string? pppLine = null;
            StringBuilder sb = new();
        public static byte[]? GetSelectedLinesAsNewPatch(GitModule module, string newFileName, string text, int selectionPosition, int selectionLength, Encoding fileContentEncoding, bool reset, byte[] filePreamble, string? treeGuid)
            var isTracked = treeGuid is not null;
            string? body = ToIndexPatch(selectedChunks, isIndex: isTracked, isWholeFile: true);
            if (body is null)
            StringBuilder header = new();
        private static byte[] GetPatchBytes(string header, string body, Encoding fileContentEncoding)
        private static IReadOnlyList<Chunk>? GetSelectedChunks(string text, int selectionPosition, int selectionLength, out string? header)
            List<Chunk> selectedChunks = new();
                    Chunk? chunk = Chunk.ParseChunk(chunkStr, currentPos, selectionPosition, selectionLength);
                    if (chunk is not null)
        private static IReadOnlyList<Chunk> FromNewFile(GitModule module, string text, int selectionPosition, int selectionLength, bool reset, byte[] filePreamble, Encoding fileContentEncoding)
        private static string? ToResetWorkTreeLinesPatch(IEnumerable<Chunk> chunks)
            static string? SubChunkToPatch(SubChunk subChunk, ref int addedCount, ref int removedCount, ref bool wereSelectedLines)
        private static string? ToIndexPatch(IEnumerable<Chunk> chunks, bool isIndex, bool isWholeFile)
            string? SubChunkToPatch(SubChunk subChunk, ref int addedCount, ref int removedCount, ref bool wereSelectedLines)
        private static string? ToPatch(IEnumerable<Chunk> chunks, [InstantHandle] SubChunkToPatchFnc subChunkToPatch)
            StringBuilder result = new();
        public string? WasNoNewLineAtTheEnd { get; set; }
        public string? IsNoNewLineAtTheEnd { get; set; }
        public string? ToIndexPatch(ref int addedCount, ref int removedCount, ref bool wereSelectedLines, bool isIndex, bool isWholeFile)
            string? diff = null;
            string? removePart = null;
            string? addPart = null;
            string? prePart = null;
            string? postPart = null;
            diff = PostContext.Count switch
                // stage no new line at the end only if last +- line is selected
                0 when selectedLastAddedLine || isIndex || isWholeFile => diff.Combine("\n", IsNoNewLineAtTheEnd),
                > 0 => diff.Combine("\n", WasNoNewLineAtTheEnd),
                _ => diff
            };
        public string? ToResetWorkTreeLinesPatch(ref int addedCount, ref int removedCount, ref bool wereSelectedLines)
            string? diff = null;
            string? removePart = null;
            string? addPart = null;
            string? prePart = null;
            string? postPart = null;
    internal delegate string? SubChunkToPatchFnc(SubChunk subChunk, ref int addedCount, ref int removedCount, ref bool wereSelectedLines);
        private readonly List<SubChunk> _subChunks = new();
        private SubChunk? _currentSubChunk;
                if (_currentSubChunk is null)
        private void AddContextLine(PatchLine line, bool preContext)
        private void AddDiffLine(PatchLine line, bool removed)
        private void ParseHeader(string header)
        public static Chunk? ParseChunk(string chunkStr, int currentPos, int selectionPosition, int selectionLength)
            string[] lines = chunkStr.Split(Delimiters.LineFeed);
            Chunk result = new();
                    PatchLine patchLine = new(line);
        public static Chunk FromNewFile(GitModule module, string fileText, int selectionPosition, int selectionLength, bool reset, byte[] filePreamble, Encoding fileContentEncoding)
            Chunk result = new() { _startLine = 0 };
            string eol = gitEol switch
                "crlf" => "\r\n",
                "native" => Environment.NewLine,
                _ => "\n"
            };
                PatchLine patchLine = new((reset ? "-" : "+") + preamble + line);
                            PatchLine? lastNotSelectedLine = result.CurrentSubChunk.RemovedLines.LastOrDefault(l => !l.Selected);
                            if (lastNotSelectedLine is not null)
            StringBuilder diff = new();