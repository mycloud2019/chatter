using Chatter.Interop;
using Chatter.Pages;
using Chatter.Windows;
using Mikodev.Links;
using Mikodev.Links.Annotations;
using Mikodev.Links.Messages;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace Chatter
{
    public partial class Entrance : Window
    {
        private readonly Client client;

        public Entrance()
        {
            InitializeComponent();
            client = App.CurrentClient;
            DataContext = client;
            Loaded += Window_Loaded;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            Debug.Assert(client != null);
            client.NewFileReceiver += x => new FileWindow(x).Show();
            client.NewDirectoryReceiver += x => new DirectoryWindow(x).Show();

            void NewMessageHandler(object s, MessageEventArgs message)
            {
                var helper = new WindowInteropHelper(this);
                if (IsActive == false)
                    _ = NativeMethods.FlashWindow(helper.Handle, true);
                message.IsHandled = App.CurrentProfile == message.Profile;
            }
            client.NewMessage += NewMessageHandler;
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var current = e.AddedItems.Cast<Profile>().FirstOrDefault();
            if (current != null)
                current.UnreadCount = 0;
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
            ObservableCollection<Profile> Filter(string input)
            {
                Debug.Assert(input.ToUpperInvariant() == input);
                var result = new ObservableCollection<Profile>();
                foreach (var item in client.Profiles)
                    if (item.Name.ToUpperInvariant().Contains(input) || item.Text.ToUpperInvariant().Contains(input))
                        result.Add(item);
                return result;
            }

            Debug.Assert(sender == searchBox);
            var text = searchBox.Text;
            var flag = string.IsNullOrEmpty(text);
            var list = flag ? (ObservableCollection<Profile>)client.Profiles : Filter(text.ToUpperInvariant());

            clientTextBlock.Text = flag ? "All" : $"Search '{text}'";
            clientListBox.ItemsSource = list;
            clientListBox.SelectedIndex = list.IndexOf(App.CurrentProfile);
        }

        private void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount != 2)
                return;
            new Cover { Owner = this }.Show();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;

            if (tag == "check")
            {
                void OpenDirectory()
                {
                    var directory = client.ReceivingDirectory;
                    if (!directory.Exists)
                        return;
                    using (Process.Start("explorer", "/e," + directory.FullName)) { }
                }
                _ = Task.Run(OpenDirectory);
            }
            else if (tag == "clean")
            {
                client.CleanProfiles();
                if (!client.Profiles.Contains(App.CurrentProfile))
                    clientListBox.SelectedIndex = -1;
            }
        }
    }
}
