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

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for PhotoGalleryWindow.xaml
    /// </summary>
    public partial class PhotoGalleryWindow : Window
    {
        public PhotoGalleryWindow(ICollection<AquaCareClasses.Image> images)
        {
            InitializeComponent();

            if (images == null || images.Count == 0)
            {
                NoPhotosText.Visibility = Visibility.Visible;
            }
            else
            {
                ImagesControl.ItemsSource = images;
            }
        }
    }
}
