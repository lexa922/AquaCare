using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class CompatibilityRule
    {
        public int CompatibilityRuleId {  get; set; }
        public int Species1Id { get; set; }
        public int Species2Id { get; set; }
        public int CompatabilityLevel {  get; set; }
        public string? Notes {  get; set; }
        public virtual FishSpecie Species1 { get; set; }
        public virtual FishSpecie Species2 { get; set; }
    }
}
