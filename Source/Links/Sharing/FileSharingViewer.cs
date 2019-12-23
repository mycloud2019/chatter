using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Sharing
{
    internal class FileSharingViewer : NotifySharingViewer
    {
        public FileSharingViewer(Profile profile, string name, string fullName, long length) : base(profile)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (fullName is null)
                throw new ArgumentNullException(nameof(fullName));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            SetName(name);
            SetFullName(fullName);
            SetLength(length);
        }
    }
}
