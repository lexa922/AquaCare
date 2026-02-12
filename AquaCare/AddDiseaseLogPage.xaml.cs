using AquaCare.Repositories;
using AquaCareClasses;
using Microsoft.Win32;
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
using NoDbImage = System.Windows.Controls.Image;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AddDiseaseLogPage.xaml
    /// </summary>
    public partial class AddDiseaseLogPage : Page
    {
        private readonly int _aquariumId;
        private readonly bool _isFish;
        private DiseaseLog _existingLog = null;
        private readonly DiseaseRepository diseaseRepo = new DiseaseRepository(UserSession.CurrentConnectionString);

        private List<int> selectedSymptomIds = new();
        private List<string> selectedPhotoPaths = new List<string>();
        public AddDiseaseLogPage(int aquariumId, bool isFish, DiseaseLog logToEdit = null)
        {
            InitializeComponent();
            _aquariumId = aquariumId;
            _isFish = isFish;
            _existingLog = logToEdit;

            if (_existingLog != null)
            {
                this.Title = "Редагування запису";
                SaveBtn.Content = "Оновити запис";
            }
            else
            {
                this.Title = _isFish ? "Додати хворобу риби" : "Додати хворобу рослини";
                SaveBtn.Content = "Зберегти запис";
            }
        }
        private void FillFormForEdit()
        {
            int patientId = _isFish ? (_existingLog.FishId ?? 0) : (_existingLog.PlantId ?? 0);
            PatientComboBox.SelectedValue = patientId;

            PatientComboBox.IsEnabled = false;

            DiseasesComboBox.SelectedValue = _existingLog.DiseaseId;

            NotesBox.Text = _existingLog.TreatmentNotes;
            StartDatePicker.SelectedDate = _existingLog.StartDate.ToDateTime(TimeOnly.MinValue);
        }
        private void Symptom_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int id)
                selectedSymptomIds.Add(id);
        }

        private void Symptom_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox cb && cb.Tag is int id)
                selectedSymptomIds.Remove(id);
        }

        private async void DiagnoseButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedSymptomIds.Count == 0)
            {
                MessageBox.Show("Оберіть хоча б один симптом!");
                return;
            }

            try
            {
                var results = await diseaseRepo.DiagnoseAsync(selectedSymptomIds);

                DiagnosisResultsList.ItemsSource = results;

                if (results.Count == 0) MessageBox.Show("На жаль, за цими симптомами нічого не знайдено.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка пошуку: {ex.Message}");
            }
        }

        private void DiagnosisResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DiagnosisResultsList.SelectedItem is DiagnosisResult result)
            {
                DiseasesComboBox.SelectedValue = result.Disease.DiseaseId;

                if (string.IsNullOrEmpty(NotesBox.Text))
                {
                    NotesBox.Text = $"Рекомендовано: {result.Disease.Treatment}";
                }
            }
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPhotoPaths == null)
                selectedPhotoPaths = new List<string>();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Зображення|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    selectedPhotoPaths.Add(filename);

                    NoDbImage imgPreview = new NoDbImage
                    {
                        Width = 60,
                        Height = 60,
                        Margin = new Thickness(5),
                        Source = new BitmapImage(new Uri(filename)),
                        Stretch = Stretch.UniformToFill
                    };

                    PhotoPreviewPanel.Children.Add(imgPreview);
                }
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PatientComboBox.SelectedValue == null)
            {
                MessageBox.Show("Оберіть пацієнта!");
                return;
            }
            if (DiseasesComboBox.SelectedValue == null)
            {
                MessageBox.Show("Оберіть хворобу (або скористайтесь діагностикою)!");
                return;
            }
            if (StartDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Оберіть дату початку хвороби!");
                return;
            }

            try
            {
                int diseaseId = (int)DiseasesComboBox.SelectedValue;
                string notes = NotesBox.Text;
                DateOnly startDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value);

                if (_existingLog != null)
                {
                    _existingLog.DiseaseId = diseaseId;
                    _existingLog.TreatmentNotes = notes;
                    _existingLog.StartDate = startDate;

                    await diseaseRepo.UpdateLogAsync(_existingLog);

                    if (selectedPhotoPaths != null && selectedPhotoPaths.Any())
                    {
                        foreach (var photoPath in selectedPhotoPaths)
                        {
                            if (!photoPath.Contains("placeholder"))
                            {
                                await diseaseRepo.AddImageToLogAsync(_existingLog.DiseaseLogId, photoPath);
                                _existingLog.Images.Add(new AquaCareClasses.Image
                                {
                                    DiseaseLogId = _existingLog.DiseaseLogId,
                                    ImagePath = photoPath
                                });
                            }
                        }
                    }

                    MessageBox.Show("Запис оновлено!");
                    NavigationService.GoBack();
                }
                else
                {
                    var newLog = new DiseaseLog
                    {
                        DiseaseId = (int)DiseasesComboBox.SelectedValue,
                        TreatmentNotes = NotesBox.Text,
                        StartDate = DateOnly.FromDateTime(StartDatePicker.SelectedDate.Value),
                        EndDate = null
                    };

                    int patientId = (int)PatientComboBox.SelectedValue;
                    if (_isFish)
                    {
                        newLog.FishId = patientId;
                        newLog.PlantId = null;
                    }
                    else
                    {
                        newLog.FishId = null;
                        newLog.PlantId = patientId;
                    }

                    if (selectedPhotoPaths == null || !selectedPhotoPaths.Any())
                    {
                        if (selectedPhotoPaths == null) selectedPhotoPaths = new List<string>();

                        string defaultPhotoPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PlaceholderImgs", "sick_fish_placeholder.png");

                        if (System.IO.File.Exists(defaultPhotoPath))
                        {
                            selectedPhotoPaths.Add(defaultPhotoPath);
                        }
                    }
                    await diseaseRepo.AddLogAsync(newLog, selectedPhotoPaths);

                    MessageBox.Show("Запис успішно додано!");

                    NavigationService.GoBack();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}");
            }
        }
        private async void Page_Loaded_1(object sender, RoutedEventArgs e)
        {
            var symptoms = await diseaseRepo.GetAllSymptomsAsync();
            SymptomsList.ItemsSource = symptoms;

            var allDiseases = await diseaseRepo.GetAllDiseasesAsync();
            DiseasesComboBox.ItemsSource = allDiseases;

            List<PatientLookupDto> potentialPatients = await diseaseRepo.GetPotentialPatientsAsync(_aquariumId, _isFish);
            PatientComboBox.ItemsSource = potentialPatients;

            if (_existingLog != null)
            {
                FillFormForEdit();
            }
            else
            {
                StartDatePicker.SelectedDate = DateTime.Now;
            }
        }
    }
}