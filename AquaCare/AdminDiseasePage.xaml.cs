using AquaCare.Repositories;
using AquaCareClasses;
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

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AdminDiseasePage.xaml
    /// </summary>
    /// 
    public class SymptomSelectViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSelected { get; set; }
    }
    public partial class AdminDiseasePage : Page

    {
        private DiseaseRepository diseaseRepo = new DiseaseRepository(UserSession.CurrentConnectionString);
        private List<SymptomSelectViewModel> symptomsViewModel;
        public AdminDiseasePage()
        {
            InitializeComponent();
        }
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSymptoms();
        }
        private async System.Threading.Tasks.Task LoadSymptoms()
        {
            try
            {
                var dbSymptoms = await diseaseRepo.GetAllSymptomsAsync();

                symptomsViewModel = dbSymptoms.Select(s => new SymptomSelectViewModel
                {
                    Id = s.SymptomId,
                    Name = s.Name,
                    IsSelected = false
                }).ToList();

                SymptomsListControl.ItemsSource = symptomsViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження: {ex.Message}");
            }
        }
        private async void AddSymptom_Click(object sender, RoutedEventArgs e)
        {
            string name = NewSymptomBox.Text.Trim();
            if (string.IsNullOrEmpty(name)) return;

            try
            {
                int newId = await diseaseRepo.AddSymptomAsync(name);

                NewSymptomBox.Text = "";

                await LoadSymptoms();

                var newItem = symptomsViewModel.FirstOrDefault(s => s.Id == newId);
                if (newItem != null) newItem.IsSelected = true;

                SymptomsListControl.ItemsSource = null;
                SymptomsListControl.ItemsSource = symptomsViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка додавання симптому: {ex.Message}");
            }
        }

        private async void SaveDisease_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DiseaseNameBox.Text) || string.IsNullOrWhiteSpace(TreatmentBox.Text))
            {
                MessageBox.Show("Заповніть назву та лікування!");
                return;
            }

            var selectedIds = symptomsViewModel
                .Where(s => s.IsSelected)
                .Select(s => s.Id)
                .ToList();

            if (selectedIds.Count == 0)
            {
                MessageBox.Show("Оберіть хоча б один симптом!");
                return;
            }

            try
            {
                var newDisease = new Disease
                {
                    Name = DiseaseNameBox.Text,
                    Treatment = TreatmentBox.Text
                };

                await diseaseRepo.CreateDiseaseWithSymptomsAsync(newDisease, selectedIds);

                MessageBox.Show("Хворобу успішно створено!");

                DiseaseNameBox.Text = "";
                TreatmentBox.Text = "";
                foreach (var s in symptomsViewModel) s.IsSelected = false;

                SymptomsListControl.ItemsSource = null;
                SymptomsListControl.ItemsSource = symptomsViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка збереження: {ex.Message}");
            }
        }
    }
}
