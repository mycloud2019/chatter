using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    internal interface IShareReceiver
    {
        Task<bool> WaitForAcceptAsync();
    }
}
