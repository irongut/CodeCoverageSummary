using CommandLine;
using Microsoft.Extensions.FileSystemGlobbing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CodeCoverageSummary
{
    internal static class Program
    {
        private static double lowerThreshold = 0.5;
        private static double upperThreshold = 0.75;

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                                 .MapResult(o =>
                                 {
                                     try
                                     {
                                         // use glob patterns to match files
                                         Matcher matcher = new();
                                         matcher.AddIncludePatterns(o.Files.ToArray());
                                         IEnumerable<string> matchingFiles = matcher.GetResultsInFullPath(".");

                                         if (matchingFiles?.Any() == false)
                                         {
                                             Console.WriteLine("Error: No files found matching glob pattern.");
                                             return -2; // error
                                         }

                                         // check files exist
                                         foreach (var file in matchingFiles)
                                         {
                                             if (!File.Exists(file))
                                             {
                                                 Console.WriteLine($"Error: Coverage file not found - {file}.");
                                                 return -2; // error
                                             }
                                         }

                                         // parse code coverage file
                                         CodeSummary summary = new();
                                         foreach (var file in matchingFiles)
                                         {
                                             Console.WriteLine($"Coverage File: {file}");
                                             summary = ParseTestResults(file, summary, o.ShowClassNames);
                                         }

                                         if (summary == null)
                                             return -2; // error

                                         summary.LineRate /= matchingFiles.Count();
                                         summary.BranchRate /= matchingFiles.Count();

                                         if (summary.Packages.Count == 0)
                                         {
                                             Console.WriteLine("Parsing Error: No packages found in coverage files.");
                                             return -2; // error
                                         }
                                         else
                                         {
                                             // hide branch rate if metrics missing
                                             bool hideBranchRate = o.HideBranchRate;
                                             if (summary.BranchRate == 0 && summary.BranchesCovered == 0 && summary.BranchesValid == 0)
                                                 hideBranchRate = true;

                                             // set health badge thresholds
                                             if (!string.IsNullOrWhiteSpace(o.Thresholds))
                                                 SetThresholds(o.Thresholds);

                                             // generate badge
                                             string badgeUrl = o.Badge ? GenerateBadge(summary) : null;

                                             // generate output
                                             string output;
                                             string fileExt;
                                             if (o.Format.Equals("text", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 fileExt = "txt";
                                                 output = GenerateTextOutput(summary, badgeUrl, o.Indicators, hideBranchRate, o.HideComplexity);
                                                 if (o.FailBelowThreshold)
                                                     output += $"Minimum allowed line rate is {lowerThreshold * 100:N0}%{Environment.NewLine}";
                                             }
                                             else if (o.Format.Equals("md", StringComparison.OrdinalIgnoreCase) || o.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 fileExt = "md";
                                                 output = GenerateMarkdownOutput(summary, badgeUrl, o.Indicators, hideBranchRate, o.HideComplexity);
                                                 if (o.FailBelowThreshold)
                                                     output += $"{Environment.NewLine}_Minimum allowed line rate is `{lowerThreshold * 100:N0}%`_{Environment.NewLine}";
                                             }
                                             else
                                             {
                                                 Console.WriteLine("Error: Unknown output format.");
                                                 return -2; // error
                                             }

                                             // output
                                             if (o.Output.Equals("console", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 Console.WriteLine();
                                                 Console.WriteLine(output);
                                             }
                                             else if (o.Output.Equals("file", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 File.WriteAllText($"code-coverage-results.{fileExt}", output);
                                             }
                                             else if (o.Output.Equals("both", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 Console.WriteLine();
                                                 Console.WriteLine(output);
                                                 File.WriteAllText($"code-coverage-results.{fileExt}", output);
                                             }
                                             else
                                             {
                                                 Console.WriteLine("Error: Unknown output type.");
                                                 return -2; // error
                                             }

                                             if (o.FailBelowThreshold && summary.LineRate < lowerThreshold)
                                             {
                                                 Console.WriteLine($"FAIL: Overall line rate below minimum threshold of {lowerThreshold * 100:N0}%.");
                                                 return -2;
                                             }

                                             return 0; // success
                                         }
                                     }
                                     catch (Exception ex)
                                     {
                                         Console.WriteLine($"Error: {ex.GetType()} - {ex.Message}");
                                         return -3; // unhandled error
                                     }
                                 },
                                 _ => -1); // invalid arguments
        }

        private static CodeSummary ParseTestResults(string filename, CodeSummary summary, bool showClassNames)
        {
            if (summary == null)
                return null;

            try
            {
                string rss = File.ReadAllText(filename);
                var xdoc = XDocument.Parse(rss);

                // test coverage for solution
                var coverage = from item in xdoc.Descendants("coverage")
                               select item;

                if (!coverage.Any())
                    throw new Exception("Coverage file invalid, data not found");

                var lineR = from item in coverage.Attributes()
                            where item.Name == "line-rate"
                            select item;

                if (!lineR.Any())
                    throw new Exception("Overall line rate not found");

                summary.LineRate += double.Parse(lineR.First().Value);

                var linesCovered = from item in coverage.Attributes()
                                   where item.Name == "lines-covered"
                                   select item;

                if (!linesCovered.Any())
                    throw new Exception("Overall lines covered not found");

                summary.LinesCovered += int.Parse(linesCovered.First().Value);

                var linesValid = from item in coverage.Attributes()
                                 where item.Name == "lines-valid"
                                 select item;

                if (!linesValid.Any())
                    throw new Exception("Overall lines valid not found");

                summary.LinesValid += int.Parse(linesValid.First().Value);

                var branchR = from item in coverage.Attributes()
                              where item.Name == "branch-rate"
                              select item;

                if (branchR.Any())
                {
                    summary.BranchRate += double.Parse(branchR.First().Value);

                    var branchesCovered = from item in coverage.Attributes()
                                          where item.Name == "branches-covered"
                                          select item;

                    if (branchesCovered.Any())
                        summary.BranchesCovered += int.Parse(branchesCovered.First().Value);

                    var branchesValid = from item in coverage.Attributes()
                                        where item.Name == "branches-valid"
                                        select item;

                    if (branchesValid.Any())
                        summary.BranchesValid += int.Parse(branchesValid.First().Value);
                }

                // test coverage for individual packages
                var packages = from item in coverage.Descendants("package")
                           select item;

                if (!showClassNames)
                    packages = from item in coverage.Descendants("package").Descendants("class")
                               select item;

                if (!packages.Any())
                    throw new Exception("No package data found");

                int i = 1;
                foreach (var item in packages)
                {
                    CodeCoverage packageCoverage = new()
                    {
                        Name = string.IsNullOrWhiteSpace(item.Attribute("name")?.Value) ? $"{Path.GetFileNameWithoutExtension(filename)} Package {i}" : item.Attribute("name").Value,
                        LineRate = double.Parse(item.Attribute("line-rate")?.Value ?? "0"),
                        BranchRate = double.Parse(item.Attribute("branch-rate")?.Value ?? "0"),
                        Complexity = double.Parse(item.Attribute("complexity")?.Value ?? "0")
                    };
                    summary.Packages.Add(packageCoverage);
                    summary.Complexity += packageCoverage.Complexity;
                    i++;
                }

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parsing Error: {ex.Message} - {filename}");
                return null;
            }
        }

        private static void SetThresholds(string thresholds)
        {
            int lowerPercentage;
            int upperPercentage = (int)(upperThreshold * 100);
            int s = thresholds.IndexOf(" ");
            if (s == 0)
            {
                throw new ArgumentException("Threshold parameter set incorrectly.");
            }
            else if (s < 0)
            {
                if (!int.TryParse(thresholds, out lowerPercentage))
                    throw new ArgumentException("Threshold parameter set incorrectly.");
            }
            else
            {
                if (!int.TryParse(thresholds.AsSpan(0, s), out lowerPercentage))
                    throw new ArgumentException("Threshold parameter set incorrectly.");

                if (!int.TryParse(thresholds.AsSpan(s + 1), out upperPercentage))
                    throw new ArgumentException("Threshold parameter set incorrectly.");
            }
            lowerThreshold = lowerPercentage / 100.0;
            upperThreshold = upperPercentage / 100.0;

            if (lowerThreshold > 1.0)
                lowerThreshold = 1.0;

            if (lowerThreshold > upperThreshold)
                upperThreshold = lowerThreshold + 0.1;

            if (upperThreshold > 1.0)
                upperThreshold = 1.0;
        }

        private static string GenerateBadge(CodeSummary summary)
        {
            string colour;
            if (summary.LineRate < lowerThreshold)
            {
                colour = "critical";
            }
            else if (summary.LineRate < upperThreshold)
            {
                colour = "yellow";
            }
            else
            {
                colour = "success";
            }
            return $"https://img.shields.io/badge/Code%20Coverage-{summary.LineRate * 100:N0}%25-{colour}?style=flat";
        }

        private static string GenerateHealthIndicator(double rate)
        {
            if (rate < lowerThreshold)
            {
                return "❌";
            }
            else if (rate < upperThreshold)
            {
                return "➖";
            }
            else
            {
                return "✔";
            }
        }

        private static string GenerateTextOutput(CodeSummary summary, string badgeUrl, bool indicators, bool hideBranchRate, bool hideComplexity)
        {
            StringBuilder textOutput = new();

            if (!string.IsNullOrWhiteSpace(badgeUrl))
            {
                textOutput.AppendLine(badgeUrl)
                          .AppendLine();
            }

            foreach (CodeCoverage package in summary.Packages)
            {
                textOutput.Append($"{package.Name}: Line Rate = {package.LineRate * 100:N0}%")
                          .Append(hideBranchRate ? string.Empty : $", Branch Rate = {package.BranchRate * 100:N0}%")
                          .Append(hideComplexity ? string.Empty : (package.Complexity % 1 == 0) ? $", Complexity = {package.Complexity}" : $", Complexity = {package.Complexity:N4}")
                          .AppendLine(indicators ? $", {GenerateHealthIndicator(package.LineRate)}" : string.Empty);
            }

            textOutput.Append($"Summary: Line Rate = {summary.LineRate * 100:N0}% ({summary.LinesCovered} / {summary.LinesValid})")
                      .Append(hideBranchRate ? string.Empty : $", Branch Rate = {summary.BranchRate * 100:N0}% ({summary.BranchesCovered} / {summary.BranchesValid})")
                      .Append(hideComplexity ? string.Empty : (summary.Complexity % 1 == 0) ? $", Complexity = {summary.Complexity}" : $", Complexity = {summary.Complexity:N4}")
                      .AppendLine(indicators ? $", {GenerateHealthIndicator(summary.LineRate)}" : string.Empty);

            return textOutput.ToString();
        }

        private static string GenerateMarkdownOutput(CodeSummary summary, string badgeUrl, bool indicators, bool hideBranchRate, bool hideComplexity)
        {
            StringBuilder markdownOutput = new();

            if (!string.IsNullOrWhiteSpace(badgeUrl))
            {
                markdownOutput.AppendLine($"![Code Coverage]({badgeUrl})")
                              .AppendLine();
            }

            markdownOutput.Append("Package | Line Rate")
                          .Append(hideBranchRate ? string.Empty : " | Branch Rate")
                          .Append(hideComplexity ? string.Empty : " | Complexity")
                          .AppendLine(indicators ? " | Health" : string.Empty)
                          .Append("-------- | ---------")
                          .Append(hideBranchRate ? string.Empty : " | -----------")
                          .Append(hideComplexity ? string.Empty : " | ----------")
                          .AppendLine(indicators ? " | ------" : string.Empty);

            foreach (CodeCoverage package in summary.Packages)
            {
                markdownOutput.Append($"{package.Name} | {package.LineRate * 100:N0}%")
                              .Append(hideBranchRate ? string.Empty : $" | {package.BranchRate * 100:N0}%")
                              .Append(hideComplexity ? string.Empty : (package.Complexity % 1 == 0) ? $" | {package.Complexity}" : $" | {package.Complexity:N4}" )
                              .AppendLine(indicators ? $" | {GenerateHealthIndicator(package.LineRate)}" : string.Empty);
            }

            markdownOutput.Append($"**Summary** | **{summary.LineRate * 100:N0}%** ({summary.LinesCovered} / {summary.LinesValid})")
                          .Append(hideBranchRate ? string.Empty : $" | **{summary.BranchRate * 100:N0}%** ({summary.BranchesCovered} / {summary.BranchesValid})")
                          .Append(hideComplexity ? string.Empty : (summary.Complexity % 1 == 0) ? $" | **{summary.Complexity}**" : $" | **{summary.Complexity:N4}**")
                          .AppendLine(indicators ? $" | {GenerateHealthIndicator(summary.LineRate)}" : string.Empty);

            return markdownOutput.ToString();
        }
    }
}
