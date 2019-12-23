using Mikodev.Links.Annotations;
using Mikodev.Links.Messages;
using System;
using System.Collections.Generic;
using System.IO;
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

        public abstract DirectoryInfo ReceivingDirectory { get; }

        public abstract ILinkSettings Settings { get; }

        public abstract void CleanProfiles();

        public abstract Task<IEnumerable<Message>> GetMessagesAsync(Profile profile);

        public abstract Task SendTextAsync(Profile profile, string text);

        public abstract Task SendImageAsync(Profile profile, string file);

        public abstract Task SendFileAsync(Profile profile, string file, FileSenderHandler handler);

        public abstract Task SendDirectoryAsync(Profile profile, string directory, DirectorySenderHandler handler);

        public abstract Task SetProfileImageAsync(string file);

        public abstract Task<Task> StartAsync();
    }
}
