using System.Net;
using ThunderPropagator.Channels.Chat.Configuration;

namespace ThunderPropagator.Channels.Chat.Models.Users;

// Issue #141: thrown by UserService.RegisterAsync when ChatChannelConfiguration.AllowGuestRegister
// is false — a host that provisions users through its own admin/SSO flow can close off
// self-service registration entirely. Forbidden rather than BadRequest since the request itself
// is well-formed; it's rejected by policy, the same reasoning GroupDeleteForbiddenException (#124)
// uses for its own Forbidden mapping.
public
#if !DEBUG
    sealed
#endif
    class GuestRegistrationDisabledException() : HttpRequestException("Guest registration is disabled.", null, HttpStatusCode.Forbidden);
