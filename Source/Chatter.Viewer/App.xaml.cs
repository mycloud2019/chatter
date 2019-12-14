using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Mikodev.Links;
using System;
using System.Linq;
using System.Reflection;

namespace Chatter.Viewer
{
    public class App : Application
    {
        private LinkClient client;

        private LinkProfile profile;

        public static LinkClient CurrentClient
        {
            get => ((App)Current).client;
            set => ((App)Current).client = value;
        }

        public static LinkProfile CurrentProfile
        {
            get => ((App)Current).profile;
            set => ((App)Current).profile = value;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            static FieldInfo FindFieldInfo(Type type, string name)
            {
                if (type == null)
                    return null;
                var info = type.GetField(name);
                if (info != null)
                    return info;
                return FindFieldInfo(type.BaseType, name);
            }

            var fonts = FontFamily.SystemFontFamilies;
            var font = fonts.FirstOrDefault(x => x.Name == "Consolas");
            if (font is null)
                return;
            var styles = this.Styles.OfType<Style>().ToList();
            foreach (var style in styles)
            {
                var field = FindFieldInfo(style.Selector.TargetType, "FontFamilyProperty");
                var property = (AvaloniaProperty)field.GetValue(null);
                style.Setters.Add(new Setter { Property = property, Value = font });
            }
        }
    }
}
