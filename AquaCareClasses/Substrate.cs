using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Substrate
    {
        public int SubstrateId {  get; set; }
        public int SubstrateTypeId { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public virtual SubstrateType SubstrateType { get; set; }
        public virtual ICollection<Aquarium> Aquariums { get; set; } = new List<Aquarium>();
        public virtual ICollection<PlantSpecie> PlantSpecies { get; set; } = new List<PlantSpecie>();
    }
}
