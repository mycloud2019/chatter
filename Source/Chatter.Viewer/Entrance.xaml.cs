using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Chatter.Viewer.Controls;
using Mikodev.Links.Annotations;
using System;
using System.Diagnostics;
using System.Linq;

namespace Chatter.Viewer
{
    public class Entrance : Window
    {
        private Border dialog;

        private ListBox listbox;

        public Entrance()
        {
            this.InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            dialog = this.FindControl<Border>("dialog");
            listbox = this.FindControl<ListBox>("listbox");
            Opened += this.Window_Opened;
            Closed += this.Window_Closed;
        }

        private void Window_Opened(object sender, EventArgs e)
        {
            var client = App.CurrentClient;
            Debug.Assert(client != null);
            DataContext = client;
            listbox.SelectionChanged += this.ListBox_SelectionChanged;
            client.NewMessage += (s, e) => e.IsHandled = e.Profile == App.CurrentProfile;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            DataContext = null;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            dialog.Child = null;
            var profile = e.AddedItems?.OfType<Profile>()?.FirstOrDefault();
            if (profile == null)
                return;
            profile.UnreadCount = 0;
            App.CurrentProfile = profile;
            dialog.Child = new Dialog();
        }
    }
}
