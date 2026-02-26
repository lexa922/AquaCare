using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class ParentDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string AquariumName { get; set; }
        public int SpeciesId { get; set; }

        public string DisplayName => $"{Name} ({AquariumName})";
    }
}
