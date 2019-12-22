using System.Collections.Generic;
using System.Threading.Tasks;

namespace Mikodev.Links.Data.Abstractions
{
    public interface ILinkDataStore
    {
        Task<IEnumerable<MessageModel>> QueryMessagesAsync(string profileId, int count);

        Task StoreMessagesAsync(IEnumerable<MessageModel> messages);
    }
}
