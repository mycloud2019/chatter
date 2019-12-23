using System.Threading.Tasks;

namespace Mikodev.Links.Abstractions
{
    public interface ISettings
    {
        Task SaveAsync(string path);
    }
}
