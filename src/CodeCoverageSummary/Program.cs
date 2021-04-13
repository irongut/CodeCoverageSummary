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
        // test file: /Dev/Csharp/CodeCoverageSummary/coverage.cobertura.xml

        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CommandLineOptions>(args)
                                       .MapResult(o =>
                                       {
                                           try
                                           {
                                               Console.WriteLine($"Code Coverage File: {o.Filename}");
                                               CodeSummary summary = ParseTestResults(o.Filename);
                                               if (summary == null)
                                               {
                                                   return -2; // error
                                               }
                                               else
                                               {
                                                   string badgeUrl = null;

                                                   if (o.Badge)
                                                   {
                                                       string colour;
                                                       if (summary.LineRate < 0.5)
                                                       {
                                                           colour = "critical";
                                                       }
                                                       else if (summary.LineRate < 0.7)
                                                       {
                                                           colour = "yellow";
                                                       }
                                                       else
                                                       {
                                                           colour = "success";
                                                       }
                                                       badgeUrl = $"https://img.shields.io/badge/Code%20Coverage-{summary.LineRate * 100:N0}%25-{colour}?style=flat";
                                                   }

                                                   StringBuilder summaryText = new();
                                                   if (o.Format.Equals("text", StringComparison.OrdinalIgnoreCase))
                                                   {
                                                       if (string.IsNullOrWhiteSpace(badgeUrl))
                                                       {
                                                           summaryText.AppendLine(badgeUrl);
                                                       }

                                                       summaryText.AppendLine($"Line Rate = {summary.LineRate * 100:N0}%, Lines Covered = {summary.LinesCovered} / {summary.LinesValid}")
                                                                  .AppendLine($"Branch Rate = {summary.BranchRate * 100:N0}%, Branches Covered = {summary.BranchesCovered} / {summary.BranchesValid}")
                                                                  .AppendLine($"Complexity = {summary.Complexity}");

                                                       foreach (CodeCoverage package in summary.Packages)
                                                       {
                                                           summaryText.AppendLine($"{package.Name}: Line Rate = {package.LineRate * 100:N0}%, Branch Rate = {package.BranchRate * 100:N0}%, Complexity = {package.Complexity}");
                                                       }
                                                   }
                                                   else if (o.Format.Equals("md", StringComparison.OrdinalIgnoreCase) || o.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
                                                   {
                                                       if (!string.IsNullOrWhiteSpace(badgeUrl))
                                                       {
                                                           summaryText.AppendLine($"![Code Coverage]({badgeUrl})");
                                                       }

                                                       summaryText.AppendLine("Package | Line Rate | Branch Rate | Complexity")
                                                                  .AppendLine("-------- | --------- | ----------- | ----------");

                                                       foreach (CodeCoverage package in summary.Packages)
                                                       {
                                                           summaryText.AppendLine($"{package.Name} | {package.LineRate * 100:N0}% | {package.BranchRate * 100:N0}% | {package.Complexity}");
                                                       }

                                                       summaryText.Append($"**Summary** | **{summary.LineRate * 100:N0}%** ({summary.LinesCovered} / {summary.LinesValid}) | ")
                                                                  .AppendLine($"**{summary.BranchRate * 100:N0}%** ({summary.BranchesCovered} / {summary.BranchesValid}) | {summary.Complexity}");
                                                   }
                                                   else
                                                   {
                                                       Console.WriteLine("Error: Unknown output format.");
                                                       return -2; // error
                                                   }

                                                   Console.WriteLine(summaryText.ToString());
                                                   return 0; // success
                                               }
                                           }
                                           catch
                                           {
                                               return -3; // unhandled error
                                           }
                                       },
                                       errs => -1); // invalid arguments
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

                foreach (var item in packages)
                {
                    CodeCoverage packageCoverage = new()
                    {
                        Name = item.Attribute("name").Value,
                        LineRate = double.Parse(item.Attribute("line-rate").Value),
                        BranchRate = double.Parse(item.Attribute("branch-rate").Value),
                        Complexity = int.Parse(item.Attribute("complexity").Value)
                    };
                    summary.Packages.Add(packageCoverage);
                    summary.Complexity += packageCoverage.Complexity;
                }

                return summary;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                return null;
            }
        }
    }
}
