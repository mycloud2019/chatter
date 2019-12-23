using Mikodev.Links.Abstractions;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    internal sealed class FileSender : FileObject, ISharingFileSender
    {
        public FileSender(IClient client, Profile profile, Stream stream, string fullName, long length) : base(client, stream, new FileSharingViewer(profile, Path.GetFileName(fullName), fullName, length)) { }

        protected override Task InvokeAsync() => PutFileAsync(Viewer.FullName, Viewer.Length);
    }
}
