using System.Runtime.CompilerServices;
using EnterpriseAgentOs.Api.GraphQL.Types;
using EnterpriseAgentOs.Infrastructure.Adapters.Channels.WhatsApp;

namespace EnterpriseAgentOs.Api.GraphQL.Subscriptions;

[ExtendObjectType(typeof(GraphQLSubscriptions))]
public class WhatsAppSubscriptions
{
    [Subscribe(With = nameof(WhatsAppConnectionStatusStream))]
    public WhatsAppConnectionStatusPayload WhatsAppConnectionStatus(
        Guid connectionId,
        [EventMessage] WhatsAppConnectionStatusPayload payload)
        => payload;

    public async IAsyncEnumerable<WhatsAppConnectionStatusPayload> WhatsAppConnectionStatusStream(
        Guid connectionId,
        [Service] WhatsAppGatewayService gateway,
        [EnumeratorCancellation] CancellationToken ct)
    {
        // Emit current state immediately
        var currentState = gateway.GetConnectionState(connectionId);
        var currentQr = gateway.GetPendingQrCode(connectionId);
        yield return new WhatsAppConnectionStatusPayload(connectionId, currentState, currentQr);

        // Stream updates via Channel
        var channel = System.Threading.Channels.Channel.CreateUnbounded<WhatsAppConnectionStatusPayload>();

        void OnStatusChanged(Guid id, string status, string? qr)
        {
            if (id == connectionId)
                channel.Writer.TryWrite(new WhatsAppConnectionStatusPayload(id, status, qr));
        }

        gateway.ConnectionStatusChanged += OnStatusChanged;
        try
        {
            await foreach (var payload in channel.Reader.ReadAllAsync(ct))
            {
                yield return payload;
                if (payload.Status == "open") break;
            }
        }
        finally
        {
            gateway.ConnectionStatusChanged -= OnStatusChanged;
        }
    }
}
