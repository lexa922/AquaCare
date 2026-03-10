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
using AquaCare.Models;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for Plant.xaml
    /// </summary>
    public partial class Plant : Page
    {
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

                foreach (var species in allSpecies)
                {

                    plantCardsList.Add(new PlantCardData
                    {
                        Id = species.PlantSpecieId,
                        Name = species.SpeciesName,
                        Description = species.SpeciesDescription,
                        CoverPath = species.FullImagePath,
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

        private void SpeciesCard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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