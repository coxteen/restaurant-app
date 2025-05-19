using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace RestaurantApp.Helpers
{
    public class OrderStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string currentStatus && parameter is string expectedStatus)
            {
                // Special case for "Active" which means not "Delivered" or "Cancelled"
                if (expectedStatus == "Active")
                {
                    return (currentStatus != "Delivered" && currentStatus != "Cancelled")
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }

                return currentStatus == expectedStatus ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}