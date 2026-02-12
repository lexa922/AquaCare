using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class FishSpecie
    {
        public int FishSpecieId { get; set; }
        public string SpeciesName { get; set; }
        public string SpeciesDescription { get; set; }
        public int MinTankSize { get; set; }
        public double BioLoadValue{get; set;}
        public int MinGroupSize {  get; set; }
        [NotMapped]
        public string? ImagePath { get; set; }
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
        public virtual ICollection<CompatibilityRule> CompatibilityRulesAsSpecies1 { get; set; } = new List<CompatibilityRule>();
        public virtual ICollection<CompatibilityRule> CompatibilityRulesAsSpecies2 { get; set; } = new List<CompatibilityRule>();
        public FishSpecie()
        {

        }
        public FishSpecie(string speciesName, string speciesDescription, int minTankSize, double bioLoadValue, 
            int minGroupSize)
        {
            SpeciesName = speciesName;
            SpeciesDescription = speciesDescription;
            MinTankSize = minTankSize;
            BioLoadValue = bioLoadValue;
            MinGroupSize = minGroupSize;
            
        }
    }
}
