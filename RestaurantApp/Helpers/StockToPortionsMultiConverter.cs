using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace RestaurantApp.Helpers
{
    public class StockToPortionsMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is int totalQuantity && values[1] is int portionSize && portionSize > 0)
            {
                int portions = totalQuantity / portionSize;

                if (portions == 0 && totalQuantity > 0)
                {
                    // There's some stock, but not enough for a full portion
                    return "Less than 1 portion";
                }
                else if (portions == 1)
                {
                    return "1 portion left";
                }
                else if (portions > 20)
                {
                    return "In stock";
                }
                else
                {
                    return $"{portions} portions left";
                }
            }

            return "Out of stock";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
