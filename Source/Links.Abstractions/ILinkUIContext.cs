using System;
using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    public interface ILinkUIContext
    {
        Task InvokeAsync(Action action);

        void VerifyAccess();
    }
}
