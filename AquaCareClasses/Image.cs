using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Image
    {
        public int ImageId {  get; set; }
        public string ImagePath { get; set;}
        public int? FishSpeciesId { get; set; }
        public int? PlantSpeciesId { get; set; } 

        public int? WaterChangeId { get; set; }
        public int? DiseaseLogId { get; set; }
        public int? BreedingLogId { get; set; }

        public virtual FishSpecie FishSpecies { get; set; }
        public virtual PlantSpecie PlantSpecies { get; set; }
        public virtual WaterChange WaterChange { get; set; }
        public virtual DiseaseLog DiseaseLog { get; set; }
        public virtual BreedingLog BreedingLog { get; set; }

    }
}
