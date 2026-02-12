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
using Microsoft.EntityFrameworkCore;
using System.IO;
using AquaCare.Repositories;
using AquaCareClasses;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for FullFishInfo.xaml
    /// </summary>
    public partial class FullFishInfo : Page
    {
        public FullFishInfo()
        {
            InitializeComponent();
        }
        private int _speciesId;

        public FullFishInfo(int speciesId) {
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
            var fishRepo=new FishRepository(UserSession.CurrentConnectionString);
            var species = await fishRepo.GetSpeciesByIdAsync(_speciesId);

                if (species != null)
                {
                    SpeciesName.Content = species.SpeciesName;
                    SpeciesDesc.Text = species.SpeciesDescription;
                    MinTank.Text = species.MinTankSize+" літрів для найменшої зграйки";
                    MinGroup.Text=species.MinGroupSize.ToString();
                    BioLoad.Text=species.BioLoadValue.ToString();

                    var imageSources = new List<DisplayImage>();
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    foreach (var img in species.Images)
                    {
                        try
                        {
                            string fullPath = Path.Combine(appDirectory, img.ImagePath);

                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                            bitmap.EndInit();

                            imageSources.Add(new DisplayImage { Source = bitmap });
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                    GalleryItemsControl.ItemsSource = imageSources;
                }
            }
        }
}
