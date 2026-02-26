using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
using System.IO;
using System.Windows.Navigation;
using FishObj = AquaCareClasses.Fish;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AddFishPage.xaml
    /// </summary>
    public partial class AddFishPage : Page
    {
        private int _aquariumId;
        public class FishSelectionViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int MinTankSize { get; set; }
            public int MinGroupSize {  get; set; }
            public double BioLoadValue {  get; set; }
            public string CoverPath { get; set; }
        }
        public AddFishPage(int aquariumId)
        {
            InitializeComponent();
            _aquariumId = aquariumId;
        }
        List<FishSpecie> fishList;
        Aquarium aquarium;
        public async Task<(List<string> warnings,bool isCritical)> CheckCompatibilityAsync(Aquarium aquarium, FishSpecie newFish)
        {
            var warnings = new List<string>();
            bool isCritical = false;
            if (aquarium.Volume < newFish.MinTankSize)
            {
                warnings.Add($"Акваріум замалий! Для {newFish.SpeciesName} потрібно мінімум {newFish.MinTankSize} л.");
            }
            var existingSpeciesIds = aquarium.Fishes
                                             .Select(f => f.SpeciesId)
                                             .Distinct()
                                             .ToList();

            if (existingSpeciesIds.Count > 0)
            {
                try
                {
                    var fishRepo = new FishRepository(UserSession.CurrentConnectionString);
                    var rules = await fishRepo.CheckCompatibilityAsync(newFish.FishSpecieId, existingSpeciesIds);

                    foreach (var rule in rules)
                    {
                        if (rule.CompatabilityLevel == 2)
                        {
                            warnings.Add($"⛔ КРИТИЧНО: {rule.Notes}");
                            isCritical = true;
                        }
                        else if (!string.IsNullOrEmpty(rule.Notes))
                        {
                            warnings.Add($"⚠️ Увага: {rule.Notes}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    warnings.Add($"Помилка перевірки: {ex.Message}");
                }
            }
            return (warnings, isCritical);
        }
        FishRepository fishRepo = new FishRepository(UserSession.CurrentConnectionString);
        public async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var aquaRepo = new AquariumRepository(UserSession.CurrentConnectionString);

            try
            {
                aquarium = await aquaRepo.GetByIdWithFishAsync(_aquariumId);

                fishList = await fishRepo.GetAllSpeciesAsync();

                List<FishSelectionViewModel> displayList = new();
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                foreach (var species in fishList)
                {
                    string finalPath = Path.Combine(baseDir, "FishSpeciesImg", "default.png");

                    if (!string.IsNullOrEmpty(species.ImagePath))
                    {
                        string potentialPath = Path.Combine(baseDir, species.ImagePath);

                        if (File.Exists(potentialPath))
                        {
                            finalPath = potentialPath;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Не знайдено файл: {potentialPath}");
                        }
                    }
                    displayList.Add(new FishSelectionViewModel
                    {
                        Id = species.FishSpecieId,
                        Name = species.SpeciesName,
                        MinTankSize=species.MinTankSize,
                        MinGroupSize=species.MinGroupSize,
                        BioLoadValue=species.BioLoadValue,
                        CoverPath = finalPath
                    });
                }
                SpeciesListBox.ItemsSource = displayList;
                SaveButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Виникла помилка при завантаженні списку риб: {ex}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCard = SpeciesListBox.SelectedItem as FishSelectionViewModel;
            if (selectedCard == null) return;

            FishSpecie selctSpecie = fishList.FirstOrDefault(s => s.FishSpecieId == selectedCard.Id);
            string name;

            if (string.IsNullOrEmpty(NameTextBox.Text))
            {
                var existingNames = aquarium.Fishes
        .Where(f => f.SpeciesId == selctSpecie.FishSpecieId)
        .Select(f => f.FishName)
        .ToList();

                int maxNumber = 0;
                string baseName = selctSpecie.SpeciesName;

                foreach (var fishName in existingNames)
                {
                    if (fishName.StartsWith(baseName))
                    {
                        string numberPart = fishName.Substring(baseName.Length).Trim();

                        if (int.TryParse(numberPart, out int number))
                        {
                            if (number > maxNumber) maxNumber = number;
                        }
                    }
                }
                int nextNumber = maxNumber + 1;
                name = $"{baseName} {nextNumber}";
            }
            else
            {
                name = NameTextBox.Text;
            }

            String[] tmp = GenderComboBox.Text.Split();

            FishObj newFish = new FishObj
            {
                FishName = name,
                SpeciesId = selctSpecie.FishSpecieId,
                Gender = tmp[0],
                IsAlive = true,
                AquariumId = _aquariumId
            };

            var results = await CheckCompatibilityAsync(aquarium, selctSpecie);

            List<string> warnings = results.warnings;
            bool isCritical = results.isCritical;
            if (warnings.Count > 0)
            {
                string message = "Знайдено застереження:\n" + string.Join("\n", warnings) + "\n\nДодати попри це?";

                var result1 = MessageBox.Show(message, "Перевірка сумісності",
                                              MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result1 == MessageBoxResult.No) return;
            }

            if (isCritical)
            {
                var result2 = MessageBox.Show(
                    "🛑 СТОП! Виявлено КРИТИЧНУ несумісність.\n" +
                    "Є дуже високий шанс летальних наслідків!\n\n" +
                    "Ви точно впевнені, що хочете продовжити? Риби можуть загинути.",
                    "Останнє попередження",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Stop);

                if (result2 == MessageBoxResult.No) return;
            }
            aquarium.Fishes.Add(newFish);
            MessageBox.Show($"{newFish.FishName} успішно додано!");
            
                try
                {
                    await fishRepo.AddAsync(newFish);

                    aquarium.Fishes.Add(newFish);

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Помилка збереження: {ex.Message}");
                }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }

        private void SpeciesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
                if (SpeciesListBox.SelectedItem != null && GenderComboBox.SelectedIndex != 0)
                {
                    SaveButton.IsEnabled = true;
                }
                else
                {
                    SaveButton.IsEnabled = false;
                }
        }

        private void GenderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SaveButton == null) return;
            if (SpeciesListBox.SelectedItem != null && GenderComboBox.SelectedIndex != 0)
            {
                SaveButton.IsEnabled = true;
            }
            else
            {
                SaveButton.IsEnabled = false;
            }
        }
    }
}
