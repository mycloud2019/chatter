using Mikodev.Links.Annotations;
using System.ComponentModel;
using System.Net;
using System.Runtime.CompilerServices;

namespace Mikodev.Links
{
    public abstract class NotifyProfile : Profile, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string name;

        private string text;

        private int count;

        private ProfileOnlineStatus status;

        private string path;

        private IPAddress address;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public override string Name { get => name; set => NotifyProperty(ref name, value); }

        public override string Text { get => text; set => NotifyProperty(ref text, value); }

        public override int UnreadCount { get => count; set => NotifyProperty(ref count, value); }

        public override ProfileOnlineStatus OnlineStatus => status;

        public override string ImagePath => path;

        public override IPAddress IPAddress => address;

        protected NotifyProfile(string profileId) : base(profileId) { }

        protected void NotifyProperty<T>(ref T location, T value, [CallerMemberName] string property = null) => NotifyPropertyHelper.NotifyProperty(this, PropertyChanging, PropertyChanged, ref location, value, property);

        public void SetOnlineStatus(ProfileOnlineStatus status) => NotifyProperty(ref this.status, status, nameof(OnlineStatus));

        public void SetImagePath(string path) => NotifyProperty(ref this.path, path, nameof(ImagePath));

        public void SetIPAddress(IPAddress address) => NotifyProperty(ref this.address, address, nameof(IPAddress));
    }
}
