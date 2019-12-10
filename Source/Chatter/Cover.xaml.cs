using Chatter.Implementations;
using Mikodev.Links;
using Mikodev.Optional;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
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
            IsEnabled = false;
            var exists = File.Exists(settingsPath);
            var result = exists
                ? await TryAsync(UsingAsync(() => new StreamReader(settingsPath, Encoding.UTF8), LinkSettings.CreateAsync))
                : Ok<LinkSettings, Exception>(default);
            if (exists && result.IsError())
                _ = MessageBox.Show(result.UnwrapError().Message, "Error while loading settings", MessageBoxButton.OK, MessageBoxImage.Error);
            var context = new SynchronizationUIContext(TaskScheduler.FromCurrentSynchronizationContext(), Dispatcher);
            client = new LinkClient(result.UnwrapOrDefault() ?? LinkSettings.Create(), context);
            if (exists == false || result.IsError())
                client.Profile.Name = string.Concat(Environment.UserName, "@", Environment.MachineName);
            DataContext = client.Profile;
            IsEnabled = true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(client != null);
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;
            button.IsEnabled = false;

            if (tag == "go")
            {
                var settings = client.Settings;
                var result = await TryAsync(UsingAsync(() => new StreamWriter(settingsPath, false, Encoding.UTF8), settings.SaveAsync));
                if (result.IsError())
                    _ = MessageBox.Show(result.UnwrapError().Message, "Error while saving settings", MessageBoxButton.OK, MessageBoxImage.Error);
                _ = await client.StartAsync();
                App.CurrentClient = client;
                var entrance = new Entrance();
                entrance.Show();
                Close();
            }
            else if (tag == "image")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog() { Filter = imageFilter };
                if (dialog.ShowDialog(this) == true)
                    _ = await TryAsync(() => client.SetProfileImageAsync(new FileInfo(dialog.FileName)));
            }

            button.IsEnabled = true;
        }
    }
}
