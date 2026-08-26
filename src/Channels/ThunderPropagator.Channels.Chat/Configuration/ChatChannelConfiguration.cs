using ThunderPropagator.Application.Channels;
using ThunderPropagator.Channels.Chat.Models.Messages;

namespace ThunderPropagator.Channels.Chat.Configuration
{
    public
#if !DEBUG
        sealed
#endif
        class ChatChannelConfiguration : AbstractChannelConfiguration
    {
        public ChatChannelConfiguration()
        {
            IsEnabled = true;
        }

        // Issue #120: how long after a message is sent its sender may still edit it —
        // MessageService.EditMessageAsync rejects an edit request once this window has elapsed.
        // 15 minutes is a common chat-app norm; a consumer can widen or narrow it via the
        // channelConfigurator callback AddChatChannel already accepts.
        public TimeSpan MessageEditWindow { get; set; } = TimeSpan.FromMinutes(15);

        // Issue #125: whether marking a message read notifies its sender at all — a single
        // channel-wide switch, same pattern as MessageEditWindow above; a consumer can turn it off
        // via the channelConfigurator callback AddChatChannel already accepts.
        public bool ReadReceiptsEnabled { get; set; } = true;

        // Issue #136: GroupService.CreateAsync enforces this against the resulting membership
        // (creator included) so a group can't grow unbounded at creation time — same
        // channelConfigurator override pattern as MessageEditWindow/ReadReceiptsEnabled above. 250 is
        // a generous default for a chat group; a consumer with different needs can widen or narrow
        // it.
        //
        // Issue #141: this is also the setting that issue's own "MaxGroupSize" AC item refers to —
        // #136 already added it and GroupService.CreateAsync already enforces it, so #141 doesn't
        // introduce a second, differently-named property for the same concept. Validate (below) now
        // additionally checks it's at least 1, closing #141's own "invalid values fail startup" AC
        // for this setting too.
        public int MaxGroupMembers { get; set; } = 250;

        // Issue #141: caps a single message's Body length. MessageService.SendMessageAsync/
        // SendMessageToGroupAsync/EditMessageAsync all enforce this identically — both REST and
        // WebSocket message flows call into those same service methods, so neither transport needs
        // its own copy of the check. 4000 characters is a generous limit for a chat message (well
        // above what a legitimate message needs) while still bounding worst-case per-message
        // storage/emit cost; a consumer can widen or narrow it via the channelConfigurator callback
        // AddChatChannel already accepts.
        public int MaxMessageLength { get; set; } = 4000;

        // Issue #141: the default page size MessageService.GetDirectMessageHistoryAsync/
        // GetGroupMessageHistoryAsync fall back to when a caller doesn't specify one, for both the
        // REST history endpoints and the WebSocket Messages/History pipeline.
        // MessageService.MaxPageSize (the hard per-request ceiling) is unaffected by this setting.
        // Defaults to MessageService.DefaultPageSize — the same 50 both transports already used as a
        // hardcoded default before this setting existed — so an unconfigured consumer sees no
        // behavior change.
        public int MessageHistoryPageSize { get; set; } = MessageService.DefaultPageSize;

        // Issue #141: whether UserService.RegisterAsync accepts new self-service registrations at
        // all. A host that provisions users through its own admin flow (invite-only, SSO-provisioned,
        // etc.) can set this to false to close off open registration entirely — the WebSocket
        // Users/Register pipeline is this setting's only call path today, since no REST registration
        // endpoint exists yet. Defaults to true (registration open), the existing behavior before
        // this setting existed, so an unconfigured consumer sees no behavior change.
        public bool AllowGuestRegister { get; set; } = true;

        // Issue #141: called once by AddChatChannel immediately after the consumer's
        // channelConfigurator callback runs, so a misconfigured value fails host startup with a
        // property-specific message rather than surfacing later as a confusing failure the first
        // time some unrelated request happens to exercise it.
        internal void Validate()
        {
            if (MaxMessageLength < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxMessageLength), MaxMessageLength, $"{nameof(MaxMessageLength)} must be at least 1.");

            if (MaxGroupMembers < 1)
                throw new ArgumentOutOfRangeException(nameof(MaxGroupMembers), MaxGroupMembers, $"{nameof(MaxGroupMembers)} must be at least 1.");

            if (MessageHistoryPageSize < 1 || MessageHistoryPageSize > MessageService.MaxPageSize)
                throw new ArgumentOutOfRangeException(nameof(MessageHistoryPageSize), MessageHistoryPageSize, $"{nameof(MessageHistoryPageSize)} must be between 1 and {MessageService.MaxPageSize}.");
        }
    }
}