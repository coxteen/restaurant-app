using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace RestaurantApp.Helpers
{
    public class StockToColorMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int totalQuantity && values[1] is int portionSize && portionSize > 0)
            {
                int portions = totalQuantity / portionSize;

                if (portions == 0)
                {
                    return new SolidColorBrush(Colors.Red);
                }
                else if (portions < 5)
                {
                    return new SolidColorBrush(Colors.OrangeRed);
                }
                else if (portions < 10)
                {
                    return new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    return new SolidColorBrush(Colors.Green);
                }
            }

            return new SolidColorBrush(Colors.Red);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
