﻿using Chatter.Internal;
using Chatter.Windows;
using Mikodev.Links;
using Mikodev.Links.Messages;
using Mikodev.Optional;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static Mikodev.Optional.Extensions;

namespace Chatter.Pages
{
    public partial class Dialog : Page
    {
        private readonly LinkProfile profile;

        private ObservableCollection<Message> messages;

        private ScrollViewer scrollViewer;

        public Dialog()
        {
            InitializeComponent();
            profile = App.CurrentProfile;
            DataContext = profile;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.Assert(profile.UnreadCount == 0);
            App.TextBoxKeyDown += TextBox_KeyDown;

            scrollViewer = listbox.FindChild<ScrollViewer>(string.Empty);
            Debug.Assert(scrollViewer != null);
            messages = profile.MessageCollection;
            messages.CollectionChanged += ObservableCollection_CollectionChanged;
            listbox.ItemsSource = messages;
            scrollViewer.ScrollToBottom();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            listbox.ItemsSource = null;
            App.TextBoxKeyDown -= TextBox_KeyDown;
            messages.CollectionChanged -= ObservableCollection_CollectionChanged;
        }

        private void SendText()
        {
            var text = textbox.Text;
            if (string.IsNullOrEmpty(text))
                return;
            var _ = App.CurrentClient.SendTextAsync(profile, textbox.Text);
            textbox.Text = string.Empty;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource != textbox || e.Key != Key.Enter)
                return;
            var modifierKeys = e.KeyboardDevice.Modifiers;
            if (modifierKeys == ModifierKeys.None)
                SendText();
            else
                textbox.Insert(Environment.NewLine);
            e.Handled = true;
        }

        private void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            scrollViewer.ScrollToBottom();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)e.OriginalSource;
            var tag = button.Tag as string;
            if (tag == "post")
                SendText();
        }

        private string SingleFileDrop(DragEventArgs e)
        {
            return e.Data.GetDataPresent(DataFormats.FileDrop) == false
                ? (string)null
                : !(e.Data.GetData(DataFormats.FileDrop) is string[] array) || array.Length != 1 ? null : array[0];
        }

        private void TextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = string.IsNullOrEmpty(SingleFileDrop(e)) ? DragDropEffects.None : DragDropEffects.Copy;
            e.Handled = true;
        }

        private async void TextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            var path = SingleFileDrop(e);
            if (string.IsNullOrEmpty(path))
                return;
            var client = App.CurrentClient;
            if (Directory.Exists(path))
            {
                _ = await TryAsync(client.SendDirectoryAsync(profile, path, x => new DirectoryWindow(x).Show()));
            }
            else if (File.Exists(path))
            {
                if (!new[] { ".JPG", ".PNG", ".BMP" }.Contains(Path.GetExtension(path).ToUpper()))
                    _ = await TryAsync(client.SendFileAsync(profile, path, x => new FileWindow(x).Show()));
                else if ((await TryAsync(() => client.SendImageAsync(profile, new FileInfo(path)))) is var result && result.IsError())
                    _ = MessageBox.Show(result.UnwrapError().Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
