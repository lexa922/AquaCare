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

namespace AquaCare
{
    /// <summary>
    /// Interaction logic for SpeciesCard.xaml
    /// </summary>
    public partial class SpeciesCard : UserControl
    {
        public SpeciesCard()
        {
            InitializeComponent();
        }
        public static readonly DependencyProperty CardImagePathProperty =
            DependencyProperty.Register(
                "CardImagePath",
                typeof(string),
                typeof(SpeciesCard),
                new PropertyMetadata(null, OnImagePathChanged));

        public static readonly DependencyProperty CardColorProperty =
            DependencyProperty.Register("CardColor", typeof(Brush), typeof(SpeciesCard), new PropertyMetadata(null));
        public Brush CardColor
        {
            get { return (Brush)GetValue(CardColorProperty); }
            set { SetValue(CardColorProperty, value); }
        }

        public string CardImagePath
        {
            get { return (string)GetValue(CardImagePathProperty); }
            set { SetValue(CardImagePathProperty, value); }
        }
        private static void OnImagePathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SpeciesCard card = (SpeciesCard)d;
            string relativePath = e.NewValue as string;

            if (!string.IsNullOrEmpty(relativePath))
            {
                try
                {
                    string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                    string fullPath = Path.Combine(appDirectory, relativePath);

                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
                    bitmap.EndInit();
                    card.CardImageElement.Source = bitmap;
                }
                catch (Exception)
                {
                    card.CardImageElement.Source = null;
                }
            }
            else
            {
                card.CardImageElement.Source = null;
            }
        }
        public static readonly DependencyProperty CardTitleProperty =
            DependencyProperty.Register("CardTitle", typeof(string), typeof(SpeciesCard), new PropertyMetadata("Заголовок"));
        public string CardTitle
        {
            get { return (string)GetValue(CardTitleProperty); }
            set { SetValue(CardTitleProperty, value); }
        }
        public static readonly DependencyProperty CardDescriptionProperty =
            DependencyProperty.Register("CardDescription", typeof(string), typeof(SpeciesCard), new PropertyMetadata("Опис..."));

        public string CardDescription
        {
            get { return (string)GetValue(CardDescriptionProperty); }
            set { SetValue(CardDescriptionProperty, value); }
        }
        public static readonly DependencyProperty AdditionalContentProperty =
            DependencyProperty.Register("AdditionalContent", typeof(object), typeof(SpeciesCard), new PropertyMetadata(null));

        public object AdditionalContent
        {
            get { return (object)GetValue(AdditionalContentProperty); }
            set { SetValue(AdditionalContentProperty, value); }
        }
        public static readonly DependencyProperty PictureSizeProperty =
            DependencyProperty.Register("PictureSize", typeof(double), typeof(SpeciesCard), new PropertyMetadata(220.0));
    }
}
