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
using static AquaCare.Fish;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for Plant.xaml
    /// </summary>
    public partial class Plant : Page
    {
        public class PlantCardData
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string CoverPath { get; set; }
            public string PlantDescription { get; set; }
            public string SoilInfo { get; set; } 
            public string LightInfo { get; set; }
        }
        public Plant()
        {
            InitializeComponent();
            LoadPlantData();
        }
        private async void LoadPlantData()
        {
            try
            {
                var plantCardsList = new List<PlantCardData>();
                var plantRepo = new PlantRepository(UserSession.CurrentConnectionString);
                var allSpecies = await plantRepo.GetAllSpeciesAsync();

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                foreach (var species in allSpecies)
                {
                    string finalPath = System.IO.Path.Combine(baseDir, "PlantSpeciesImg", "default.png");
                    if (!string.IsNullOrEmpty(species.ImagePath))
                    {
                        string potentialPath = System.IO.Path.Combine(baseDir, species.ImagePath);
                        if (System.IO.File.Exists(potentialPath)) finalPath = potentialPath;
                    }

                    plantCardsList.Add(new PlantCardData
                    {
                        Id = species.PlantSpecieId,
                        Name = species.SpeciesName,
                        Description = species.SpeciesDescription,
                        CoverPath = finalPath,
                        SoilInfo = species.CompatibleSubstrates,
                        LightInfo = species.LightLevelName
                    });
                }

                PlantItemsControl.ItemsSource = plantCardsList;
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
            PlantCardData cardData = clickedCard.DataContext as PlantCardData;
            if (cardData == null) return;
            int speciesId = cardData.Id;
            Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new FullPlantInfo(speciesId));
            }
        }
    }
}