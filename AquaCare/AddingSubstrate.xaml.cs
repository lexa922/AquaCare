using AquaCare.Repositories;
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
    /// Interaction logic for AddingSubstrate.xaml
    /// </summary>
    public partial class AddingSubstrate : Page
    {
        internal class ColorOption
        {
            public string Name { get; set; }
            public string DbValue { get; set; }
            public Brush ColorBrush { get; set; }
        }
        private List<ColorOption> availableColors;
        private string selectedColorName = null;
        public AddingSubstrate()
        {
            InitializeComponent();
        }
        List<SubstrateType> types;
        SubstrateRepository subsRepo = new SubstrateRepository(UserSession.CurrentConnectionString);
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            availableColors = new List<ColorOption>
            {
               new ColorOption { Name = "Чорний", DbValue = "Black", ColorBrush = Brushes.Black },
               new ColorOption { Name = "Білий",  DbValue = "White", ColorBrush = Brushes.White },
               new ColorOption { Name = "Сірий",  DbValue = "Gray",  ColorBrush = Brushes.Gray },
               new ColorOption { Name = "Червоний", DbValue = "Red", ColorBrush = Brushes.Red },
               new ColorOption { Name = "Коричневий", DbValue = "#A52A2A", ColorBrush = Brushes.Brown }
            };
            ColorPalette.ItemsSource = availableColors;
            types = await subsRepo.GetSubstrateTypesAsync();

            SubstrateType.ItemsSource = types;
            
            SubstrateSize.ItemsSource = SubstrateSizes.GetAll();
        }
            
        private void ColorRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Border clickedBorder = sender as Border;
            if (clickedBorder == null) return;
            ColorOption selectedColor = clickedBorder.DataContext as ColorOption;
            if (selectedColor == null) return;
            selectedColorName = selectedColor.DbValue;
            foreach (var item in ColorPalette.Items)
            {
                DependencyObject container = ColorPalette.ItemContainerGenerator.ContainerFromItem(item);
                Border border = FindVisualChild<Border>(container);

                if (border != null)
                {
                    border.BorderThickness = new Thickness(1);
                    border.BorderBrush = Brushes.Gray;
                }
            }
            clickedBorder.BorderThickness = new Thickness(3);
            clickedBorder.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF46A1FF"));

        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(SubstrateName.Text) || string.IsNullOrEmpty(SubstrateType.Text) 
                || selectedColorName == null || string.IsNullOrEmpty(SubstrateSize.Text))
            {
                MessageBox.Show("Помилка! Заповність всі поля", "Помилка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                    var newSubstrate = new Substrate
                    {
                        Name = SubstrateName.Text,
                        SubstrateTypeId = types[SubstrateType.SelectedIndex].SubstrateTypeId,
                        Color = selectedColorName,
                        Size = SubstrateSize.Text
                    };
                    await subsRepo.AddSubstrateAsync(newSubstrate);
                    SubstrateName.Clear();
                    SubstrateType.SelectedItem = null;
                    SubstrateSize.SelectedItem = null;
            }
            
        }
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
    }
}
