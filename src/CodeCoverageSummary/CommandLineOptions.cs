using CommandLine;
using System;

namespace CodeCoverageSummary
{
    public class CommandLineOptions
    {
        [Value(index: 0, Required = true, HelpText = "Code coverage file to analyse.")]
        public string Filename { get; set; }

        [Option(longName: "badge", Required = false, HelpText = "Include a Line Rate coverage badge in the output using shields.io - true or false.", Default = "false")]
        public string BadgeString { get; set; }

        public bool Badge => BadgeString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "fail", Required = false, HelpText = "Fail if overall Line Rate below lower threshold - true or false.", Default = "false")]
        public string FailString { get; set; }

        public bool FailBelowThreshold => FailString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "format", Required = false, HelpText = "Output Format - markdown or text.", Default = "text")]
        public string Format { get; set; }

        [Option(longName: "indicators", Required = false, HelpText = "Include health indicators in the output - true or false.", Default = "true")]
        public string IndicatorsString { get; set; }

        public bool Indicators => IndicatorsString.Equals("true", StringComparison.OrdinalIgnoreCase);

        [Option(longName: "output", Required = false, HelpText = "Output Type - console, file or both.", Default = "console")]
        public string Output { get; set; }

        [Option(longName: "thresholds", Required = false, HelpText = "Threshold percentages for badge and health indicators, lower threshold can also be used to fail the action.", Default = "50 75")]
        public string Thresholds { get; set; }
    }
}
