using Chatter.Internal;
using Chatter.Interop;
using Mikodev.Links.Abstractions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Chatter.Windows
{
    public abstract partial class ShareWindow : Window
    {
        private readonly ISharingObject shareObject;

        private readonly SharingViewer viewer;

        private IList<(double progress, double speed)> values;

        private double maximumSpeed = 0.0;

        public ShareWindow(ISharingObject shareObject)
        {
            InitializeComponent();

            var owner = Application.Current.MainWindow;
            Debug.Assert(owner is Entrance);
            Owner = owner;

            var receiver = shareObject is ISharingReceiver;
            this.shareObject = shareObject ?? throw new ArgumentNullException(nameof(shareObject));
            this.viewer = shareObject.Viewer;
            if (receiver == false)
                acceptButton.Visibility = Visibility.Collapsed;
            sourceTextBlock.Text = receiver ? "Sender" : "Receiver";
            Title = receiver ? "Receiver" : "Sender";
            DataContext = this.viewer;

            var handler = new PropertyChangedEventHandler((s, e) => OnUpdate(e.PropertyName));
            if (receiver)
                Loaded += (s, _) => NativeMethods.FlashWindow(new WindowInteropHelper((Window)s).Handle, true);
            Closed += (s, _) => (shareObject as IDisposable)?.Dispose();
            Loaded += (s, _) => OnUpdate(string.Empty);
            Loaded += (s, _) => ((INotifyPropertyChanged)viewer).PropertyChanged += handler;
            Unloaded += (s, _) => ((INotifyPropertyChanged)viewer).PropertyChanged -= handler;
            SizeChanged += (s, _) => UpdateGraphics(values);
        }

        private void OnButtonClick(object _, RoutedEventArgs e)
        {
            var button = e.OriginalSource as Button; ;
            var receiver = shareObject as ISharingReceiver;
            if (button == acceptButton)
            {
                Debug.Assert(receiver != null);
                receiver.Accept(true);
                button.IsEnabled = false;
            }
            else if (button == cancelButton)
            {
                if (receiver != null && viewer.Status == SharingStatus.Pending)
                    receiver.Accept(false);
                else
                    (shareObject as IDisposable)?.Dispose();
            }
            else if (button == backupButton)
            {
                Close();
            }
        }

        protected virtual void OnUpdate(string propertyName)
        {
            if (propertyName != nameof(SharingViewer.Status) || (viewer.Status & SharingStatus.Completed) == 0)
                return;
            // Change visibility when completed
            buttonPanel.IsEnabled = false;
            buttonPanel.Visibility = Visibility.Collapsed;
            backupPanel.Visibility = Visibility.Visible;
        }

        protected void UpdateNotice(string text)
        {
            noticeTextBlock.Text = $"Status: {text}";
        }

        protected void UpdateGraphics(IList<(double progress, double speed)> values)
        {
            canvas.ClearVisuals();
            this.values = values;
            if (values == null || values.Count == 0)
                return;
            var window = canvas.FindAncestor<Window>();
            var point = canvas.TranslatePoint(new Point(0, 0), window);
            // Align to pixels
            var offset = new Vector(point.X - Math.Truncate(point.X), point.Y - Math.Truncate(point.Y));
            var width = canvas.ActualWidth;
            var height = canvas.ActualHeight;
            var visual = new DrawingVisual();
            var context = visual.RenderOpen();

            var lastValue = values.Last();
            if (lastValue.speed > maximumSpeed)
                maximumSpeed = lastValue.speed;
            var maximum = 1.25 * maximumSpeed;

            var points = new List<Point>();
            foreach (var (progress, speed) in values)
            {
                var y = (1.0 - speed / maximum) * height;
                var x = progress * width;
                if (progress != 0 && points.Count == 0)
                    points.Add(new Point(0, y));
                points.Add(new Point(x, y));
            }
            var lastPoint = points.Last();
            var streamGeometry = new StreamGeometry();
            using (var geometryContext = streamGeometry.Open())
            {
                geometryContext.BeginFigure(new Point(0, height), true, true);
                points.Add(new Point(lastPoint.X, height));
                geometryContext.PolyLineTo(points, true, true);
            }
            var status = viewer.Status;
            var color = status == SharingStatus.Success
                ? Color.FromArgb(192, 32, 192, 0)
                : status == SharingStatus.Aborted ? Color.FromArgb(192, 220, 20, 60) : Color.FromArgb(192, 58, 110, 165);
            context.DrawGeometry(new SolidColorBrush(color), null, streamGeometry);

            var vertical = (int)(lastPoint.Y + 0.5) + 0.5 - offset.Y;
            context.DrawLine(new Pen(Brushes.Black, 1), new Point(0, vertical), new Point(width, vertical));

            var text = $"{Extensions.ToUnit((long)values.Last().speed)}/s";
            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);
            var fontSize = noticeTextBlock.FontSize;
            var formatted = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, Foreground, 96.0);
            var textPoint = new Point(width - formatted.Width - canvasBorder.BorderThickness.Right, vertical > formatted.Height ? vertical - formatted.Height : vertical);
            context.DrawText(formatted, textPoint);

            context.Close();
            canvas.AddVisual(visual);
        }
    }
}
