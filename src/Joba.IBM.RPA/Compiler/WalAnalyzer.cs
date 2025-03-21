﻿using System.Text.RegularExpressions;

namespace Joba.IBM.RPA
{
    internal sealed partial class WalAnalyzer
    {
        private readonly WalParser parser = new();

        internal WalAnalyzer(WalFile wal) : this(wal.Content) { }
        internal WalAnalyzer(WalContent content) => Lines = parser.Parse(content);

        internal WalLines Lines { get; }

        internal IEnumerable<WalLine> EnumerateCommands(string commandName) =>
            Lines.Where(l => (l.Command?.Equals(commandName, StringComparison.InvariantCultureIgnoreCase)).GetValueOrDefault());

        internal IEnumerable<TCommand> EnumerateCommands<TCommand>(string commandName) where TCommand : WalLine =>
            EnumerateCommands(commandName).Cast<TCommand>();

        class WalParser
        {
            private static readonly IDictionary<string, Type> commandsMapping;

            static WalParser()
            {
                commandsMapping = new Dictionary<string, Type>
                {
                    { ExecuteScriptLine.Verb, typeof(ExecuteScriptLine) },
                    { DefineVariableLine.Verb, typeof(DefineVariableLine) },
                    { ImportLine.Verb, typeof(ImportLine) },
                    { GoSubLine.Verb, typeof(GoSubLine) },
                    { BeginSubLine.Verb, typeof(BeginSubLine) },
                    { SetVarIfLine.Verb, typeof(SetVarIfLine) },
                    { ExcelOpenLine.Verb, typeof(ExcelOpenLine) }
                };
            }

            public WalLines Parse(WalContent wal) => new(ParseInternal(wal).ToList());

            private static IEnumerable<WalLine> ParseInternal(WalContent wal)
            {
                var lines = Regex.Split(wal.ToString(), "\r?\n");
                for (var index = 0; index < lines.Length; index++)
                {
                    var lineNumber = index + 1;
                    var line = lines[index];
                    var match = Regex.Match(line, "^\\s*(?<verb>[a-z]+)\\s?", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        var verb = match.Groups["verb"].Value;
                        if (commandsMapping.TryGetValue(verb, out var type))
                            yield return (WalLine)Activator.CreateInstance(type, new object[] { lineNumber, line, verb })!;
                        else
                            yield return new WalLine(lineNumber, line, null);
                    }
                    else
                        yield return new WalLine(lineNumber, line, null);
                }
            }
        }
    }

    internal class DefineVariableLine : WalLine
    {
        public const string Verb = "defVar";

        public DefineVariableLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, @"--name\s+(?<name>\w+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--name' parameter in the correct format.");
            Name = match.Groups["name"]?.Value;

