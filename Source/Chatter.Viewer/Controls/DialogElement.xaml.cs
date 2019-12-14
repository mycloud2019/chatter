﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Mikodev.Links.Messages;
using Mikodev.Links.Messages.Implementations;
using Mikodev.Optional;
using System.ComponentModel;
using System.Diagnostics;
using static Mikodev.Optional.Extensions;

namespace Chatter.Viewer.Controls
{
    public class DialogElement : UserControl
    {
        private readonly Grid grid;

        private readonly Border head;

        private readonly Border tail;

        private readonly Border border;

        private Message message;

        public DialogElement()
        {
            AvaloniaXamlLoader.Load(this);
            this.grid = this.FindControl<Grid>("grid");
            this.head = this.FindControl<Border>("head");
            this.tail = this.FindControl<Border>("tail");
            this.border = this.FindControl<Border>("content");
            this.DataContextChanged += (s, e) => { Detach(); Attach(); };
            this.DetachedFromVisualTree += (s, e) => { Detach(); };
        }

        private void Attach()
        {
            Debug.Assert(message is null);
            message = (Message)DataContext;
            Debug.Assert(message != null);
            message.PropertyChanged += this.Message_PropertyChanged;
            if (message is TextMessage text)
                border.Child = new TextBox { IsReadOnly = true, Padding = new Thickness(8), Text = text.Text, Background = Brushes.Transparent, BorderThickness = new Thickness(0), };
            else if (message is ImageMessage image)
                border.Child = LoadImageElement(image.ImagePath);
            const int space = 48;
            var local = message.Reference == MessageReference.Local;
            grid.HorizontalAlignment = local ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            head.Width = local ? space : default;
            tail.Width = local ? default : space;
        }

        private void Detach()
        {
            if (message is null)
                return;
            message.PropertyChanged -= this.Message_PropertyChanged;
            message = null;
        }

        private void Message_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (message is ImageMessage image && e.PropertyName == nameof(ImageMessage.ImagePath))
                border.Child = LoadImageElement(image.ImagePath);
        }

        private IControl LoadImageElement(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;
            var bitmap = Try(() => new Bitmap(path)).UnwrapOrDefault();
            if (bitmap is null)
                return null;
            return new Image { Source = bitmap, MaxHeight = 512, MaxWidth = 512 };
        }
    }
}
