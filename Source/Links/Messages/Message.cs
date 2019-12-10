using Mikodev.Links.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mikodev.Links.Messages
{
    public abstract class Message : INotifyPropertyChanging, INotifyPropertyChanged
    {
        private MessageStatus messageStatus;

        #region notify property change

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChange<T>(ref T location, T value, [CallerMemberName] string callerName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(callerName));
            if (EqualityComparer<T>.Default.Equals(location, value))
                return;
            location = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(callerName));
        }

        #endregion

        public string Id { get; }

        public MessageReference Reference { get; }

        public abstract string Path { get; }

        public DateTime DateTime { get; } = DateTime.Now;

        public MessageStatus Status
        {
            get => messageStatus;
            internal set => OnPropertyChange(ref messageStatus, value);
        }

        public Message()
        {
            Id = $"{DateTime.Now:yyyyMMddHHmmssffff}-{Guid.NewGuid():N}";
            Reference = MessageReference.Local;
        }

        public Message(string id)
        {
            if (id.IsNullOrEmpty())
                throw new InvalidOperationException();
            Id = id;
            Reference = MessageReference.Remote;
        }

        public override string ToString() => $"{nameof(Message)}(Id: {Id}, DateTime: {DateTime:u})";
    }
}
