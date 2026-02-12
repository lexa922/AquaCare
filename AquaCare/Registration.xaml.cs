using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
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
    /// Interaction logic for Registration.xaml
    /// </summary>
    public partial class Registration : Page
    {
        public Registration()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Text = null;
            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox1.Password.Trim();
            string passwordRepeat = PasswordBox2.Password.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordRepeat))
            {
                ErrorTextBlock.Text = "Будь ласка, заповність кожне з полів";
                return;
            }
            if (password != passwordRepeat)
            {
                ErrorTextBlock.Text = "Паролі мають збігатись";
                return;
            }
            try
            {
                var userRepo = new UserRepository("Server=MAIN;Database=AquaCareDB;User Id=Aqua_Admin_Login;Password=StrongPass123!;TrustServerCertificate=True;");

                if (await userRepo.IsUsernameTakenAsync(username))
                {
                    ErrorTextBlock.Text = "Це ім'я користувача вже зайняте";
                    return;
                }
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                await userRepo.RegisterUserAsync(username, passwordHash);
                MessageBox.Show("Реєстрація успішна! Тепер ви можете увійти.");
                Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
                if (mainFrame != null)
                {
                    mainFrame.Navigate(new Login());
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show($"Помилка реєстрації: {ex.Message}");
            }
        }
    }
}

