using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
using Microsoft.Data.SqlClient;
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

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var userRepo = new UserRepository("Server=MAIN;Database=AquaCareDB;User Id=Aqua_User_Login;Password=UserPass123!;TrustServerCertificate=True;");
            string username = UsernameTextbox.Text.Trim();
            string password = PasswordBox1.Password.Trim();

                var user = await userRepo.GetUserForLoginAsync(username);
                if (user == null)
                {
                    ErrorTextBox.Text = "Неправильний пароль або логін";
                    return;
                }
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, user.Value.PasswordHash);

            var tmpUser = user.Value.User;

            if (!isPasswordCorrect)
                {
                    ErrorTextBox.Text = "Неправильний пароль або логін";
                    return;
                }
                var roleName = await userRepo.GetUserRoleNameAsync(tmpUser.UserId);
                var builder = new SqlConnectionStringBuilder();
                builder.DataSource = "Main";
                builder.InitialCatalog = "AquaCareDB";
                builder.TrustServerCertificate = true;
                builder.Pooling = false;

                if (roleName == "Admin")
                {
                    builder.UserID = "Aqua_Admin_Login";
                    builder.Password = "StrongPass123!";
                }
                else if(roleName=="User")
                {
                    builder.UserID = "Aqua_User_Login";
                    builder.Password = "UserPass123!";
                }
            else
            {
                builder.UserID = "Aqua_SuperAdmin_Login";
                builder.Password = "SuperStrongPass123!";
            }
                string newConnectionString = builder.ConnectionString;

            UserSession.Login(tmpUser, roleName, newConnectionString);
                Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(new Home());
                    Button ToAquariums = Application.Current.MainWindow.FindName("Aquariums") as Button;
                    ToAquariums.Visibility = Visibility.Visible;
                    Button ToSpecies = Application.Current.MainWindow.FindName("SpeciesAll") as Button;
                    ToSpecies.Visibility = Visibility.Visible;
                }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
            if (mainFrame != null) 
            {
                mainFrame.Navigate(new Registration());
            }
        }
    }
}
