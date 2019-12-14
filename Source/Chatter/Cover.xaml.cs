using Chatter.Implementations;
using Mikodev.Links;
using Mikodev.Optional;
using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static Mikodev.Optional.Extensions;

namespace Chatter
{
    public partial class Cover : Window
    {
        private static readonly string imageFilter = "Image Files (*.bmp, *.jpg, *.png) | *.bmp; *.jpg; *.png";

        private static readonly string settingsPath = $"{nameof(Chatter)}.settings.json";

        private LinkClient client = default;

        public Cover()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            var exists = File.Exists(settingsPath);
            var result = exists
                ? await TryAsync(() => LinkSettings.LoadAsync(settingsPath))
                : Ok<LinkSettings, Exception>(default);
            if (exists && result.IsError())
                _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            client = new LinkClient(result.UnwrapOrDefault() ?? LinkSettings.Create(), new SynchronizationUIContext(TaskScheduler.FromCurrentSynchronizationContext(), Dispatcher));
            if (exists == false || result.IsError())
                client.Profile.Name = string.Concat(Environment.UserName, "@", Environment.MachineName);
            DataContext = client.Profile;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(client != null);
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "go")
            {
                App.CurrentClient = client;
                var settings = client.Settings;
                var source = await TryAsync(() => settings.SaveAsync(settingsPath));
                if (source.IsError())
                    _ = MessageBox.Show(source.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                var result = await TryAsync(() => client.StartAsync());
                if (result.IsError())
                    _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    new Entrance().Show();
                Close();
            }
            else if (tag == "image")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog() { Filter = imageFilter };
                if (dialog.ShowDialog(this) == true)
                    _ = await TryAsync(() => client.SetProfileImageAsync(new FileInfo(dialog.FileName)));
            }
        }
    }
}
