using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Chatter.Converters
{
    internal sealed class HalfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var mode = parameter as string;
            if (value is double number)
            {
                var result = number / 2;
                switch (mode)
                {
                    case "double":
                        return result;

                    default:
                        return new CornerRadius(result);
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
