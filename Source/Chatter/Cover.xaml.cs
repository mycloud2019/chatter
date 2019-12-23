using Chatter.Implementations;
using Mikodev.Links;
using Mikodev.Links.Abstractions;
using Mikodev.Links.Data;
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
        private static readonly string ImageFileFilter = "Image Files (*.bmp, *.jpg, *.png) | *.bmp; *.jpg; *.png";

        private static readonly string SettingsPath = $"{nameof(Chatter)}.settings.json";

        private IClient client;

        public Cover()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            static async Task<IClient> CreateClient()
            {
                var exists = File.Exists(SettingsPath);
                var result = exists
                    ? await TryAsync(() => LinkFactory.CreateSettingsAsync(SettingsPath))
                    : Ok<ISettings, Exception>(default);
                if (exists && result.IsError())
                    _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                var store = new SqliteStorage($"{nameof(Chatter)}.db");
                await store.InitializeAsync();
                var context = new SynchronizationDispatcher(TaskScheduler.FromCurrentSynchronizationContext(), Application.Current.Dispatcher);
                var client = LinkFactory.CreateClient(result.UnwrapOrDefault() ?? LinkFactory.CreateSettings(), context, store);
                if (exists == false || result.IsError())
                    client.Profile.Name = $"{Environment.UserName}@{Environment.MachineName}";
                return client;
            }

            this.IsEnabled = false;
            using var _0 = Disposable.Create(() => this.IsEnabled = true);

            client = App.CurrentClient ?? await CreateClient();
            DataContext = client.Profile;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            static async Task SaveSettings(ISettings settings)
            {
                var result = await TryAsync(() => settings.SaveAsync(SettingsPath));
                if (result.IsOk())
                    return;
                _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            static bool StartClient(IClient client)
            {
                var result = Try(() => client.Start());
                if (result.IsError())
                    _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return result.IsOk();
            }

            Debug.Assert(client != null);
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;

            button.IsEnabled = false;
            using var _0 = Disposable.Create(() => button.IsEnabled = true);

            if (tag == "go")
            {
                var isnull = App.CurrentClient is null;
                App.CurrentClient = client;
                await SaveSettings(client.Settings);
                if (isnull && StartClient(client))
                    new Entrance().Show();
                Close();
            }
            else if (tag == "image")
            {
                var dialog = new Microsoft.Win32.OpenFileDialog() { Filter = ImageFileFilter };
                if (dialog.ShowDialog(this) == true)
                    _ = await TryAsync(() => client.SetProfileImageAsync(dialog.FileName));
            }
        }
    }
}
