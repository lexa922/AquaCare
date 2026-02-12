using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for CreatingAquarium.xaml
    /// </summary>
    public partial class CreatingAquarium : Page
    {
        public CreatingAquarium()
        {
            InitializeComponent();
        }
        List<LightIntensityLevel> lights;
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var substRepo = new SubstrateRepository(UserSession.CurrentConnectionString);
            var lightRepo = new LightsRepository(UserSession.CurrentConnectionString);

            try
            {
                var substratesFromDb = await substRepo.GetSubstratesAsync();
                lights = await lightRepo.GetLightIntensityLevelsAsync();

                LightIntensityList.ItemsSource = lights;

                var displayList = new List<SubstrateViewModel>();

                    foreach (var s in substratesFromDb)
                    {
                        string typeName = s.SubstrateType.TypeName;
                        string fullDescription = $"{typeName} | {s.Size}мм";

                        displayList.Add(new SubstrateViewModel
                        {
                            SubstrateId = s.SubstrateId,
                            Name = s.Name,
                            Color = s.Color,
                            Description = fullDescription
                        });
                    }
                    SubstrateList.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження ґрунтів: {ex.Message}");
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TankName.Text))
            {
                MessageBox.Show("Будь ласка, введіть назву акваріума.");
                return;
            }
            if (SubstrateList.SelectedValue == null)
            {
                MessageBox.Show("Будь ласка, виберіть тип ґрунту.");
                return;
            }
            if (LightIntensityList.SelectedValue == null)
            {
                MessageBox.Show("Будь ласка, виберіть інтенсивність освітлення.");
                return;
            }

            var aquaRepo = new AquariumRepository(UserSession.CurrentConnectionString);

            string name = TankName.Text;
            int volume = TankVolume.Value ?? 0;
            var selectedItem = SubstrateList.SelectedItem as SubstrateViewModel;
            int substrateId = selectedItem.SubstrateId;
            DateTime dtOn = LightOnTime.Value ?? DateTime.Today.AddHours(9);
            DateTime dtOff = LightOffTime.Value ?? DateTime.Today.AddHours(21);

            TimeOnly timeOn = TimeOnly.FromDateTime(dtOn);
            TimeOnly timeOff = TimeOnly.FromDateTime(dtOff);

            var selectedLight = LightIntensityList.SelectedItem as LightIntensityLevel;
            int lightIntensityId = selectedLight.LightIntensityLevelId;
            int userId = UserSession.CurrentUser.UserId;

                var newAquarium = new Aquarium
                {
                    UserId = userId,
                    Name = name,
                    Volume = volume,
                    SubstrateId = substrateId,
                    LightOnTime = timeOn,
                    LightOffTime = timeOff,
                    LightIntensityLevelId = lightIntensityId
                };

                try
                {
                    await aquaRepo.CreateAsync(newAquarium);
                    MessageBox.Show("Акваріум успішно створено!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження: {ex.Message}\n{ex.InnerException?.Message}");
                }
        }
        public class SubstrateViewModel
        {
            public int SubstrateId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Color { get; set; }
        }
    }
}
