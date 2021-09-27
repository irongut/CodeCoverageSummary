using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CodeCoverageSummary
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                                 .MapResult(o =>
                                 {
                                     try
                                     {
                                         if (!File.Exists(o.Filename))
                                         {
                                             Console.WriteLine("Error: Code coverage file not found.");
                                             return -2; // error
                                         }

                                         // parse code coverage file
                                         Console.WriteLine($"Code Coverage File: {o.Filename}");
                                         CodeSummary summary = ParseTestResults(o.Filename);
                                         if (summary == null)
                                         {
                                             Console.WriteLine("Error: Parsing code coverage file.");
                                             return -2; // error
                                         }
                                         else
                                         {
                                             // generate badge
                                             string badgeUrl = o.Badge ? GenerateBadge(summary) : null;

                                             // generate output
                                             string output;
                                             string fileExt;
                                             if (o.Format.Equals("text", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 fileExt = "txt";
                                                 output = GenerateTextOutput(summary, badgeUrl);
                                             }
                                             else if (o.Format.Equals("md", StringComparison.OrdinalIgnoreCase) || o.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 fileExt = "md";
                                                 output = GenerateMarkdownOutput(summary, badgeUrl);
                                             }
                                             else
                                             {
                                                 Console.WriteLine("Error: Unknown output format.");
                                                 return -2; // error
                                             }

                                             // output
                                             if (o.Output.Equals("console", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 Console.WriteLine(output);
                                             }
                                             else if (o.Output.Equals("file", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 File.WriteAllText($"code-coverage-results.{fileExt}", output);
                                             }
                                             else if (o.Output.Equals("both", StringComparison.OrdinalIgnoreCase))
                                             {
                                                 Console.WriteLine(output);
                                                 File.WriteAllText($"code-coverage-results.{fileExt}", output);
                                             }
                                             else
                                             {
                                                 Console.WriteLine("Error: Unknown output type.");
                                                 return -2; // error
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

        private static CodeSummary ParseTestResults(string filename)
        {
            CodeSummary summary = new();
            try
            {
                string rss = File.ReadAllText(filename);
                var xdoc = XDocument.Parse(rss);

                // test coverage for solution
                var coverage = from item in xdoc.Descendants("coverage")
                               select item;

                var lineR = from item in coverage.Attributes()
                            where item.Name == "line-rate"
                            select item;
                summary.LineRate = double.Parse(lineR.First().Value);

                var linesCovered = from item in coverage.Attributes()
                                   where item.Name == "lines-covered"
                                   select item;
                summary.LinesCovered = int.Parse(linesCovered.First().Value);

                var linesValid = from item in coverage.Attributes()
                                 where item.Name == "lines-valid"
                                 select item;
                summary.LinesValid = int.Parse(linesValid.First().Value);

                var branchR = from item in coverage.Attributes()
                              where item.Name == "branch-rate"
                              select item;
                summary.BranchRate = double.Parse(branchR.First().Value);

                var branchesCovered = from item in coverage.Attributes()
                                      where item.Name == "branches-covered"
                                      select item;
                summary.BranchesCovered = int.Parse(branchesCovered.First().Value);

                var branchesValid = from item in coverage.Attributes()
                                    where item.Name == "branches-valid"
                                    select item;
                summary.BranchesValid = int.Parse(branchesValid.First().Value);

                summary.Complexity = 0;

                // test coverage for individual packages
                var packages = from item in coverage.Descendants("package")
                               select item;

                int i = 1;
                foreach (var item in packages)
                {
                    CodeCoverage packageCoverage = new()
                    {
                        Name = string.IsNullOrWhiteSpace(item.Attribute("name").Value) ? $"Package {i}" : item.Attribute("name").Value,
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
                Console.WriteLine($"Parse Error: {ex.Message}");
                return null;
            }
        }

        private static string GenerateBadge(CodeSummary summary)
        {
            string colour;
            if (summary.LineRate < 0.5)
            {
                colour = "critical";
            }
            else if (summary.LineRate < 0.75)
            {
                colour = "yellow";
            }
            else
            {
                colour = "success";
            }
            return $"https://img.shields.io/badge/Code%20Coverage-{summary.LineRate * 100:N0}%25-{colour}?style=flat";
        }

        private static string GenerateTextOutput(CodeSummary summary, string badgeUrl)
        {
            StringBuilder textOutput = new();

            if (!string.IsNullOrWhiteSpace(badgeUrl))
            {
                textOutput.AppendLine(badgeUrl);
            }

            textOutput.AppendLine($"Line Rate = {summary.LineRate * 100:N0}%, Lines Covered = {summary.LinesCovered} / {summary.LinesValid}")
                      .AppendLine($"Branch Rate = {summary.BranchRate * 100:N0}%, Branches Covered = {summary.BranchesCovered} / {summary.BranchesValid}");

            if (summary.Complexity % 1 == 0)
            {
                textOutput.AppendLine($"Complexity = {summary.Complexity}");
            }
            else
            {
                textOutput.AppendLine($"Complexity = {summary.Complexity:N4}");
            }

            foreach (CodeCoverage package in summary.Packages)
            {
                if (package.Complexity % 1 == 0)
                {
                    textOutput.AppendLine($"{package.Name}: Line Rate = {package.LineRate * 100:N0}%, Branch Rate = {package.BranchRate * 100:N0}%, Complexity = {package.Complexity}");
                }
                else
                {
                    textOutput.AppendLine($"{package.Name}: Line Rate = {package.LineRate * 100:N0}%, Branch Rate = {package.BranchRate * 100:N0}%, Complexity = {package.Complexity:N4}");
                }
            }

            return textOutput.ToString();
        }

        private static string GenerateMarkdownOutput(CodeSummary summary, string badgeUrl)
        {
            StringBuilder markdownOutput = new();

            if (!string.IsNullOrWhiteSpace(badgeUrl))
            {
                markdownOutput.AppendLine($"![Code Coverage]({badgeUrl})");
                markdownOutput.AppendLine("");
            }

            markdownOutput.AppendLine("Package | Line Rate | Branch Rate | Complexity")
                          .AppendLine("-------- | --------- | ----------- | ----------");

            foreach (CodeCoverage package in summary.Packages)
            {
                if (package.Complexity % 1 == 0)
                {
                    markdownOutput.AppendLine($"{package.Name} | {package.LineRate * 100:N0}% | {package.BranchRate * 100:N0}% | {package.Complexity}");
                }
                else
                {
                    markdownOutput.AppendLine($"{package.Name} | {package.LineRate * 100:N0}% | {package.BranchRate * 100:N0}% | {package.Complexity:N4}");
                }
            }

            markdownOutput.Append($"**Summary** | **{summary.LineRate * 100:N0}%** ({summary.LinesCovered} / {summary.LinesValid}) | ")
                          .Append($"**{summary.BranchRate * 100:N0}%** ({summary.BranchesCovered} / {summary.BranchesValid}) | ");

            if (summary.Complexity % 1 == 0)
            {
                markdownOutput.AppendLine(summary.Complexity.ToString());
            }
            else
            {
                markdownOutput.AppendLine(summary.Complexity.ToString("N4"));
            }

            return markdownOutput.ToString();
        }
    }
}
