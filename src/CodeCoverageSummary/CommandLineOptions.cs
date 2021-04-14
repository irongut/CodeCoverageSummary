using CommandLine;

namespace CodeCoverageSummary
{
    public class CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "Code coverage file to analyse.")]
        public string Filename { get; set; }

        [Option(shortName: 'f', longName: "format", Required = false, HelpText = "Output Format - markdown or text (default).", Default = "text")]
        public string Format { get; set; }

        [Option(shortName: 'b', longName: "badge", Required = false, HelpText = "Include a badge in the output - true / false (default).", Default = false)]
        public bool Badge { get; set; }

        [Option(shortName: 'o', longName: "output", Required = false, HelpText = "Output Type - console (default), file or both.", Default = "console")]
        public string Output { get; set; }
    }
}
