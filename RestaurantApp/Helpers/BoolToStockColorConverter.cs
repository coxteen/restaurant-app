using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantApp.Helpers
{
    public class BoolToStockColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isLowStock)
            {
                return isLowStock ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Green);
            }
            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}