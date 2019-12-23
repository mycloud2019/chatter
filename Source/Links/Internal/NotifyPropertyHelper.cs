using System.Collections.Generic;
using System.ComponentModel;

namespace Mikodev.Links.Internal
{
    internal static class NotifyPropertyHelper
    {
        public static void NotifyProperty<T>(object sender, PropertyChangingEventHandler changing, PropertyChangedEventHandler changed, ref T location, T value, string property)
        {
            changing?.Invoke(sender, new PropertyChangingEventArgs(property));
            if (EqualityComparer<T>.Default.Equals(location, value))
                return;
            location = value;
            changed?.Invoke(sender, new PropertyChangedEventArgs(property));
        }
    }
}
