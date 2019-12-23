using Mikodev.Links.Annotations;
using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public sealed class DirectorySender : DirectoryObject
    {
        public DirectorySender(Client client, Profile profile, Stream stream, string fullPath) : base(client, profile, stream)
        {
            Name = Path.GetDirectoryName(fullPath);
            FullName = fullPath;
        }

        protected override Task InvokeAsync() => PutDirectoryAsync(FullName);
    }
}
