using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class DiagnosisResult
    {
        public Disease Disease { get; set; }
        public int MatchCount { get; set; }
        public int TotalSymptoms { get; set; }
        public string MatchInfo => $"Збіг симптомів: {MatchCount}/{TotalSymptoms}";
        public double Probability => TotalSymptoms == 0 ? 0 : (double)MatchCount / TotalSymptoms;
    }
}
