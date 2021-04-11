using System.Collections.Generic;

namespace CodeCoverageSummary
{
    public class CodeCoverage
    {
        public string Name { get; set; }

        public double LineRate { get; set; }

        public double BranchRate { get; set; }

        public int Complexity { get; set; }
    }

    public class CodeSummary
    {
        public double LineRate { get; set; }

        public int LinesCovered { get; set; }

        public int LinesValid { get; set; }

        public double BranchRate { get; set; }

        public int BranchesCovered { get; set; }

        public int BranchesValid { get; set; }

        public List<CodeCoverage> Packages { get; set; }

        public CodeSummary()
        {
            Packages = new List<CodeCoverage>();
        }
    }
}
