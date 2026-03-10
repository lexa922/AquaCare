using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class PlantSpecie
    {
        public int PlantSpecieId {  get; set; }
        public string SpeciesName {  get; set; }
        public string SpeciesDescription {  get; set; }
        public int LightDurationHrs {  get; set; }
        public int? LightIntensityLevelId { get; set; }
        public virtual LightIntensityLevel LightIntensityLevel { get; set; }
        public virtual ICollection<SubstrateType> SuitableSubstrateTypes { get; set; } = new List<SubstrateType>();
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();

        [NotMapped]
        public string? ImagePath { get; set; }

        [NotMapped]
        public string? LightLevelName { get; set; }

        [NotMapped]
        public string? CompatibleSubstrates { get; set; }

        public PlantSpecie() { }
        public PlantSpecie(string speciesName, int lightDurationHrs)
        {
            SpeciesName = speciesName;
            LightDurationHrs = lightDurationHrs;
        }
        public string FullImagePath => 
            File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagePath)) 
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ImagePath) 
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlantSpeciesImg", "default.png");
    }
}
