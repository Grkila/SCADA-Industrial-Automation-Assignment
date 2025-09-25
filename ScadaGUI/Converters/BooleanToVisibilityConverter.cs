using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ScadaGUI.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Ako je vrednost 'true', vrati 'Visible', inače 'Collapsed'
            return (value is bool b && b) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Nije nam potrebno za ovaj slučaj, ali mora da stoji tu zbog komplajlera.
            throw new NotImplementedException();
        }
    }
}