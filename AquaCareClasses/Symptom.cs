using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Symptom
    {
        public int SymptomId { get; set; }
        public string Name { get; set; }
        public virtual ICollection<Disease> Diseases { get; set; } = new List<Disease>();
    }
}
