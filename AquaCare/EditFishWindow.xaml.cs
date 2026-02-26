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
using DbFish = AquaCareClasses.Fish;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for EditFishWindow.xaml
    /// </summary>
    public partial class EditFishWindow : Window
    {
        private DbFish _fish;
        private readonly FishRepository fishRepository = new FishRepository(UserSession.CurrentConnectionString);
        public EditFishWindow(DbFish fish)
        {
            InitializeComponent();
            _fish = fish;

            NameBox.Text = _fish.FishName;

            foreach (ComboBoxItem item in GenderCombo.Items)
            {
                if (item.Tag.ToString() == _fish.Gender)
                {
                    GenderCombo.SelectedItem = item;
                    break;
                }
            }
        }
        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameBox.Text) || GenderCombo.SelectedItem == null)
            {
                MessageBox.Show("Заповніть всі поля!");
                return;
            }

            try
            {
                _fish.FishName = NameBox.Text;
                _fish.Gender = ((ComboBoxItem)GenderCombo.SelectedItem).Tag.ToString();

                await fishRepository.UpdateFishAsync(_fish);

                MessageBox.Show("Дані оновлено!");
                this.Close();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }
    }
}
