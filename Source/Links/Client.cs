using Mikodev.Links.Annotations;
using Mikodev.Links.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public abstract class Client
    {
        public abstract event DirectoryReceiverHandler NewDirectoryReceiver;

        public abstract event FileReceiverHandler NewFileReceiver;

        public abstract event EventHandler<MessageEventArgs> NewMessage;

        public abstract Profile Profile { get; }

        public abstract IEnumerable<Profile> Profiles { get; }

        public abstract string ReceivingDirectory { get; }

        public abstract ILinkSettings Settings { get; }

        public abstract void CleanProfiles();

        public abstract Task<IEnumerable<Message>> GetMessagesAsync(Profile profile);

        public abstract Task PutTextAsync(Profile profile, string text);

        public abstract Task PutImageAsync(Profile profile, string file);

        public abstract Task PutFileAsync(Profile profile, string file, FileSenderHandler handler);

        public abstract Task PutDirectoryAsync(Profile profile, string directory, DirectorySenderHandler handler);

        public abstract Task SetProfileImageAsync(string file);

        public abstract Task<Task> StartAsync();
    }
}
