using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class Disease
    {
        public int DiseaseId {  get; set; }
        public string Name { get; set; }
        public string Treatment {  get; set; }
        public virtual ICollection<Symptom> Symptoms { get; set; } = new List<Symptom>();
        public virtual ICollection<DiseaseLog> DiseaseLogs { get; set; } = new List<DiseaseLog>();
    }
}
