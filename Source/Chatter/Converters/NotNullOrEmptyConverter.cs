using System;
using System.Globalization;
using System.Windows.Data;

namespace Chatter.Converters
{
    internal sealed class NotNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string data)
                return !string.IsNullOrEmpty(data);
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
