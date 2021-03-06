﻿using System.IO;
using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public sealed class FileSender : FileObject
    {
        public FileSender(LinkClient client, LinkProfile profile, Stream stream, string fullName, long length) : base(client, profile, stream, length)
        {
            Name = Path.GetFileName(fullName);
            FullName = fullName;
        }

        protected override Task InvokeAsync() => SendFileAsync(FullName, Length);
    }
}
