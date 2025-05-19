using System;
using System.Globalization;
using System.Windows.Data;

namespace RestaurantApp.Helpers
{
    public class ProductQuantityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int productId && parameter is string paramStr)
            {
                int change = int.Parse(paramStr);
                // Return a tuple containing the product ID and the new quantity (current + change)
                // We'll handle the actual calculation in the ViewModel
                return Tuple.Create(productId, change);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}