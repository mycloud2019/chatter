using Chatter.Interop;
using Chatter.Pages;
using Chatter.Windows;
using Mikodev.Links;
using Mikodev.Links.Messages;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Chatter
{
    public partial class Entrance : Window
    {
        public Entrance()
        {
            InitializeComponent();
            Loaded += Entrance_Loaded;
        }

        private void Entrance_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            var client = App.CurrentClient;
            Debug.Assert(client != null);
            client.NewFileReceiver += x => new FileWindow(x).Show();
            client.NewDirectoryReceiver += x => new DirectoryWindow(x).Show();

            void handler(object s, MessageEventArgs message)
            {
                Debug.Assert(App.CurrentClient == s);
                var helper = new WindowInteropHelper(this);
                if (IsActive == false)
                    _ = NativeMethods.FlashWindow(helper.Handle, true);
                message.IsHandled = App.CurrentProfile == message.Profile;
            }
            client.NewMessage += handler;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var current = e.AddedItems.Cast<LinkProfile>().FirstOrDefault();
            if (current != null)
                current.Hint = 0;
            App.CurrentProfile = current;
            dialogFrame.Content = current == null ? null : new Dialog();
        }

        private void Frame_Navigated(object sender, NavigationEventArgs e)
        {
            var frame = (Frame)sender;
            while (frame.CanGoBack)
                _ = frame.RemoveBackEntry();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            BindingList<LinkProfile> Filter(string input)
            {
                Debug.Assert(input.ToUpperInvariant() == input);
                var result = new BindingList<LinkProfile>();
                foreach (var item in App.CurrentClient.ProfileCollection)
                    if (item.Name.ToUpperInvariant().Contains(input) || item.Text.ToUpperInvariant().Contains(input))
                        result.Add(item);
                return result;
            }

            Debug.Assert(sender == searchBox);
            var client = App.CurrentClient;
            Debug.Assert(client != null);
            var text = searchBox.Text;
            var flag = string.IsNullOrEmpty(text);
            var list = flag ? client.ProfileCollection : Filter(text.ToUpperInvariant());

            clientTextBlock.Text = flag ? "All" : $"Search '{text}'";
            clientListBox.ItemsSource = list;
            clientListBox.SelectedIndex = list.IndexOf(App.CurrentProfile);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;
            var client = App.CurrentClient;

            if (tag == "check")
            {
                void OpenDirectory()
                {
                    var directory = client.ReceiveDirectory;
                    if (!directory.Exists)
                        return;
                    using (Process.Start("explorer", "/e," + directory.FullName)) { }
                }
                _ = Task.Run(OpenDirectory);
            }
            else if (tag == "clean")
            {
                client.CleanProfileCollection();
                if (!client.ProfileCollection.Contains(App.CurrentProfile))
                    clientListBox.SelectedIndex = -1;
            }
        }
    }
}
