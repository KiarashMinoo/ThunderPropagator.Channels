using System.Net;

namespace ThunderPropagator.Channels.Chat.Models.Groups
{
    internal
#if !DEBUG
        sealed
#endif
        class GroupService(IChatContext chatContext)
    {
        public Task<Group?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
            => chatContext.GetAsync<Group, Guid>(groupId, cancellationToken);

        public Task<Group> CreateAsync(string name, CancellationToken cancellationToken = default, params Guid[] users)
        {
            var group = Group.Create(name);

            foreach (var user in users)
                group.AddUser(user);

            return chatContext.CreateAsync(group, cancellationToken);
        }

        public async Task<IReadOnlyCollection<Group>> GetAllAsync(CancellationToken cancellationToken = default)
            => await chatContext.GetAllAsync<Group>(cancellationToken);

        public async Task AddUserToGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            group.AddUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
        }

        public async Task RemoveUserFromGroupAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            group.RemoveUser(userId);
            await chatContext.UpdateAsync(group, cancellationToken);
        }

        public async Task<Group> RenameGroupAsync(Guid groupId, string name, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            group.SetName(name);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }

        public async Task<Group> SetGroupIconAsync(Guid groupId, string icon, CancellationToken cancellationToken = default)
        {
            var group = await GetByIdAsync(groupId, cancellationToken) ?? throw new GroupNotFoundException();
            group.SetGroupIcon(icon);
            await chatContext.UpdateAsync(group, cancellationToken);
            return group;
        }
    }
}