using Avalonia.Data.Converters;
using Mikodev.Links;
using System;
using System.Globalization;

namespace Chatter.Viewer.ValueConverters
{
    internal class LinkOnlineStatusToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value switch
            {
                LinkOnlineStatus.Online => string.Empty,
                LinkOnlineStatus.Offline => $"[{value.ToString()}]",
                _ => "<Invalid>"
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }
}
