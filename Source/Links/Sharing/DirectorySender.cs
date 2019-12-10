using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public sealed class DirectorySender : DirectoryObject
    {
        public DirectorySender(LinkClient client, LinkProfile profile, Stream stream, string fullPath) : base(client, profile, stream)
        {
            Name = Path.GetDirectoryName(fullPath);
            FullName = fullPath;
        }

        protected override Task InvokeAsync() => SendDirectoryAsync(FullName);
    }
}
