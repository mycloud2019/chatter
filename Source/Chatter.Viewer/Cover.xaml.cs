﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Chatter.Viewer.Implementations;
using Mikodev.Links;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data;
using Mikodev.Optional;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using static Mikodev.Optional.Extensions;

namespace Chatter.Viewer
{
    public class Cover : Window
    {
        private static readonly List<FileDialogFilter> filters = new List<FileDialogFilter> { new FileDialogFilter { Extensions = new List<string> { "bmp", "jpg", "png" }, Name = "Image File" } };

        private static readonly string settingsPath = $"{nameof(Chatter)}.settings.json";

        private IClient client = null;

        public Cover()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            Opened += Window_Opened;
            Closed += Window_Closed;
        }

        private async void Window_Opened(object sender, EventArgs e)
        {
            _ = AddHandler(Button.ClickEvent, Button_Click);

            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            var exists = File.Exists(settingsPath);
            var result = exists
                ? await TryAsync(() => LinkFactory.CreateSettingsAsync(settingsPath))
                : Ok<ISettings, Exception>(default);
            if (exists && result.IsError())
                await Notice.ShowDialog(this, result.UnwrapError().Message, "Error");
            var store = new SqliteStorage($"{nameof(Chatter)}.db");
            var context = new SynchronizationDispatcher(Dispatcher.UIThread);
            client = LinkFactory.CreateClient(result.UnwrapOrDefault() ?? LinkFactory.CreateSettings(), context, store);
            client.Profile.Name = string.Concat(Environment.UserName, "@", Environment.MachineName);
            DataContext = client.Profile;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            RemoveHandler(Button.ClickEvent, Button_Click);
        }

        private async void Button_Click(object sender, RoutedEventArgs args)
        {
            var button = (Button)args.Source;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "go")
            {
                App.CurrentClient = client;
                var settings = client.Settings;
                var source = await TryAsync(() => settings.SaveAsync(settingsPath));
                if (source.IsError())
                    await Notice.ShowDialog(this, source.UnwrapError().Message, "Error");
                var result = Try(() => client.Start());
                if (result.IsError())
                    await Notice.ShowDialog(this, result.UnwrapError().Message, "Error");
                else
                    new Entrance().Show();
                Close();
            }
            else if (tag == "image")
            {
                var dialog = new OpenFileDialog() { AllowMultiple = false, Filters = filters };
                var target = await dialog.ShowAsync(this);
                var result = target.FirstOrDefault();
                if (string.IsNullOrEmpty(result))
                    return;
                _ = await TryAsync(() => client.SetProfileImageAsync(result));
            }
        }
    }
}
