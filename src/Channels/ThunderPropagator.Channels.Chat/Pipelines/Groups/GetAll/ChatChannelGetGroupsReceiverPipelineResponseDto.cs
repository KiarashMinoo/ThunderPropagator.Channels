using ThunderPropagator.Application.Collections;
using ThunderPropagator.Channels.Chat.Endpoints;

namespace ThunderPropagator.Channels.Chat.Pipelines.Groups.GetAll
{
    // Issue #35: used to expose raw Group entities — GroupUsers (each carrying a nested User) and
    // all — to any authenticated caller, leaking every member's identity for every group in the
    // system regardless of the caller's own membership. Reuses ChatChannelGroupSummaryDto, the same
    // reduced projection issue #131 already built for the REST group-listing endpoint for this exact
    // reason. Groups/GetAll itself deliberately stays unscoped by membership (a "what groups exist"
    // discovery query, paired with the self-service Groups/Join pipeline — see #131's own comment on
    // why the REST endpoint reuses UserService.GetUserGroupsAsync instead of this one for "my groups"
    // rather than fixing this pipeline to do the same); only the per-group membership detail leaked
    // through it needed to go.
    internal
#if !DEBUG
        sealed
#endif
        class ChatChannelGetGroupsReceiverPipelineResponseDto : ResponseContentFormCollection
    {
        public required IReadOnlyCollection<ChatChannelGroupSummaryDto> Groups { get; init; }
    }
}