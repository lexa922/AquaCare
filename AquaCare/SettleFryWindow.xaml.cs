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
using System.Windows.Shapes;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for SettleFryWindow.xaml
    /// </summary>
    public partial class SettleFryWindow : Window
    {
        private BreedingLog _currentLog;
        private int? _determinedSpeciesId = null;

        private readonly BreedingRepository breedingRepository = new BreedingRepository(UserSession.CurrentConnectionString);
        private readonly AquariumRepository aquariumRepository = new AquariumRepository(UserSession.CurrentConnectionString);
        private readonly FishRepository fishRepository = new FishRepository(UserSession.CurrentConnectionString);
        public SettleFryWindow(BreedingLog log)
        {
            InitializeComponent();
            _currentLog = log;

            LoadData();
        }
        private async void LoadData()
        {
            AquariumCombo.ItemsSource = await aquariumRepository.GetAquariumsByUserIdAsync(UserSession.CurrentUser.UserId);
            int? parentId = _currentLog.MaleFishId ?? _currentLog.FemaleFishId;
            if (parentId.HasValue)
            {
                _determinedSpeciesId = await breedingRepository.GetFishSpeciesIdAsync(parentId.Value);
            }

            if (_determinedSpeciesId.HasValue)
            {
                SpeciesCombo.Visibility = Visibility.Collapsed;
                AutoSpeciesText.Visibility = Visibility.Visible;

                AutoSpeciesText.Text = "✅ Вид визначено по батьках";
            }
            else
            {
                SpeciesCombo.Visibility = Visibility.Visible;
                AutoSpeciesText.Visibility = Visibility.Collapsed;

                SpeciesCombo.ItemsSource = await fishRepository.GetAllSpeciesAsync();

            }
        }

        private async void Settle_Click(object sender, RoutedEventArgs e)
        {
            if (AquariumCombo.SelectedValue == null)
            {
                MessageBox.Show("Будь ласка, оберіть акваріум!");
                return;
            }

            int finalSpeciesId;

            if (_determinedSpeciesId.HasValue)
            {
                finalSpeciesId = _determinedSpeciesId.Value;
            }
            else
            {
                if (SpeciesCombo.SelectedValue == null)
                {
                    MessageBox.Show("Оскільки батьки невідомі, ви мусите обрати вид вручну!");
                    return;
                }
                finalSpeciesId = (int)SpeciesCombo.SelectedValue;
            }

            if (!int.TryParse(CountBox.Text, out int count) || count <= 0)
            {
                MessageBox.Show("Введіть коректну кількість риб!");
                return;
            }

            int targetAqId = (int)AquariumCombo.SelectedValue;
            string baseName = NameBox.Text;

            try
            {
                await breedingRepository.SettleFryAsync(_currentLog.BreedingLogId, targetAqId, finalSpeciesId, count, baseName);
                MessageBox.Show($"Успішно заселено {count} риб!");
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Помилка заселення: {ex.Message}");
            }
        }
    }
}
