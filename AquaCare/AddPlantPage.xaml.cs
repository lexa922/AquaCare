using AquaCareClasses;
using AquaClasses;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
using DbPlant = AquaCareClasses.Plant;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AddPlantPage.xaml
    /// </summary>
    public partial class AddPlantPage : Page
    {
        private int _aquariumId;
        public class PlantSelectionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string CoverPath { get; set; }
        }

        public AddPlantPage(int aquariumId)
        {
            InitializeComponent();
            _aquariumId = aquariumId;
        }
        List<PlantSpecie> filteredPlants;
        PlantRepository plantRepo = new PlantRepository(UserSession.CurrentConnectionString);
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var aquaRepo = new AquariumRepository(UserSession.CurrentConnectionString);

            try
            {
                var displayList = new List<PlantSelectionViewModel>();
                var aquarium = await aquaRepo.GetByIdAsync(_aquariumId);

                if (aquarium != null && aquarium.Substrate != null)
                {
                    int currentSubstrateTypeId = aquarium.Substrate.SubstrateTypeId;
                    int currentLightIntensityId = aquarium.LightIntensityLevelId;

                    filteredPlants = await plantRepo.GetCompatiblePlantsAsync(currentSubstrateTypeId, currentLightIntensityId);
                }
                else
                {
                    filteredPlants = await plantRepo.GetAllSpeciesAsync();
                }
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var species in filteredPlants)
                {
                    string finalPath = System.IO.Path.Combine(baseDir, "PlantSpeciesImg", "default.png");

                    var firstImage = species.Images?.FirstOrDefault();

                    if (firstImage != null && !string.IsNullOrEmpty(firstImage.ImagePath))
                    {
                        string cleanDbPath = firstImage.ImagePath.TrimStart('/', '\\');

                        string potentialPath = System.IO.Path.Combine(baseDir, cleanDbPath);

                        if (System.IO.File.Exists(potentialPath))
                        {
                            finalPath = potentialPath;
                        }
                    }

                    string substrateInfo = "Будь-який";
                    if (species.SuitableSubstrateTypes != null && species.SuitableSubstrateTypes.Any())
                    {
                        substrateInfo = string.Join(", ", species.SuitableSubstrateTypes.Select(s => s.TypeName));
                    }

                    string lightName = species.LightIntensityLevel?.Name ?? "Невідомо";

                    string durationInfo = species.LightDurationHrs > 0 ? $"{species.LightDurationHrs} год" : "Не вказано";

                    string fullDescription = $"🌍 Ґрунт: {substrateInfo}\n" +
                                             $"💡 Освітлення: {lightName}\n" +
                                             $"⏱ Тривалість дня: {durationInfo}";

                    displayList.Add(new PlantSelectionViewModel
                    {
                        Id = species.PlantSpecieId,
                        Name = species.SpeciesName,
                        Description = fullDescription,
                        CoverPath = finalPath
                    });
                }

                SpeciesListBox.ItemsSource = displayList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження: {ex.Message}");
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedPlant = SpeciesListBox.SelectedItem as PlantSelectionViewModel;

            if (selectedPlant == null)
            {
                MessageBox.Show("Будь ласка, оберіть рослину (натисніть на картку).");
                return;
            }

            int speciesId = selectedPlant.Id;
            int quantity = QuantityUpDown.Value ?? 1;

                var newPlant = new DbPlant
                {
                    AquariumId = _aquariumId,
                    SpeciesId = speciesId,
                    Quantity = quantity
                };

                try
                {
                    await plantRepo.AddAsync(newPlant);
                    MessageBox.Show("Рослини успішно посаджено!");

                    if (NavigationService.CanGoBack)
                    {
                        NavigationService.GoBack();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження: {ex.Message}");
                    var errorMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                    MessageBox.Show($"ДЕТАЛІ ПОМИЛКИ:\n{errorMessage}");
                }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }
    }
}
