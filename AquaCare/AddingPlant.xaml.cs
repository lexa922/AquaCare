using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
using DbImage = AquaCareClasses.Image;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AddingPlant.xaml
    /// </summary>
    public partial class AddingPlant : Page
    {
        private List<string> selectedPhotoPaths = new List<string>();
        public AddingPlant()
        {
            InitializeComponent();
        }
        private void SelectPhotosButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.jpg;*.png;*.jpeg)|*.jpg;*.png;*.jpeg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                selectedPhotoPaths.AddRange(openFileDialog.FileNames);
                SelectedPhotos.ItemsSource = null;
                SelectedPhotos.ItemsSource = selectedPhotoPaths;
            }
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(PlantName.Text))
            {
                MessageBox.Show("Назва виду є обов'язковою.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selectedPhotoPaths == null || !selectedPhotoPaths.Any())
            {
                MessageBox.Show("Хоча б одне фото є обов'язковим.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Substrate.SelectedItems.Count == 0)
            {
                MessageBox.Show("Виберіть хоча б один підходящий тип ґрунту.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (LightIntensity.SelectedValue == null)
            {
                MessageBox.Show("Виберіть інтенсивність освітлення.", "Помилка валідації", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int lightDuration = LightDuration.Value ?? 0;
                var newPlant = new PlantSpecie
                {
                    SpeciesName = PlantName.Text,
                    SpeciesDescription = SpeciesDesc.Text,
                    LightDurationHrs = lightDuration,
                    LightIntensityLevelId = (int)LightIntensity.SelectedValue
                };

                var selectedSubstrateIds = Substrate.SelectedItems
                                            .Cast<SubstrateType>()
                                            .Select(s => s.SubstrateTypeId)
                                            .ToList();

                var dbImagePaths = new List<string>();
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagesFolder = Path.Combine(appDirectory, "PlantSpeciesImg");
                Directory.CreateDirectory(imagesFolder);

                foreach (string originalPath in selectedPhotoPaths)
                {
                    string fileName = Path.GetFileName(originalPath);
                    string destinationPath = Path.Combine(imagesFolder, fileName);
                    File.Copy(originalPath, destinationPath, true);

                    string relativePath = Path.Combine("PlantSpeciesImg", fileName);
                    dbImagePaths.Add(relativePath);
                }

                var plantRepo = new PlantRepository(UserSession.CurrentConnectionString);

                await plantRepo.AddSpeciesAsync(newPlant, selectedSubstrateIds, dbImagePaths);

                MessageBox.Show("Новий вид рослини успішно додано!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);
                ClearForm();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var plantRepo = new PlantRepository(UserSession.CurrentConnectionString);
                var lightRepo = new LightsRepository(UserSession.CurrentConnectionString);
                var subsRepo = new SubstrateRepository(UserSession.CurrentConnectionString);

                var substrateTypes = await subsRepo.GetSubstrateTypesAsync();
                Substrate.ItemsSource = substrateTypes;

                Substrate.DisplayMemberPath = "TypeName";
                Substrate.SelectedValuePath = "SubstrateTypeId";

                var lightIntensity = await lightRepo.GetLightIntensityLevelsAsync();
                LightIntensity.ItemsSource = lightIntensity;

                LightIntensity.DisplayMemberPath = "Name";
                LightIntensity.SelectedValuePath = "LightIntensityLevelId";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження довідників: {ex.Message}", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ClearForm()
        {
            PlantName.Clear();
            Substrate.SelectedItems.Clear();
            LightDuration.Value = 8;
            LightIntensity.SelectedItem = null;
            SelectedPhotos.ItemsSource = null;
            selectedPhotoPaths.Clear();
        }
    }
}
