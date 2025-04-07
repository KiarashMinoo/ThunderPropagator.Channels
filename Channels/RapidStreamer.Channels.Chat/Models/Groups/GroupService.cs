using Microsoft.EntityFrameworkCore;

namespace RapidStreamer.Channels.Chat.Models.Groups
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupService(IChatContext chatContext)
    {
        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
            => chatContext.Groups.SingleOrDefaultAsync(x => x.Id == groupId, cancellationToken);

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

        public async Task AddUserToGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await chatContext.Groups.SingleAsync(x => x.Id == groupId, cancellationToken);
            group.AddUser(userId);
            await chatContext.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveUserFromGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await chatContext.Groups.SingleAsync(x => x.Id == groupId, cancellationToken);
            group.RemoveUser(userId);
            await chatContext.SaveChangesAsync(cancellationToken);
        }

        public async Task<Group> RenameGroupAsync(Guid groupId, string name, CancellationToken cancellationToken = default)
        {
            var group = await chatContext.Groups.SingleAsync(x => x.Id == groupId, cancellationToken);
            group.SetName(name);
            await chatContext.SaveChangesAsync(cancellationToken);
            return group;
        }

        public async Task<Group> SetGroupIconAsync(Guid groupId, string icon, CancellationToken cancellationToken = default)
        {
            var group = await chatContext.Groups.SingleAsync(x => x.Id == groupId, cancellationToken);
            group.SetGroupIcon(icon);
            await chatContext.SaveChangesAsync(cancellationToken);
            return group;
        }
    }
}