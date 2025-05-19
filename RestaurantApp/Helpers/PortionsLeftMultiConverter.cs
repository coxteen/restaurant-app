using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Helpers
{
    public class PortionsLeftMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int portionSize && values[1] is int totalQuantity)
            {
                if (portionSize <= 0) return "0 portions";
                int portions = totalQuantity / portionSize;
                return $"{portions} portion{(portions != 1 ? "s" : "")} left";
            }
            return "0 portions";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}