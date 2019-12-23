using Mikodev.Links.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class FileReceiver : FileObject, IShareReceiver, ISharingFileReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public FileReceiver(IClient client, Profile profile, Stream stream, string name, long length) : base(client, stream, new FileSharingViewer(profile, name, name, length)) { }

        public void Accept(bool flag) => completion.SetResult(flag);

        public Task<bool> WaitForAcceptAsync() => completion.Task;

        protected override Task InvokeAsync() => GetFileAsync(Viewer.FullName, Viewer.Length);
    }
}
