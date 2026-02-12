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
    /// Interaction logic for CompatabilityPage.xaml
    /// </summary>
    public class CompatibilityViewModel
    {
        public int RuleId { get; set; }
        public string Species1Name { get; set; }
        public string Species2Name { get; set; }
        public int Level { get; set; }
        public string Notes { get; set; }

        public string LevelText
        {
            get
            {
                return Level switch
                {
                    2 => "⛔ НЕБЕЗПЕЧНО",
                    1 => "⚠️ Увага",
                    0 => "✅ Сумісні",
                    _ => "Невідомо"
                };
            }
        }
        public string LevelColor
        {
            get
            {
                return Level switch
                {
                    2 => "#FFCDD2",
                    1 => "#FFF9C4",
                    0 => "#C8E6C9",
                    _ => "White"
                };
            }
        }
    }
    public partial class CompatabilityPage : Page
    {
        private FishRepository fishRepository=new FishRepository(UserSession.CurrentConnectionString);
        private AdminRepository adminRepository= new AdminRepository(UserSession.CurrentConnectionString);
        public CompatabilityPage()
        {
            InitializeComponent();
            LoadData();
        }
        private async void LoadData()
        {
            try
            {
                var species = await fishRepository.GetAllSpeciesAsync();

                Species1Combo.ItemsSource = species;
                Species2Combo.ItemsSource = species;

                RulesGrid.ItemsSource = await adminRepository.GetAllRulesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження: " + ex.Message);
            }
        }

        private async void AddRule_Click(object sender, RoutedEventArgs e)
        {
            if (Species1Combo.SelectedValue == null || Species2Combo.SelectedValue == null)
            {
                MessageBox.Show("Оберіть обидва види!");
                return;
            }

            try
            {
                int s1 = (int)Species1Combo.SelectedValue;
                int s2 = (int)Species2Combo.SelectedValue;

                string tagValue = ((ComboBoxItem)LevelCombo.SelectedItem).Tag.ToString();
                int level = int.Parse(tagValue);

                string note = NoteBox.Text;

                await adminRepository.AddRuleAsync(s1, s2, level, note);

                NoteBox.Clear();
                LoadData();
                MessageBox.Show("Правило додано!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка: " + ex.Message);
            }
        }

        private async void DeleteRule_Click(object sender, RoutedEventArgs e)
        {
            var rule = ((FrameworkElement)sender).DataContext as CompatibilityViewModel;
            if (rule == null) return;

            if (MessageBox.Show("Видалити правило?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                await adminRepository.DeleteRuleAsync(rule.RuleId);
                LoadData();
            }
        }
    }
}
