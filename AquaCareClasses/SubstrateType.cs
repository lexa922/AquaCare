using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class SubstrateType
    {
        public int SubstrateTypeId {  get; set; }
        public string TypeName { get; set; }
        public virtual ICollection<Substrate> Substrates { get; set; } = new List<Substrate>();
        public virtual ICollection<PlantSpecie> PlantSpecies { get; set; } = new List<PlantSpecie>();
    }
}
