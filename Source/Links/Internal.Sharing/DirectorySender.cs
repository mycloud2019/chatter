using Mikodev.Links.Abstractions;
using Mikodev.Links.Internal.Implementations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal sealed class DirectorySender : DirectoryObject, ISharingDirectorySender
    {
        public DirectorySender(IClient client, Profile profile, Stream stream, string fullPath) : base(client, stream, new NotifyDirectorySharingViewer(profile, Path.GetDirectoryName(fullPath), fullPath)) { }

        protected override Task InvokeAsync() => PutDirectoryAsync(Viewer.FullName);
    }
}
