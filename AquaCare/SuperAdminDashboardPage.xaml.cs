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
using static AquaCare.Repositories.AdminRepository;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for SuperAdminDashboardPage.xaml
    /// </summary>
    public partial class SuperAdminDashboardPage : Page
    {
        private AdminRepository adminRepository = new AdminRepository(UserSession.CurrentConnectionString);
        public SuperAdminDashboardPage()
        {
            InitializeComponent();
            LoadData();
        }
        private async void LoadData()
        {
            try
            {
                var stats = await adminRepository.GetGlobalStatsAsync();
                TotalUsersText.Text = stats.TotalUsers.ToString();
                TotalTanksText.Text = stats.TotalTanks.ToString();

                var topFish = await adminRepository.GetTopFishSpeciesAsync();
                TopFishList.ItemsSource = topFish;

                var topPlants = await adminRepository.GetTopPlantSpeciesAsync();
                TopPlantsList.ItemsSource = topPlants;

                var diseaseStats = await adminRepository.GetTopDiseaseAsync();
                TopDiseaseName.Text = diseaseStats.Name;
                TopDiseaseCount.Text = $"{diseaseStats.Count} випадків";

                var breederStats = await adminRepository.GetTopBreedingSpeciesAsync();
                TopBreederName.Text = breederStats.Species;
                TopBreederCount.Text = $"{breederStats.TotalFry} мальків";

                int totalVol = await adminRepository.GetTotalSystemVolumeAsync();
                TotalVolumeText.Text = totalVol.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("uk-UA"));

                UsersGrid.ItemsSource = await adminRepository.GetAllUsersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка завантаження даних: " + ex.Message);
            }
        }

        private async void MakeAdmin_Click(object sender, RoutedEventArgs e)
        {
            var user = ((FrameworkElement)sender).DataContext as UserViewModel;
            if (user == null) return;

            if (MessageBox.Show($"Надати права адміністратора користувачу {user.Login}?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    await adminRepository.SetUserRoleAsync(user.UserId, "Admin");
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка: " + ex.Message);
                }
            }
        }
        private async void RemoveAdmin_Click(object sender, RoutedEventArgs e)
        {
            var user = ((FrameworkElement)sender).DataContext as UserViewModel;
            if (user == null) return;

            if (MessageBox.Show($"Забрати права адміністратора у {user.Login}?", "Підтвердження", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    await adminRepository.SetUserRoleAsync(user.UserId, "User");
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка: " + ex.Message);
                }
            }
        }

        private async void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var user = ((FrameworkElement)sender).DataContext as AdminRepository.UserViewModel;
            if (user == null) return;

            if (MessageBox.Show($"Ви впевнені, що хочете видалити {user.Login}?\nЦе видалить всі його акваріуми!", "УВАГА", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await adminRepository.DeleteUserAsync(user.UserId);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Неможливо видалити: " + ex.Message);
                }
            }
        }
    }
}
