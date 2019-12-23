using System.Threading.Tasks;

namespace Mikodev.Links
{
    public interface ILinkSettings
    {
        Task SaveAsync(string path);
    }
}
