using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Aquarium
    {
        public int UserId {  get; set; }
        public int AquariumId {  get; set; }
        public string Name { get; set; }
        public int Volume {  get; set; }
        public int SubstrateId { get; set; }
        public int LightIntensityLevelId { get; set; }
        public virtual Substrate Substrate { get; set; }

        public virtual LightIntensityLevel LightIntensityLevel { get; set; }
        public TimeOnly LightOnTime {  get; set; }
        public TimeOnly LightOffTime {  get; set; }
        public ICollection<Fish>Fishes { get; set; }
        public ICollection<Plant>Plants { get; set; }
        public ICollection<WaterChange> WaterChanges { get; set; }
    }
}
