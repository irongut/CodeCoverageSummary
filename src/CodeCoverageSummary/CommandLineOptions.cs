using CommandLine;
using System;
using System.Collections.Generic;

namespace CodeCoverageSummary
{
    public class CommandLineOptions
    {
        [Option(longName: "files", Separator = ',', Required = true, HelpText = "A comma separated list of code coverage files to analyse.")]
        public IEnumerable<string> Files { get; set; }

        [Option(longName: "badge", Required = false, HelpText = "Include a Line Rate coverage badge in the output using shields.io - true or false.", Default = "false")]
        public string BadgeString { get; set; }

        public bool Badge => BadgeString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "fail", Required = false, HelpText = "Fail if overall Line Rate below lower threshold - true or false.", Default = "false")]
        public string FailString { get; set; }

        public bool FailBelowThreshold => FailString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "format", Required = false, HelpText = "Output Format - markdown or text.", Default = "text")]
        public string Format { get; set; }

        [Option(longName: "hidebranch", Required = false, HelpText = "Hide Branch Rate values in the output - true or false.", Default = "false")]
        public string HideBranchString { get; set; }

        public bool HideBranchRate => HideBranchString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "hidecomplexity", Required = false, HelpText = "Hide Complexity values in the output - true or false.", Default = "false")]
        public string HideComplexityString { get; set; }

        public bool HideComplexity => HideComplexityString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "showclassnames", Required = false, HelpText = "Show individual class detail in the output - true or false.", Default = "true")]
        public string ShowClassNamesString { get; set; }
        public bool ShowClassNames => ShowClassNamesString.Equals("false", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "indicators", Required = false, HelpText = "Include health indicators in the output - true or false.", Default = "true")]
        public string IndicatorsString { get; set; }

        public bool Indicators => IndicatorsString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "output", Required = false, HelpText = "Output Type - console, file or both.", Default = "console")]
        public string Output { get; set; }

        [Option(longName: "thresholds", Required = false, HelpText = "Threshold percentages for badge and health indicators, lower threshold can also be used to fail the action.", Default = "50 75")]
        public string Thresholds { get; set; }
    }
}
