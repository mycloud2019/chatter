using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class FileSender : FileObject, ISharingFileSender
    {
        public FileSender(IClient client, Profile profile, Stream stream, string fullName, long length) : base(client, stream, new NotifyFileSharingViewer(profile, Path.GetFileName(fullName), fullName, length)) { }

        protected override Task InvokeAsync() => PutFileAsync(Viewer.FullName, Viewer.Length);
    }
}
