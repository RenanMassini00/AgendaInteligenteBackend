using System.Text.Json.Serialization;

namespace Scheduler.Api.DTOs;

public record PushPublicKeyResponse(
    bool Enabled,
    string? PublicKey
);

public record PushSubscriptionKeysRequest(
    [property: JsonPropertyName("p256dh")] string P256dh,
    [property: JsonPropertyName("auth")] string Auth
);

public record PushSubscriptionRegisterRequest(
    string Endpoint,
    PushSubscriptionKeysRequest Keys,
    long? ExpirationTime,
    string? UserAgent,
    string? DeviceName
);

public record PushSubscriptionRemoveRequest(
    string Endpoint
);

public record PushTestRequest(
    string? Title,
    string? Body,
    string? Url
);

public record PushSubscriptionStatusResponse(
    ulong UserId,
    bool UserExists,
    bool WebPushConfigured,
    int ActiveSubscriptions,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt,
    int FailureCount,
    string Message
);

public record PushTestResponse(
    string Message,
    ulong UserId,
    int ActiveSubscriptions,
    bool ProviderAccepted,
    DateTime? LastSuccessAt,
    DateTime? LastFailureAt
);
