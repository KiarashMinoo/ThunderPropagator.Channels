using Microsoft.EntityFrameworkCore;

namespace RapidStreamer.Channels.Chat.Models.Groups
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupService(IChatContext chatContext)
    {
        public async Task<Group> CreateAsync(string name, CancellationToken cancellationToken = default, params Guid[] users)
        {
            var group = Group.Create(name);

            foreach (var user in users)
                group.AddUser(user);

            var entry = await chatContext.Groups.AddAsync(group, cancellationToken);

            await chatContext.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<IReadOnlyCollection<Group>> GetAllAsync(CancellationToken cancellationToken = default)
            => await chatContext.Groups.AsNoTracking().ToListAsync(cancellationToken);
    }
}