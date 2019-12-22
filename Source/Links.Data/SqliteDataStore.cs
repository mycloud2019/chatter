using Microsoft.EntityFrameworkCore;
using Mikodev.Links.Data.Abstractions;
using Mikodev.Links.Data.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mikodev.Links.Data
{
    public class SqliteDataStore : ILinkDataStore
    {
        private readonly string filename;

        public SqliteDataStore(string filename)
        {
            if (string.IsNullOrEmpty(filename))
                throw new ArgumentException("Argument can not be null or empty.", nameof(filename));
            this.filename = filename;
        }

        public async Task<IEnumerable<MessageModel>> QueryMessagesAsync(string profileId, int count)
        {
            using var context = new MessageContext(filename);
            _ = await context.Database.EnsureCreatedAsync();
            var query = context.Messages.Where(x => x.ProfileId == profileId).OrderByDescending(x => x.DateTime).Take(count);
            var messages = await query.ToListAsync();
            return messages;
        }

        public async Task StoreMessagesAsync(IEnumerable<MessageModel> messages)
        {
            using var context = new MessageContext(filename);
            _ = await context.Database.EnsureCreatedAsync();
            await context.Messages.AddRangeAsync(messages);
            _ = await context.SaveChangesAsync();
        }
    }
}
