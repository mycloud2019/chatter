using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Mikodev.Links;
using Mikodev.Links.Messages;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;

namespace Chatter.Viewer.Controls
{
    public class Dialog : UserControl
    {
        private readonly ListBox listbox;

        private readonly TextBox textbox;

        private LinkProfile profile;

        private ObservableCollection<Message> messages;

        public Dialog()
        {
            AvaloniaXamlLoader.Load(this);
            this.AttachedToVisualTree += this.UserControl_AttachedToVisualTree;
            this.DetachedFromVisualTree += this.UserControl_DetachedFromVisualTree;
            this.listbox = this.FindControl<ListBox>("listbox");
            this.textbox = this.FindControl<TextBox>("textbox");
            this.FindControl<Button>("post").Click += (s, e) => PostText();
        }

        private void UserControl_AttachedToVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            this.profile = App.CurrentProfile;
            this.messages = profile.MessageCollection;
            this.DataContext = profile;
            listbox.Items = messages;
            textbox.KeyDown += this.TextBox_KeyDown;
            messages.CollectionChanged += this.ObservableCollection_CollectionChanged;
        }

        private void UserControl_DetachedFromVisualTree(object sender, VisualTreeAttachmentEventArgs e)
        {
            messages.CollectionChanged -= this.ObservableCollection_CollectionChanged;
            textbox.KeyDown -= this.TextBox_KeyDown;
            listbox.Items = null;
            this.DataContext = null;
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            switch (e.Modifiers)
            {
                case InputModifiers.None:
                    PostText();
                    break;

                case InputModifiers.Shift:
                    textbox.Text = textbox.Text.Insert(textbox.CaretIndex, Environment.NewLine);
                    textbox.CaretIndex += Environment.NewLine.Length;
                    break;
            }
        }

        private async void ObservableCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var item = e.NewItems?.OfType<Message>()?.FirstOrDefault();
            if (item == null)
                return;
            await Task.Delay(200);
            listbox.ScrollIntoView(item);
        }

        private void PostText()
        {
            var text = textbox.Text;
            if (string.IsNullOrEmpty(text))
                return;
            _ = App.CurrentClient.SendTextAsync(profile, text);
            textbox.Text = string.Empty;
        }
    }
}
