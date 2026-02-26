using AquaClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
using Microsoft.EntityFrameworkCore;
using System.IO;
using AquaCareClasses;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for FullPlantInfo.xaml
    /// </summary>
    public partial class FullPlantInfo : Page
    {
        public FullPlantInfo()
        {
            InitializeComponent();
        }
        private int _speciesId;

        public FullPlantInfo(int speciesId)
        {
            InitializeComponent();
            _speciesId = speciesId;
            LoadSpeciesDetails();
        }
        public class DisplayImage
        {
            public BitmapImage Source { get; set; }
        }
        private async void LoadSpeciesDetails()
        {
            try
            {
                var plantRepo = new PlantRepository(UserSession.CurrentConnectionString);
                var species = await plantRepo.GetSpeciesByIdAsync(_speciesId);

                if (species != null)
                {
                    SpeciesName.Content = species.SpeciesName;
                    SpeciesDesc.Text = species.SpeciesDescription;
                    LightDuration.Content = species.LightDurationHrs;
                    LightIntensity.Content = species.LightLevelName;
                    CompatibleSubstrates.Text = species.CompatibleSubstrates;

                    var imageSources = new List<DisplayImage>();
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;

                    foreach (var img in species.Images)
                    {
                        try
                        {
                            string fullPath = img.ImagePath;
                            if (!Path.IsPathRooted(fullPath))
                            {
                                fullPath = Path.Combine(appDirectory, img.ImagePath);
                            }

                            if (File.Exists(fullPath))
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                                bitmap.EndInit();

                                imageSources.Add(new DisplayImage { Source = bitmap });
                            }
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    GalleryItemsControl.ItemsSource = imageSources;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Помилка завантаження деталей: {ex.Message}");
            }
        }
    }
}
