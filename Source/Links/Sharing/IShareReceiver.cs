using System.Threading.Tasks;

namespace Mikodev.Links.Sharing
{
    public interface IShareReceiver
    {
        void Accept(bool accept);

        Task<bool> AcceptAsync();
    }
}
