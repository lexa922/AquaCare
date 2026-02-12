using AquaCareClasses;
using Azure;
using System.Text;
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
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new Login());
            SpeciesAll.Visibility = Visibility.Collapsed;
            Aquariums.Visibility = Visibility.Collapsed;
        }

        private void SpeciesAll_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AllSpecies());
            ToMain.Visibility = Visibility.Visible;
            SpeciesAll.Visibility = Visibility.Collapsed;
            Aquariums.Visibility = Visibility.Collapsed;
            AddAquarium.Visibility = Visibility.Collapsed;

            FishSpecies.Visibility = Visibility.Visible;
            PlantSpecies.Visibility = Visibility.Visible;
            if (UserSession.UserRole != "User")
            {
                AddFish.Visibility = Visibility.Visible;
                AddPlant.Visibility = Visibility.Visible;
                AddSubstrate.Visibility = Visibility.Visible;
                AddDisease.Visibility = Visibility.Visible;
                AddCompatabilityRule.Visibility = Visibility.Visible;
            }
            if (UserSession.UserRole == "Superadmin")
            {
                ToSuperAdminPanel.Visibility = Visibility.Visible;
            }
        }

        private void ToMain_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Home());
            SpeciesAll.Visibility = Visibility.Visible;
            Aquariums.Visibility = Visibility.Visible;
            ToMain.Visibility = Visibility.Collapsed;

            FishSpecies.Visibility = Visibility.Collapsed;
            PlantSpecies.Visibility = Visibility.Collapsed;

            AddFish.Visibility = Visibility.Collapsed;
            AddPlant.Visibility = Visibility.Collapsed;
            AddSubstrate.Visibility = Visibility.Collapsed;
            AddDisease.Visibility = Visibility.Collapsed;
            AddAquarium.Visibility = Visibility.Collapsed;
            AddCompatabilityRule.Visibility= Visibility.Collapsed;
        }
        private void FishSpecies_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Fish());
        }
        private void PlantSpecies_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Plant());
        }
        private void ToFish_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Fish());
            ToFish.Visibility = Visibility.Collapsed;
        }
        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Content is FullFishInfo)
                ToFish.Visibility = Visibility.Visible;
            else
                ToFish.Visibility = Visibility.Collapsed;

            if (e.Content is Login || e.Content is Registration)
                LogoutBtn.Visibility = Visibility.Collapsed;
            else
                LogoutBtn.Visibility = Visibility.Visible;
        }
        private void AddFish_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AddingFish());
        }
        private void AddPlant_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AddingPlant());
        }
        private void AddSubstrate_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AddingSubstrate());
        }

        private void Aquariums_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Aquariums(UserSession.CurrentUser.UserId));
            AddAquarium.Visibility = Visibility.Visible;
        }

        private void AddAquarium_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CreatingAquarium());
            AddAquarium.Visibility = Visibility.Collapsed;
        }

        private void AddDisease_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new AdminDiseasePage());
        }
        private void ToSuperAdminPanel_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new SuperAdminDashboardPage());
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Ви впевнені, що хочете вийти з акаунту?", "Вихід",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                UserSession.Logout();
                Aquariums.Visibility = Visibility.Collapsed;
                SpeciesAll.Visibility = Visibility.Collapsed;
                ToSuperAdminPanel.Visibility = Visibility.Collapsed;
                MainFrame.Navigate(new Login());
            }
            while (MainFrame.CanGoBack)
            {
                MainFrame.RemoveBackEntry();
            }
        }
        private void AddCompatabilityRule_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new CompatabilityPage());
        }
    }
}
