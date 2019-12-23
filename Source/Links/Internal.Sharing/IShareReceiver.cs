using System.Threading.Tasks;

namespace Mikodev.Links.Internal.Sharing
{
    internal interface IShareReceiver
    {
        Task<bool> WaitForAcceptAsync();
    }
}
