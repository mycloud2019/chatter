using Mikodev.Links.Messages;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links
{
    public interface ILinkClient
    {
        LinkProfile Profile { get; }
        ObservableCollection<LinkProfile> ProfileCollection { get; }
        DirectoryInfo ReceiveDirectory { get; }
        LinkSettings Settings { get; }

        event DirectoryReceiverHandler NewDirectoryReceiver;

        event FileReceiverHandler NewFileReceiver;

        event EventHandler<MessageEventArgs> NewMessage;

        void CleanProfileCollection();

        void Dispose();

        Task SendDirectoryAsync(LinkProfile profile, string directoryPath, DirectorySenderHandler handler);

        Task SendFileAsync(LinkProfile profile, string filePath, FileSenderHandler handler);

        Task SendImageAsync(LinkProfile profile, FileInfo fileInfo);

        Task SendTextAsync(LinkProfile profile, string text);

        Task<string> SetProfileImageAsync(FileInfo fileInfo);

        Task<Task> StartAsync();
    }
}
