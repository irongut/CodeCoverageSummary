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

                                         // parse code coverage files, merging coverage for
                                         // packages/classes that appear in more than one file
                                         // (e.g. separate unit test and functional test coverage runs)
                                         Dictionary<string, PackageMerge> merge = new();
                                         foreach (var file in matchingFiles)
                                         {
                                             Console.WriteLine($"Coverage File: {file}");
                                             if (!ParseTestResults(file, merge))
                                                 return -2; // error
                                         }

                                         CodeSummary summary = BuildSummary(merge);

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

        // per-line coverage, keyed by line number, merged across coverage files
        private sealed class LineMerge
        {
            public bool Covered { get; set; }

            public int BranchesCovered { get; set; }

            public int BranchesValid { get; set; }
        }

        private sealed class ClassMerge
        {
            public double Complexity { get; set; }

            public Dictionary<int, LineMerge> Lines { get; } = new();
        }

        private sealed class PackageMerge
        {
            public Dictionary<string, ClassMerge> Classes { get; } = new();
        }

        private static bool ParseTestResults(string filename, Dictionary<string, PackageMerge> merge)
        {
            try
            {
                string rss = File.ReadAllText(filename);
                var xdoc = XDocument.Parse(rss);

                // test coverage for solution
                var coverage = xdoc.Descendants("coverage");

                if (!coverage.Any())
                    throw new Exception("Coverage file invalid, data not found");

                // test coverage for individual packages
                var packages = coverage.Descendants("package");

                if (!packages.Any())
                    throw new Exception("No package data found");

                int packageIndex = 1;
                foreach (var packageElement in packages)
                {
                    string packageName = string.IsNullOrWhiteSpace(packageElement.Attribute("name")?.Value)
                        ? $"{Path.GetFileNameWithoutExtension(filename)} Package {packageIndex}"
                        : packageElement.Attribute("name").Value;

                    if (!merge.TryGetValue(packageName, out PackageMerge packageMerge))
                    {
                        packageMerge = new PackageMerge();
                        merge[packageName] = packageMerge;
                    }

                    var classes = packageElement.Descendants("class");

                    if (!classes.Any())
                        throw new Exception("No class data found");

                    int classIndex = 1;
                    foreach (var classElement in classes)
                    {
                        string className = string.IsNullOrWhiteSpace(classElement.Attribute("name")?.Value)
                            ? $"{Path.GetFileNameWithoutExtension(filename)} Class {classIndex}"
                            : classElement.Attribute("name").Value;

                        if (!packageMerge.Classes.TryGetValue(className, out ClassMerge classMerge))
                        {
                            classMerge = new ClassMerge();
                            packageMerge.Classes[className] = classMerge;
                        }

                        classMerge.Complexity = Math.Max(classMerge.Complexity,
                            double.TryParse(classElement.Attribute("complexity")?.Value ?? "0", out double complex) ? complex : 0);

                        foreach (var lineElement in classElement.Element("lines")?.Elements("line") ?? Enumerable.Empty<XElement>())
                        {
                            if (!int.TryParse(lineElement.Attribute("number")?.Value, out int lineNumber))
                                continue;

                            int hits = int.TryParse(lineElement.Attribute("hits")?.Value, out int h) ? h : 0;
                            (int branchesCovered, int branchesValid) = ParseConditionCoverage(lineElement.Attribute("condition-coverage")?.Value);

                            if (!classMerge.Lines.TryGetValue(lineNumber, out LineMerge lineMerge))
                            {
                                lineMerge = new LineMerge();
                                classMerge.Lines[lineNumber] = lineMerge;
                            }

                            lineMerge.Covered = lineMerge.Covered || hits > 0;
                            lineMerge.BranchesValid = Math.Max(lineMerge.BranchesValid, branchesValid);
                            lineMerge.BranchesCovered = Math.Max(lineMerge.BranchesCovered, branchesCovered);
                        }

                        classIndex++;
                    }

                    packageIndex++;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parsing Error: {ex.Message} - {filename}");
                return false;
            }
        }

        private static (int covered, int valid) ParseConditionCoverage(string conditionCoverage)
        {
            // format: "50% (1/2)"
            if (string.IsNullOrWhiteSpace(conditionCoverage))
                return (0, 0);

            int open = conditionCoverage.IndexOf('(');
            int slash = conditionCoverage.IndexOf('/');
            int close = conditionCoverage.IndexOf(')');

            if (open < 0 || slash < 0 || close < 0 || slash < open || close < slash)
                return (0, 0);

            bool coveredOk = int.TryParse(conditionCoverage.AsSpan(open + 1, slash - open - 1), out int covered);
            bool validOk = int.TryParse(conditionCoverage.AsSpan(slash + 1, close - slash - 1), out int valid);

            return coveredOk && validOk ? (covered, valid) : (0, 0);
        }

        private static CodeSummary BuildSummary(Dictionary<string, PackageMerge> merge)
        {
            CodeSummary summary = new();

            foreach (var (packageName, packageMerge) in merge)
            {
                int packageLinesCovered = 0, packageLinesValid = 0;
                int packageBranchesCovered = 0, packageBranchesValid = 0;
                double packageComplexity = 0;

                foreach (var classMerge in packageMerge.Classes.Values)
                {
                    packageLinesValid += classMerge.Lines.Count;
                    packageLinesCovered += classMerge.Lines.Values.Count(l => l.Covered);
                    packageBranchesValid += classMerge.Lines.Values.Sum(l => l.BranchesValid);
                    packageBranchesCovered += classMerge.Lines.Values.Sum(l => l.BranchesCovered);
                    packageComplexity += classMerge.Complexity;
                }

                double packageLineRate = packageLinesValid == 0 ? 0 : (double)packageLinesCovered / packageLinesValid;
                double packageBranchRate = packageBranchesValid == 0 ? 0 : (double)packageBranchesCovered / packageBranchesValid;

                summary.Packages.Add(new CodeCoverage
                {
                    Name = packageName,
                    LineRate = packageLineRate,
                    BranchRate = packageBranchRate,
                    Complexity = packageComplexity
                });

                summary.LinesCovered += packageLinesCovered;
                summary.LinesValid += packageLinesValid;
                summary.BranchesCovered += packageBranchesCovered;
                summary.BranchesValid += packageBranchesValid;
                summary.Complexity += packageComplexity;
            }

            summary.LineRate = summary.LinesValid == 0 ? 0 : (double)summary.LinesCovered / summary.LinesValid;
            summary.BranchRate = summary.BranchesValid == 0 ? 0 : (double)summary.BranchesCovered / summary.BranchesValid;

            return summary;
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
