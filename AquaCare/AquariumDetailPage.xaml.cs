using AquaCare.Repositories;
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
using DbFish = AquaCareClasses.Fish;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AquariumDetailPage.xaml
    /// </summary>
    public partial class AquariumDetailPage : Page
    {
        private int _aquariumId;
        private FishRepository fishRepo = new FishRepository(UserSession.CurrentConnectionString);
        private PlantRepository plantRepo = new PlantRepository(UserSession.CurrentConnectionString);
        private WaterChangeRepository changeRepo = new WaterChangeRepository(UserSession.CurrentConnectionString);
        private DiseaseRepository diseaseRepo = new DiseaseRepository(UserSession.CurrentConnectionString);
        public AquariumDetailPage(int aquariumId)
        {
            InitializeComponent();
            _aquariumId = aquariumId;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadData();
        }
        private readonly AquariumRepository aquaRepo = new AquariumRepository(UserSession.CurrentConnectionString);
        private readonly BreedingRepository breedingRepository = new BreedingRepository(UserSession.CurrentConnectionString);

        private int _aquariumVolume;
        private async Task LoadBreedingLogsAsync()
        {
            var logs = await breedingRepository.GetLogsByAquariumIdAsync(_aquariumId);
            BreedingList.ItemsSource = logs;
        }
        private async Task LoadDiseasesAsync()
        {
            try
            {
                var activeDiseases = await diseaseRepo.GetActiveLogsByAquariumIdAsync(_aquariumId);

                ActiveDiseasesList.ItemsSource = null;

                ActiveDiseasesList.ItemsSource = activeDiseases;

                if (activeDiseases.Count == 0)
                {
                    NoDiseasesPanel.Visibility = Visibility.Visible;
                    ActiveDiseasesList.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NoDiseasesPanel.Visibility = Visibility.Collapsed;
                    ActiveDiseasesList.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не вдалося завантажити журнал хвороб: {ex.Message}");
            }
        }
        private async Task LoadFishList()
        {
            var fishList = await fishRepo.GetAllByAquariumIdAsync(_aquariumId);
            FishGrid.ItemsSource = fishList;
        }
        private async Task LoadData()
        {
            try
            {
                Aquarium aquarium = await aquaRepo.GetByIdAsync(_aquariumId);
                _aquariumVolume = aquarium.Volume;

                if (aquarium != null)
                {
                    TankNameText.Text = aquarium.Name;
                    string substrate = aquarium.Substrate.Name ?? "Немає ґрунту";
                    TankInfoText.Text = $"{aquarium.Volume} л | Ґрунт: {substrate}";
                }
                await LoadFishList();

                var rawPlantList = await plantRepo.GetAllByAquariumIdAsync(_aquariumId);

                var finalGroupedList = rawPlantList
                    .GroupBy(p => p.Plants.SpeciesName)
                    .Select(group => new
                    {
                        SpeciesName = group.Key,
                        Quantity = group.Sum(x => x.Quantity),
                        LightInfo = group.First().Plants.LightIntensityLevel != null
                            ? group.First().Plants.LightIntensityLevel.Name
                            : "-"
                    }).ToList();
                PlantsGrid.ItemsSource = finalGroupedList;

                var changes = await changeRepo.GetAllByAquariumIdAsync(_aquariumId);
                ChangesGrid.ItemsSource = changes;

                await LoadDiseasesAsync();
                await LoadBreedingLogsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження: {ex.Message}");
            }
        }

        private void AddFish_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddFishPage(_aquariumId));
        }

        private void AddPlant_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddPlantPage(_aquariumId));
        }
        private void AddWaterChange_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddWaterChange(_aquariumId, _aquariumVolume));
        }
        private void ViewPhotosButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var waterChange = button.DataContext as WaterChange;

            if (waterChange != null)
            {
                var gallery = new PhotoGalleryWindow(waterChange.Images);
                gallery.ShowDialog();
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            await aquaRepo.DeleteAsync(_aquariumId);
            NavigationService.Navigate(new Aquariums());
        }

        private void AddFishDiseaseButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddDiseaseLogPage(_aquariumId, true));
        }

        private void AddPlantDiseaseButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddDiseaseLogPage(_aquariumId, false));
        }

        private async void CureButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int logId)
            {
                var result = MessageBox.Show(
                    "Позначити пацієнта як повністю здорового?\nЗапис переміститься в архів.",
                    "Одужання",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await diseaseRepo.CureAsync(logId);


                        await LoadDiseasesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка: {ex.Message}");
                    }
                }
            }
        }
        private void EditDiseaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DiseaseLog logToEdit)
            {
                bool isFish = logToEdit.FishId != null;
                NavigationService.Navigate(new AddDiseaseLogPage(_aquariumId, isFish, logToEdit));
            }
        }
        private async void DeathButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int logId)
            {
                var result = MessageBox.Show(
                    "Ви впевнені, що пацієнт загинув?\n\n" +
                    "Це закриє історію хвороби, а статус риби буде змінено на 'Мертва'.",
                    "Сумна подія 😢",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await diseaseRepo.RegisterDeathAsync(logId);

                        await LoadDiseasesAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка: {ex.Message}");
                    }
                }
            }
        }
        private async void DeleteFishButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int fishId)
            {
                var result = MessageBox.Show(
                    "Ви точно хочете видалити цю рибу? Це незворотньо.\n(Історія розмноження збережеться, але без імені видаленого)",
                    "Видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {

                        await fishRepo.DeleteByIdAsync(fishId);
                        await LoadFishList();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка видалення: {ex.Message}");
                    }
                }
            }
        }
        private void AddBreeding_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new AddBreedingPage(_aquariumId));
        }
        private void EditBreeding_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BreedingLog log)
            {
                NavigationService.Navigate(new AddBreedingPage(_aquariumId, log));
            }
        }
        private void SettleFry_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is BreedingLog log)
            {
                var win = new SettleFryWindow(log);
                win.ShowDialog();
                LoadBreedingLogsAsync();
            }
        }
        private async void DeleteBreeding_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is int logId)
            {
                var result = MessageBox.Show(
                    "Ви точно хочете видалити цей запис про нерест?\nФотографії також будуть видалені.",
                    "Видалення",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await breedingRepository.DeleteLogAsync(logId);
                        await LoadBreedingLogsAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Помилка видалення: {ex.Message}");
                    }
                }
            }
        }
        private async void EditFishButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DbFish fishToEdit)
            {
                var editWindow = new EditFishWindow(fishToEdit);

                editWindow.ShowDialog();

                await LoadFishList();
            }
        }
    }
}
