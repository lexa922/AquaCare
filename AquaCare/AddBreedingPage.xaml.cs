using AquaCare.Repositories;
using AquaCareClasses;
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
    /// Interaction logic for AddBreedingPage.xaml
    /// </summary>
    public partial class AddBreedingPage : Page
    {
        private readonly int _currentAquariumId;
        private BreedingLog _existingLog;
        private List<ParentDto> _allFathers = new List<ParentDto>();
        private List<ParentDto> _allMothers = new List<ParentDto>();
        private bool _isFiltering = false;
        private List<string> _newPhotoPaths = new List<string>();
        private readonly BreedingRepository breedRepo=new BreedingRepository(UserSession.CurrentConnectionString);

        public AddBreedingPage(int aquariumId, BreedingLog logToEdit = null)
        {
            InitializeComponent();
            _currentAquariumId = aquariumId;
            _existingLog = logToEdit;
            if (_existingLog != null)
            {
                Title = "Редагування нересту";
                LoadExistingData();
            }
        }
        private async void LoadExistingData()
        {
            SpawnDatePicker.SelectedDate = _existingLog.StartDate;
            FatherCombo.SelectedValue = _existingLog.MaleFishId;
            MotherCombo.SelectedValue = _existingLog.FemaleFishId;
            CountBox.Text = _existingLog.OffspringCount?.ToString() ?? "0";
            OutcomeBox.Text = _existingLog.Outcome;

            var photos = await breedRepo.GetPhotosAsync(_existingLog.BreedingLogId);
            PhotosList.ItemsSource = photos;
        }
        private void ClearFather_Click(object sender, RoutedEventArgs e)
        {
            _isFiltering = true;
            FatherCombo.SelectedIndex = -1;

            MotherCombo.ItemsSource = _allMothers;
            _isFiltering = false;
        }

        private void ClearMother_Click(object sender, RoutedEventArgs e)
        {
            _isFiltering = true;
            MotherCombo.SelectedIndex = -1;

            FatherCombo.ItemsSource = _allFathers;
            _isFiltering = false;
        }
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void AddPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Filter = "Images|*.jpg;*.png;*.jpeg" };
            if (dlg.ShowDialog() == true)
            {
                _newPhotoPaths.AddRange(dlg.FileNames);
                PhotosList.ItemsSource = null;
                PhotosList.ItemsSource = _newPhotoPaths; // Тут проста логіка, для повноцінної треба об'єднувати старі і нові
                PhotoCountText.Text = $"Нових: {_newPhotoPaths.Count}";
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SpawnDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Будь ласка, оберіть дату події.");
                return;
            }

            try
            {
                var log = new BreedingLog
                {
                    AquariumId = _currentAquariumId,
                    StartDate = SpawnDatePicker.SelectedDate.Value,
                    MaleFishId = (int?)FatherCombo.SelectedValue,
                    FemaleFishId = (int?)MotherCombo.SelectedValue,

                    OffspringCount = int.TryParse(CountBox.Text, out int count) ? count : 0,

                    Outcome = OutcomeBox.Text,

                    BreedingLogId = _existingLog?.BreedingLogId ?? 0
                };
                if (_existingLog == null)
                {
                    int newId = await breedRepo.AddLogAsync(log);

                    if (_newPhotoPaths != null)
                    {
                        foreach (var path in _newPhotoPaths)
                        {
                            await breedRepo.AddPhotoAsync(newId, path);
                        }
                    }
                }
                else
                {
                    await breedRepo.UpdateLogAsync(log);
                    foreach (var path in _newPhotoPaths)
                    {
                        await breedRepo.AddPhotoAsync(log.BreedingLogId, path);
                    }
                }
                MessageBox.Show("Запис про нерест успішно додано! 🐟");

                NavigationService?.Navigate(new AquariumDetailPage(_currentAquariumId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new AquariumDetailPage(_currentAquariumId));
        }

        private void Border_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private async void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            try
            {
                int currentUserId = UserSession.CurrentUser.UserId;
                _allFathers = await breedRepo.GetPotentialParentsAsync("Male", currentUserId);
                _allMothers = await breedRepo.GetPotentialParentsAsync("Female", currentUserId);

                FatherCombo.ItemsSource = _allFathers;
                MotherCombo.ItemsSource = _allMothers;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження списків батьків: {ex.Message}");
            }
        }

        private void FatherCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isFiltering) return;

            if (FatherCombo.SelectedItem is ParentDto selectedFather)
            {
                _isFiltering = true;

                var currentMother = MotherCombo.SelectedItem as ParentDto;

                var compatibleMothers = _allMothers
                    .Where(m => m.SpeciesId == selectedFather.SpeciesId)
                    .ToList();

                MotherCombo.ItemsSource = compatibleMothers;

                if (currentMother != null && currentMother.SpeciesId != selectedFather.SpeciesId)
                {
                    MotherCombo.SelectedIndex = -1;
                }
                else if (currentMother != null)
                {
                    MotherCombo.SelectedValue = currentMother.Id;
                }

                _isFiltering = false;
            }
        }

        private void MotherCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isFiltering) return;

            if (MotherCombo.SelectedItem is ParentDto selectedMother)
            {
                _isFiltering = true;

                var currentFather = FatherCombo.SelectedItem as ParentDto;

                var compatibleFathers = _allFathers
                    .Where(f => f.SpeciesId == selectedMother.SpeciesId)
                    .ToList();

                FatherCombo.ItemsSource = compatibleFathers;

                if (currentFather != null && currentFather.SpeciesId != selectedMother.SpeciesId)
                {
                    FatherCombo.SelectedIndex = -1;
                }
                else if (currentFather != null)
                {
                    FatherCombo.SelectedValue = currentFather.Id;
                }

                _isFiltering = false;
            }
        }
    }
}
