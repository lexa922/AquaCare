using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class WaterChange
    {
        public int WaterChangeId {  get; set; }
        public int AquariumId {  get; set; }
        public DateOnly ChangeDate {  get; set; }
        public int Volume {  get; set; }
        public Aquarium Aquarium { get; set; }
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();
    }
}
