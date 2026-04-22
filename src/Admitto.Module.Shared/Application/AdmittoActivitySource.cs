using System.Diagnostics;

namespace Amolenk.Admitto.Module.Shared.Application;

/// <summary>
/// Central <see cref="System.Diagnostics.ActivitySource"/> for Admitto-specific tracing spans
/// (mediator command/query/event handling, outbox dispatch, queue consumption).
/// </summary>
/// <remarks>
/// Spans are exported through the OpenTelemetry pipeline configured in
/// <c>Admitto.ServiceDefaults</c>, which registers this source by name.
/// Propagation across the Azure Storage Queue boundary is done via the W3C
/// <c>traceparent</c>/<c>tracestate</c> CloudEvents extension attributes so that
/// worker-side consumers can resume the same trace when queue processing is re-enabled.
/// </remarks>
public static class AdmittoActivitySource
{
    public const string Name = "Admitto";

    public static readonly ActivitySource ActivitySource = new(Name);

    /// <summary>
    /// CloudEvents distributed-tracing extension attribute carrying the W3C traceparent.
    /// See https://github.com/cloudevents/spec/blob/main/cloudevents/extensions/distributed-tracing.md
    /// </summary>
    public const string TraceParentAttribute = "traceparent";

    /// <summary>
    /// CloudEvents distributed-tracing extension attribute carrying the W3C tracestate.
    /// </summary>
    public const string TraceStateAttribute = "tracestate";
}
