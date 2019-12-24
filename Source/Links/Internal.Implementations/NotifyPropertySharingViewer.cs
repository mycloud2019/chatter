using Mikodev.Links.Abstractions;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mikodev.Links.Internal.Implementations
{
    internal abstract class NotifyPropertySharingViewer : SharingViewer, INotifyPropertyChanging, INotifyPropertyChanged
    {
        private string name;

        private string full;

        private long length;

        private long position;

        private double speed;

        private double progress;

        private SharingStatus status;

        private TimeSpan remaining;

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        public override Profile Profile { get; }

        public override string Name => name;

        public override string FullName => full;

        public override long Length => length;

        public override long Position => position;

        public override double Speed => speed;

        public override double Progress => progress;

        public override SharingStatus Status => status;

        public override TimeSpan Remaining => remaining;

        protected NotifyPropertySharingViewer(Profile profile) => this.Profile = profile ?? throw new ArgumentNullException(nameof(profile));

        protected void NotifyProperty<T>(ref T location, T value, [CallerMemberName] string property = null) => NotifyPropertyHelper.NotifyProperty(this, PropertyChanging, PropertyChanged, ref location, value, property);

        public void SetName(string name) => NotifyProperty(ref this.name, name, nameof(Name));

        public void SetFullName(string name) => NotifyProperty(ref this.full, name, nameof(FullName));

        public void SetLength(long length) => NotifyProperty(ref this.length, length, nameof(Length));

        public void SetPosition(long position) => NotifyProperty(ref this.position, position, nameof(Position));

        public void SetSpeed(double speed) => NotifyProperty(ref this.speed, speed, nameof(Speed));

        public void SetProgress(double progress) => NotifyProperty(ref this.progress, progress, nameof(Progress));

        public void SetStatus(SharingStatus status) => NotifyProperty(ref this.status, status, nameof(Status));

        public void SetRemaining(TimeSpan remaining) => NotifyProperty(ref this.remaining, remaining, nameof(Remaining));
    }
}
