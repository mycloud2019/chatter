using Mikodev.Links.Abstractions;
using System;

namespace Mikodev.Links.Internal.Sharing
{
    internal class DirectorySharingViewer : NotifySharingViewer
    {
        public DirectorySharingViewer(Profile profile, string name, string fullName) : base(profile)
        {
            if (name is null)
                throw new ArgumentNullException(nameof(name));
            if (fullName is null)
                throw new ArgumentNullException(nameof(fullName));
            SetName(name);
            SetFullName(fullName);
        }
    }
}
