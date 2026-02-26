using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Fish
    {
        public int FishId{get; set;}
        public string FishName{get; set;}
        public string Gender { get; set;}
        public bool IsAlive {  get; set;}
        public int AquariumId {  get; set;}
        public int SpeciesId {  get; set;}
        public virtual FishSpecie Species { get; set; }
        public ICollection<BreedingLog> MaleBreedingLogs { get; set; }
        public ICollection<BreedingLog> FemaleBreedingLogs { get; set; }
    }
}
