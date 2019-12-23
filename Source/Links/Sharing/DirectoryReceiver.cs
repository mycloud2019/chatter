using Mikodev.Links.Annotations;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public sealed class DirectoryReceiver : DirectoryObject, IShareReceiver
    {
        private readonly TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();

        public DirectoryReceiver(Client client, Profile profile, Stream stream, string name) : base(client, profile, stream)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullName = name;
        }

        public void Accept(bool flag) => completion.SetResult(flag);

        public Task<bool> AcceptAsync() => completion.Task;

        protected override Task InvokeAsync() => ReceiveDirectoryAsync(FullName);
    }
}
