using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AquaCareClasses
{
    public class DiseaseLog
    {
        public int DiseaseLogId { get; set; }
        public int? FishId { get; set; }
        public int? PlantId { get; set; }
        public int DiseaseId { get; set; }
        public string? TreatmentNotes { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string DiseaseName { get; set; }
        public string PatientName { get; set; }
        public bool IsActive => EndDate == null;
        public virtual ICollection<Image> Images { get; set; } = new List<Image>();

        public string MainImagePath
        {
            get
            {
                var img = Images?.FirstOrDefault(i => !i.ImagePath.Contains("placeholder", StringComparison.OrdinalIgnoreCase));
                return img != null ? img.ImagePath : "/PlaceholderImgs/sick_fish_placeholder.png";
            }
        }
        public string TypeIcon => FishId != null ? "🐟" : "🌿";
        public string DurationInfo
        {
            get
            {
                int days = (DateTime.Now.Date - StartDate.ToDateTime(TimeOnly.MinValue).Date).Days;
                return days == 0 ? "Сьогодні" : $"{days} дн.";
            }
        }
    }
}
