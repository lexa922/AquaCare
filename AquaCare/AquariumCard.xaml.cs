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
using static AquaCare.Aquariums;

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for AquariumCard.xaml
    /// </summary>
    public partial class AquariumCard : UserControl
    {
        public AquariumCard()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty AquariumNameProperty =
            DependencyProperty.Register("AquariumName", typeof(string), typeof(AquariumCard), new PropertyMetadata("Мій Акваріум"));
        public string AquariumName
        {
            get { return (string)GetValue(AquariumNameProperty); }
            set { SetValue(AquariumNameProperty, value); }
        }

        public static readonly DependencyProperty VolumeTextProperty =
            DependencyProperty.Register("VolumeText", typeof(string), typeof(AquariumCard), new PropertyMetadata("0 л"));
        public string VolumeText
        {
            get { return (string)GetValue(VolumeTextProperty); }
            set { SetValue(VolumeTextProperty, value); }
        }

        public static readonly DependencyProperty SubstrateTextProperty =
            DependencyProperty.Register("SubstrateText", typeof(string), typeof(AquariumCard), new PropertyMetadata("Не вказано"));
        public string SubstrateText
        {
            get { return (string)GetValue(SubstrateTextProperty); }
            set { SetValue(SubstrateTextProperty, value); }
        }

        public static readonly DependencyProperty LightScheduleProperty =
            DependencyProperty.Register("LightSchedule", typeof(string), typeof(AquariumCard), new PropertyMetadata("-"));
        public string LightSchedule
        {
            get { return (string)GetValue(LightScheduleProperty); }
            set { SetValue(LightScheduleProperty, value); }
        }
        public static readonly DependencyProperty FishCountTextProperty =
    DependencyProperty.Register("FishCountText", typeof(string), typeof(AquariumCard), new PropertyMetadata("0"));
        public string FishCountText
        {
            get { return (string)GetValue(FishCountTextProperty); }
            set { SetValue(FishCountTextProperty, value); }
        }

        public static readonly DependencyProperty PlantCountTextProperty =
            DependencyProperty.Register("PlantCountText", typeof(string), typeof(AquariumCard), new PropertyMetadata("0"));
        public string PlantCountText
        {
            get { return (string)GetValue(PlantCountTextProperty); }
            set { SetValue(PlantCountTextProperty, value); }
        }

        public static readonly DependencyProperty StatusTextProperty =
            DependencyProperty.Register("StatusText", typeof(string), typeof(AquariumCard), new PropertyMetadata("-"));
        public string StatusText
        {
            get { return (string)GetValue(StatusTextProperty); }
            set { SetValue(StatusTextProperty, value); }
        }

        public static readonly DependencyProperty StatusColorBrushProperty =
            DependencyProperty.Register("StatusColorBrush", typeof(Brush), typeof(AquariumCard), new PropertyMetadata(Brushes.Gray));
        public Brush StatusColorBrush
        {
            get { return (Brush)GetValue(StatusColorBrushProperty); }
            set { SetValue(StatusColorBrushProperty, value); }
        }
        public static readonly DependencyProperty WaterFreqTextProperty =
    DependencyProperty.Register("WaterFreqText", typeof(string), typeof(AquariumCard), new PropertyMetadata("7 днів"));

        public string WaterFreqText
        {
            get { return (string)GetValue(WaterFreqTextProperty); }
            set { SetValue(WaterFreqTextProperty, value); }
        }

        private void AquariumCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
