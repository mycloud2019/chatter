using Mikodev.Links.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    internal sealed class DirectoryReceiver : DirectoryObject, IShareReceiver, ISharingDirectoryReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public DirectoryReceiver(IClient client, Profile profile, Stream stream, string name) : base(client, stream, new DirectorySharingViewer(profile, name, name)) { }

        public void Accept(bool flag) => completion.SetResult(flag);

        public Task<bool> WaitForAcceptAsync() => completion.Task;

        protected override Task InvokeAsync() => GetDirectoryAsync(Viewer.FullName);
    }
}
