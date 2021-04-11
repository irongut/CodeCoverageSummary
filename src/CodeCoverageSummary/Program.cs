using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace CodeCoverageSummary
{
    internal static class Program
    {
        private const string _filename = "D:\\Dev\\Csharp\\CodeCoverageSummary\\coverage.cobertura.xml";

        private static int Main(string[] args)
        {
            Console.WriteLine($"Parsing Code Coverage File: {_filename}{Environment.NewLine}");
            string summary = ParseTestResults(_filename);
            if (string.IsNullOrWhiteSpace(summary))
            {
                return -3;
            }
            else
            {
                Console.WriteLine(summary);
                return 0;
            }
        }

        private static string ParseTestResults(string filename)
        {
            StringBuilder summaryText = new StringBuilder();
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
                double lineRate = double.Parse(lineR.First().Value);

                var linesCovered = from item in coverage.Attributes()
                                   where item.Name == "lines-covered"
                                   select item;

                var linesValid = from item in coverage.Attributes()
                                 where item.Name == "lines-valid"
                                 select item;

                var branchR = from item in coverage.Attributes()
                              where item.Name == "branch-rate"
                              select item;
                double branchRate = double.Parse(branchR.First().Value);

                var branchesCovered = from item in coverage.Attributes()
                                      where item.Name == "branches-covered"
                                      select item;

                var branchesValid = from item in coverage.Attributes()
                                    where item.Name == "branches-valid"
                                    select item;

                summaryText.AppendLine("Code Coverage Results:")
                           .AppendLine($"Line Rate = {lineRate * 100:N0}%, Lines Covered = {linesCovered.First().Value} / {linesValid.First().Value}")
                           .AppendLine($"Branch Rate = {branchRate * 100:N0}%, Branches Covered = {branchesCovered.First().Value} / {branchesValid.First().Value}");

                // test coverage for individual packages
                var packages = from item in coverage.Descendants("package")
                               select item;

                foreach (var item in packages)
                {
                    string pName = item.Attribute("name").Value;
                    double pLineRate = double.Parse(item.Attribute("line-rate").Value);
                    double pBranchRate = double.Parse(item.Attribute("branch-rate").Value);
                    int pComplexity = int.Parse(item.Attribute("complexity").Value);
                    summaryText.AppendLine($"{pName}: Line Rate = {pLineRate * 100:N0}%, Branch Rate = {pBranchRate * 100:N0}%, Complexity = {pComplexity}");
                }

                summaryText.AppendLine();

                return summaryText.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse Error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
