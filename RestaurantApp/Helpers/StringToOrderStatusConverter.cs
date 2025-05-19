using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantApp.Helpers
{
    public class StringToOrderStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                // Show cancel button only for orders with status "Registered" or "Preparing"
                return status == "Registered" || status == "Preparing" ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}