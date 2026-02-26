using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class BreedingLog
    {
        public int BreedingLogId {  get; set; }
        public int AquariumId {  get; set; }
        public int? MaleFishId {  get; set; }
        public int? FemaleFishId {  get; set; }
        public DateTime StartDate {  get; set; }
        public string? Outcome {  get; set; }
        public int? OffspringCount {  get; set; }
        public Aquarium Aquarium { get; set; }
        public Fish MaleFish {  get; set; }
        public Fish FemaleFish { get; set; }
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();

    }
}
