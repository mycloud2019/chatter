using Mikodev.Links.Annotations;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public sealed class FileReceiver : FileObject, IShareReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public FileReceiver(Client client, Profile profile, Stream stream, string name, long length) : base(client, profile, stream, length)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = name;
        }

        public void Accept(bool flag) => completion.SetResult(flag);

        public Task<bool> AcceptAsync() => completion.Task;

        protected override Task InvokeAsync() => ReceiveFileAsync(FullName, Length);
    }
}
