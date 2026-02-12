using AquaCare.Repositories;
using AquaCareClasses;
using AquaClasses;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
using DbImage = AquaCareClasses.Image;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AddingFish.xaml
    /// </summary>
    public partial class AddingFish : Page
    {
        private List<string> selectedPhotoPaths = new List<string>();
        public AddingFish()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SpecieName.Text) || !selectedPhotoPaths.Any())
            {
                MessageBox.Show("Назва виду та хоча б одне фото є обов'язковими.");
                return;
            }
            var dbImagePaths = new List<string>();
            var newSpecies = new FishSpecie(
                    SpecieName.Text,
                    SpeciesDesc.Text,
                    int.Parse(MinTank.Text),
                    double.Parse(BioLoad.Text),
                    int.Parse(MinGroup.Text)
            );
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string imagesFolder = Path.Combine(appDirectory, "FishSpeciesImg");
                Directory.CreateDirectory(imagesFolder);

            foreach (string originalPath in selectedPhotoPaths)
            {
                string fileName = Path.GetFileName(originalPath);
                string destinationPath = Path.Combine(imagesFolder, fileName);
                File.Copy(originalPath, destinationPath, true);

                string relativePath = Path.Combine("FishSpeciesImg", fileName);

                dbImagePaths.Add(relativePath);
            }
            var fishRepo = new FishRepository(UserSession.CurrentConnectionString);

            await fishRepo.AddSpeciesAsync(newSpecies, dbImagePaths);

            MessageBox.Show("Новий вид риби та фото успішно додано!");
        }
        private void SelectPhotosButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Filter = "Image files (*.jpg;*.png;*.jpeg)|*.jpg;*.png;*.jpeg|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                selectedPhotoPaths.AddRange(openFileDialog.FileNames);
                SelectedPhotosListBox.ItemsSource = null;
                SelectedPhotosListBox.ItemsSource = selectedPhotoPaths;
            }
        }
    }
}