            match = Regex.Match(content, @"--type\s+(?<type>\w+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--type' parameter in the correct format.");
            Type = match.Groups["type"]?.Value;
        }

        internal string Name { get; }
        internal string Type { get; }
    }

    internal class GoSubLine : WalLine
    {
        public const string Verb = "goSub";

        public GoSubLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, "--label\\s+(?<label>(\\w+)|\"(.*?)\")", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--label' parameter in the correct format.");
            Label = match.Groups["label"]?.Value;
        }

        internal string Label { get; }
    }

    internal class BeginSubLine : WalLine
    {
        public const string Verb = "beginSub";

        public BeginSubLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, @"--name\s+(?<name>\w+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--name' parameter in the correct format.");
            Name = match.Groups["name"]?.Value;
        }

        internal string Name { get; }
    }

    internal class SetVarIfLine : WalLine
    {
        public const string Verb = "setVarIf";

        public SetVarIfLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, "--variablename\\s+\"(?<name>.*?)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--variablename' parameter in the correct format.");
            Name = match.Groups["name"]?.Value;

            match = Regex.Match(content, "--value\\s+(?<value>(\\w+)|\"(.*?)\")", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--value' parameter in the correct format.");
            Value = match.Groups["value"]?.Value;
        }

        internal string Name { get; }
        internal string Value { get; }
    }

    internal class ImportLine : WalLine
    {
        public const string Verb = "import";

        public ImportLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, @"--name\s+(?<name>\w+)", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--name' parameter in the correct format.");
            Name = match.Groups["name"]?.Value;

            match = Regex.Match(content, "--type\\s+\"(?<type>\\w+)\"", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--type' parameter in the correct format.");
            Type = match.Groups["type"]?.Value;

            match = Regex.Match(content, @"--content\s+(?<content>([^\s]+))", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--content' parameter in the correct format.");
            Base64Content = match.Groups["content"]?.Value;
        }

        internal string Name { get; }
        internal string Type { get; }
        internal string Base64Content { get; }
    }

    internal class ExcelOpenLine : ReferenceableWalLine
    {
        public const string Verb = "excelOpen";
        private readonly string[] referenceParameterNames = new string[] { "--file" };

        public ExcelOpenLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, @"--file\s+""(?<file>.*?)""", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--file' parameter in the correct format.");

            File = match.Groups["file"]?.Value;
        }

        internal override string[] ReferenceParameterNames => referenceParameterNames;
        internal string File { get; }

        internal override string? GetRelativePath(DirectoryInfo workingDir)
        {
            /*
             * NOTE: IBM RPA version 23.0.x does not have 'working directory' concept,
             *       but this tool is an OPINIONATED version of how PROJECTS should work.
             * We expect ALL 'executeScript' references to be on the following format:
             *   ${[working_directory_variable]}\[path_of_the_wal_file_within_working_directory]
             * Examples
             *   - ${workingDir}\myscript.wal
             *   - ${var1}\packages\package1.wal
             *   - ${folder}\math\sum.wal
             * Not allowed (we're going to return NULL for those, so we skip adding them to the build package)
             *   - ${workingDir}\${var1}\myscript.wal
             *   - ${scriptName}
             *   - c:\myscript.wal
             *   - myscript
             *   - myscript.wal
             *   
             * We need to find the 'relative path' to the 'working directory'.
             * Let's replace the first (and only first) variable --> ${workingDir}\\ <-- with empty strings.
             */

            var name = File.Replace(@"\\", @"\");
            if (Path.IsPathFullyQualified(name))
                return null;

            var path = StartsWithVariableAndDirectorySeparator().Replace(name, string.Empty).TrimStart('\\');
            if (ContainsVariable().IsMatch(path))
                return null;

            /*
             * NOTE: at this point, we should have gone from 
             *   - "${workingDir}\sum.wal" -> "sum.wal"
             *   - "${workingDir}\packages\sum.wal" -> "packages\sum.wal"
             */

            return path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }

    internal class ExecuteScriptLine : ReferenceableWalLine
    {
        public const string Verb = "executeScript";
        private readonly bool isServerReference;
        private readonly string[] referenceParameterNames = new string[] { "--name" };

        public ExecuteScriptLine(int number, string content, string? command)
            : base(number, content, command)
        {
            var match = Regex.Match(content, @"--name\s+(""(?<localScriptName>.*?)""|(?<publishedScriptName>\w+))", RegexOptions.IgnoreCase);
            if (!match.Success)
                throw new Exception($"Line {number} ({Verb}) does not have the '--name' parameter in the correct format.");

            var local = match.Groups["localScriptName"]?.Value;
            var published = match.Groups["publishedScriptName"]?.Value;

            if (string.IsNullOrEmpty(local) && string.IsNullOrEmpty(published))
                throw new Exception($"Could not parse '--name' parameter at line '{number}' ({Verb}).");

            Name = !string.IsNullOrEmpty(local) ? local : published;
            isServerReference = string.IsNullOrEmpty(published) == false;
        }

        internal override string[] ReferenceParameterNames => referenceParameterNames;
        internal string Name { get; }

        internal override string? GetRelativePath(DirectoryInfo workingDir)
        {
            /*
             * NOTE: IBM RPA version 23.0.x does not have 'working directory' concept,
             *       but this tool is an OPINIONATED version of how PROJECTS should work.
             * We expect ALL 'executeScript' references to be on the following format:
             *   ${[working_directory_variable]}\[path_of_the_wal_file_within_working_directory]
             * Examples
             *   - ${workingDir}\myscript.wal
             *   - ${var1}\packages\package1.wal
             *   - ${folder}\math\sum.wal
             * Not allowed (we're going to return NULL for those, so we skip adding them to the build package)
             *   - ${workingDir}\${var1}\myscript.wal
             *   - ${scriptName}
             *   - c:\myscript.wal
             *   - myscript
             *   - myscript.wal
             *   
             * We need to find the 'relative path' to the 'working directory'.
             * Let's replace the first (and only first) variable --> ${workingDir}\\ <-- with empty strings.
             */

            var name = Name.Replace(@"\\", @"\");
            if (isServerReference || Path.IsPathFullyQualified(name))
                return null;

            var path = StartsWithVariableAndDirectorySeparator().Replace(name, string.Empty).TrimStart('\\');
            if (ContainsVariable().IsMatch(path))
                return null;

            /*
             * NOTE: at this point, we should have gone from 
             *   - "${workingDir}\sum.wal" -> "sum.wal"
             *   - "${workingDir}\packages\sum.wal" -> "packages\sum.wal"
             */

            return path.Replace('\\', Path.DirectorySeparatorChar);
        }
    }

    internal class WalLines : IEnumerable<WalLine>
    {
        private readonly IList<WalLine> lines;

        internal WalLines(IList<WalLine> lines)
        {
            this.lines = lines;
        }

        public int Count => lines.Count;
        public WalLine this[int index] => lines[index];

        public IEnumerator<WalLine> GetEnumerator() => lines.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)lines).GetEnumerator();
    }

    internal abstract partial class ReferenceableWalLine : WalLine
    {
        internal ReferenceableWalLine(int number, string content, string? command) : base(number, content, command) { }
        internal abstract string[] ReferenceParameterNames { get; }
        internal abstract string? GetRelativePath(DirectoryInfo workingDir);

        [GeneratedRegex(@"^\$\{[a-z_0-9]+\}(?=\\)", RegexOptions.IgnoreCase)]
        protected static partial Regex StartsWithVariableAndDirectorySeparator();
        [GeneratedRegex(@"\$\{[a-z_0-9]+\}", RegexOptions.IgnoreCase)]
        protected static partial Regex ContainsVariable();
    }

    internal class WalLine
    {
        internal WalLine(int number, string content, string? command)
        {
            LineNumber = number;
            Content = content;
            Command = command;
        }

        public int LineNumber { get; }
        public string Content { get; private set; }
        public string? Command { get; }

        public override string ToString() => Content;
    }
}
