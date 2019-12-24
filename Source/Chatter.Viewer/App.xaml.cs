﻿using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Mikodev.Links.Abstractions;
using System;
using System.Linq;
using System.Reflection;

namespace Chatter.Viewer
{
    public class App : Application
    {
        private IClient client;

        private Profile profile;

        public static IClient CurrentClient
        {
            get => ((App)Current).client;
            set => ((App)Current).client = value;
        }

        public static Profile CurrentProfile
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

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
                desktop.MainWindow = new Cover();
            base.OnFrameworkInitializationCompleted();
        }
    }
}
