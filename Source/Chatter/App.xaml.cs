using Mikodev.Links;
using Mikodev.Links.Annotations;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Chatter
{
    public partial class App : Application
    {
        private Client client;

        private Profile profile;

        private EventHandler<KeyEventArgs> textboxKeyDown;

        public static Client CurrentClient
        {
            get => (Current as App)?.client;
            set => ((App)Current).client = value;
        }

        public static Profile CurrentProfile
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
            (client as IDisposable)?.Dispose();
            base.OnExit(e);
        }
    }
}
