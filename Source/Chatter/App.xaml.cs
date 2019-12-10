using Mikodev.Links;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chatter
{
    public partial class App : Application
    {
        private LinkClient client;

        private LinkProfile profile;

        private EventHandler<KeyEventArgs> textboxKeyDown;

        public static LinkClient CurrentClient
        {
            get => (Current as App)?.client;
            set => ((App)Current).client = value;
        }

        public static LinkProfile CurrentProfile
        {
            get => (Current as App)?.profile;
            set => ((App)Current).profile = value;
        }

        public static event EventHandler<KeyEventArgs> TextBoxKeyDown
        {
            add => ((App)Current).textboxKeyDown += value;
            remove => ((App)Current).textboxKeyDown -= value;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            EventManager.RegisterClassHandler(typeof(TextBox), UIElement.KeyDownEvent, new KeyEventHandler((sender, args) => textboxKeyDown?.Invoke(sender, args)));
        }

        protected override void OnExit(ExitEventArgs e)
        {
            client?.Dispose();
            base.OnExit(e);
        }
    }
}
