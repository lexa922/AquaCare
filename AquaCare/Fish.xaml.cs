using AquaClasses;
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
using System.IO;
using Microsoft.EntityFrameworkCore;
using AquaCare.Repositories;
using AquaCareClasses;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for Fish.xaml
    /// </summary>
    public partial class Fish : Page
    {
        public class FishCardData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string CoverPath { get; set; }
        }

        public Fish()
        {
            InitializeComponent();
            LoadFishData();
        }

        private async void LoadFishData()
        {
            try
            {
                var fishCardsList = new List<FishCardData>();
                var fishRepo = new FishRepository(UserSession.CurrentConnectionString);

                var allFishSpecies = await fishRepo.GetAllSpeciesAsync();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var species in allFishSpecies)
                {
                    string finalPath = Path.Combine(baseDir, "FishSpeciesImg", "default.png");

                    if (!string.IsNullOrEmpty(species.ImagePath))
                    {
                        string potentialPath = Path.Combine(baseDir, species.ImagePath);

                        if (File.Exists(potentialPath))
                        {
                            finalPath = potentialPath;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Не знайдено файл: {potentialPath}");
                        }
                    }

                    fishCardsList.Add(new FishCardData
                    {
                        Id = species.FishSpecieId,
                        Name = species.SpeciesName,
                        Description = species.SpeciesDescription,
                        CoverPath = finalPath
                    });
                }

                FishItemsControl.ItemsSource = fishCardsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка: {ex.Message}");
            }
        }
        private void SpeciesCard_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SpeciesCard clickedCard = sender as SpeciesCard;
            if (clickedCard == null) return;

            FishCardData cardData = clickedCard.DataContext as FishCardData;
            if (cardData == null) return;
            int speciesId = cardData.Id;
            Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new FullFishInfo(speciesId));
            }
        }
    }
}
