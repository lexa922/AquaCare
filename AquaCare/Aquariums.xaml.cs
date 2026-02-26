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
    /// Interaction logic for Aquariums.xaml
    /// </summary>
    public partial class Aquariums : Page
    {
        private int _UserId;
        public class AquariumViewModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string VolumeDisplay { get; set; }
            public string SubstrateName { get; set; }
            public string LightInfo { get; set; }
            public int FishCount { get; set; }
            public int PlantCount { get; set; }
            public string StatusText { get; set; }
            public string StatusColor { get; set; }
            public string WaterChangeFrequency { get; set; }
        }
        public Aquariums()
        {
            InitializeComponent();
        }
        public Aquariums(int UserId)
        {
            _UserId = UserId;
            InitializeComponent();
            LoadUserAquariums();
        }
        private async void LoadUserAquariums()
        {
            var aquaRepo = new AquariumRepository(UserSession.CurrentConnectionString);
            ICollection<Aquarium> aquariums = new List<Aquarium>();
            if(!aquariums.Any())
            {
                    var tmp = await aquaRepo.GetAquariumsByUserIdAsync(_UserId);
                    if (tmp != null)
                    {
                        aquariums = tmp;
                    }
                    if (!aquariums.Any())
                    {
                        AquariumsGreatingLabel.Content = "У вас ще не зареєстровано жодного акваріума\n" +
                        "Для початку роботи створіть новий";
                    }
                    else
                    {
                        try
                        {
                        var myAquariums = await aquaRepo.GetAquariumsWithDetailsAsync(_UserId);
                        var displayList = new List<AquariumViewModel>();

                        foreach (var tank in myAquariums)
                        {
                            int fishCount = tank.Fishes.Count(f => f.IsAlive);
                            int plantCount = tank.Plants.Sum(p => p.Quantity);

                            int recommendedDays = 7;
                            string freqDescription = "Стандарт";

                            double totalBioLoad = tank.Fishes.Where(f => f.IsAlive && f.Species != null)
                                                             .Sum(f => f.Species.BioLoadValue);

                            if (tank.Volume > 0 && totalBioLoad > 0)
                            {
                                double density = totalBioLoad / tank.Volume;

                                if (density < 0.3)
                                {
                                    recommendedDays = 14;
                                    freqDescription = "Мале навантаження";
                                }
                                else if (density >= 0.3 && density <= 0.8)
                                {
                                    recommendedDays = 7;
                                    freqDescription = "Оптимально";
                                }
                                else if (density > 0.8 && density <= 1.2)
                                {
                                    recommendedDays = 5;
                                    freqDescription = "Високе навантаження";
                                }
                                else
                                {
                                    recommendedDays = 3;
                                    freqDescription = "Перенаселення!";
                                }
                            }

                            string frequencyDisplay = $"{recommendedDays} дн. ({freqDescription})";

                            var lastChange = tank.WaterChanges
                                                 .OrderByDescending(wc => wc.ChangeDate)
                                                 .FirstOrDefault();

                            string statusText = "OK";
                            string statusColor = "#4CAF50";

                            if (lastChange == null)
                            {
                                statusText = "Немає даних";
                                statusColor = "#9E9E9E";
                            }
                            else
                            {
                                DateOnly today = DateOnly.FromDateTime(DateTime.Now);
                                int daysSinceChange = today.DayNumber - lastChange.ChangeDate.DayNumber;

                                int criticalLimit = (int)(recommendedDays * 1.5);

                                if (daysSinceChange > criticalLimit)
                                {
                                    statusText = $"Прострочено ({daysSinceChange} дн.)";
                                    statusColor = "#F44336";
                                }
                                else if (daysSinceChange >= recommendedDays)
                                {
                                    statusText = "Час підміни";
                                    statusColor = "#FFC107";
                                }
                                else
                                {
                                    statusText = "OK";
                                    statusColor = "#4CAF50";
                                }
                            }

                            displayList.Add(new AquariumViewModel
                            {
                                Id = tank.AquariumId,
                                Name = tank.Name,
                                VolumeDisplay = $"{tank.Volume} л",
                                SubstrateName = tank.Substrate?.Name ?? "Не обрано",
                                LightInfo = $"{tank.LightOnTime:HH:mm} - {tank.LightOffTime:HH:mm}",
                                FishCount = fishCount,
                                PlantCount = plantCount,

                                StatusText = statusText,
                                StatusColor = statusColor,
                                WaterChangeFrequency = frequencyDisplay
                            });
                        }

                        AquariumsList.ItemsSource = displayList;
                    }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Помилка: {ex.Message}");
                        }
                    }
            }
        }
        private void AquariumCard_Click(object sender, MouseButtonEventArgs e)
        {
            var card = sender as AquariumCard;
            var viewModel = card.DataContext as AquariumViewModel;

            Frame mainFrame = Application.Current.MainWindow.FindName("MainFrame") as Frame;
            if (mainFrame != null)
            {
                mainFrame.Navigate(new AquariumDetailPage(viewModel.Id));
            }
        }
    }
}
