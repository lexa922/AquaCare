using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Plant
    {
        public int PlantId {  get; set; }
        public int AquariumId {  get; set; }
        public int SpeciesId {  get; set; }
        public int Quantity {  get; set; }
        public Aquarium Aquariums { get; set; }
        [ForeignKey("SpeciesId")]
        public PlantSpecie Plants { get; set; }
        
    }
}
