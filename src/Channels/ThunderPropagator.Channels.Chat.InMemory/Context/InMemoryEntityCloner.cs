using System.Reflection;
using ThunderPropagator.Channels.Chat.Models.Groups;
using ThunderPropagator.Channels.Chat.InMemory.Context;

namespace ThunderPropagator.Channels.Chat.InMemory.Context
{
    /// <summary>
    /// Deep-clones a Chat entity by copying every instance field via reflection, constructing the
    /// clone through the entity's own private parameterless constructor.
    ///
    /// This exists so InMemoryChatContext never hands out (or stores) the same live object the
    /// caller holds — every read and write copies. Without it, mutating an entity returned from
    /// GetAsync would silently change the store's copy too, without ever calling UpdateAsync — the
    /// exact "ad hoc in-memory implementation hides bugs a real database would catch" problem #112
    /// exists to avoid (a real database, and the EF Core/MongoDB providers backed by one, can never
    /// behave this way: reading always produces a value copied out of storage).
    ///
    /// Group is the one type needing a special case: cloning it by blindly copying every field would
    /// copy the reference to its private _groupUsers HashSet, not a new one, so the clone and the
    /// original would still share (and corrupt) the same collection.
    /// </summary>
    internal static class InMemoryEntityCloner
    {
        private static readonly FieldInfo GroupUsersField = typeof(Group)
            .GetField("_groupUsers", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public static TEntity Clone<TEntity>(TEntity source) where TEntity : class
        {
            var type = typeof(TEntity);
            var constructor = type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
                ?? throw new InvalidOperationException($"{type.Name} has no parameterless constructor.");
            var clone = (TEntity)constructor.Invoke(null);

            foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                field.SetValue(clone, field.GetValue(source));

            if (clone is Group clonedGroup)
            {
                var originalSet = (HashSet<GroupUser>)GroupUsersField.GetValue(source)!;
                GroupUsersField.SetValue(clonedGroup, new HashSet<GroupUser>(originalSet));
            }

            return clone;
        }
    }
}
