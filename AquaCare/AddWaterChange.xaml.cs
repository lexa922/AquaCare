using AquaCare.Repositories;
using AquaCareClasses;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for AddWaterChange.xaml
    /// </summary>
    public partial class AddWaterChange : Page
    {
        private int _aquariumId;
        private int _aquariumVolume;
        private List<string> _selectedFilePaths = new List<string>();

        public AddWaterChange(int aquariumId, int aquariumVolume)
        {
            InitializeComponent();
            _aquariumId = aquariumId;
            _aquariumVolume = aquariumVolume;
            ChangeDatePicker.SelectedDate = DateTime.Now;
        }

        private void VolumeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SelectImagesButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Зображення|*.jpg;*.jpeg;*.png;*.bmp";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    if (!_selectedFilePaths.Contains(filename))
                    {
                        _selectedFilePaths.Add(filename);
                    }
                }
                RefreshImagesList();
            }
        }

        private void RefreshImagesList()
        {
            SelectedImagesList.ItemsSource = null;
            SelectedImagesList.ItemsSource = _selectedFilePaths;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(VolumeTextBox.Text) || !int.TryParse(VolumeTextBox.Text, out int volume))
            {
                MessageBox.Show("Будь ласка, введіть коректний об'єм води (число).", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (volume>=_aquariumVolume)
            {
                MessageBox.Show("Неможливо зробити підміну об'ємом більшу за акваріум","Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (ChangeDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Будь ласка, оберіть дату.", "Помилка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DateOnly date = DateOnly.FromDateTime(ChangeDatePicker.SelectedDate.Value);

            var newWaterChange = new WaterChange
            {
                AquariumId = _aquariumId,
                ChangeDate = date,
                Volume = volume
            };

            try
            {
                var repo = new WaterChangeRepository(UserSession.CurrentConnectionString);

                await repo.AddAsync(newWaterChange, _selectedFilePaths);

                MessageBox.Show("Підміну води успішно записано!", "Успіх", MessageBoxButton.OK, MessageBoxImage.Information);

                NavigationService.GoBack();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка при збереженні: {ex.Message}", "Помилка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}