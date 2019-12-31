using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Elastic.Apm;
using Elastic.Apm.Api;
using Elastic.Apm.Logging;
using MediatR.Pipeline.Diagnostics.Events;

namespace ElasticApm.MediatR {
    public class MediatrDiagnosticListener : IObserver<KeyValuePair<string, object>> {
        private readonly IApmAgent _apmAgent;
        private readonly IApmLogger _apmLogger;

        private readonly ConcurrentDictionary<Guid, ISpan> _processingQueries = new ConcurrentDictionary<Guid, ISpan>();

        public MediatrDiagnosticListener(IApmAgent apmAgent) {
            _apmAgent = apmAgent;
            _apmLogger = apmAgent.Logger;
        }

        public void OnCompleted() {
        }

        public void OnError(Exception error) {
        }

        public void OnNext(KeyValuePair<string, object> value) {
            switch (value.Key) {
                case "RequestStarted" when value.Value is MediatrEventData payload &&
                                           _apmAgent.Tracer.CurrentTransaction != null:
                    HandleRequestStartEvent(payload);
                    break;
                case "RequestCompleted" when value.Value is MediatrEventData payload:
                    HandleRequestCompletedEvent(payload);
                    break;
                case "RequestError" when value.Value is MediatrExceptionData payload:
                    HandleRequestFailedEvent(payload);
                    break;
            }
        }

        private void HandleRequestStartEvent(MediatrEventData @event) {
            try {
                var transaction = _apmAgent.Tracer.CurrentTransaction;
                var currentExecutionSegment = _apmAgent.Tracer.CurrentSpan ?? (IExecutionSegment) transaction;
                var span = currentExecutionSegment.StartSpan(
                    @event.RequestType.Name,
                    "MediatR",
                    @event.RequestSubType);

                if (!_processingQueries.TryAdd(@event.RequestGuid, span)) return;

                span.Action = @event.RequestSubType == "Query" ? ApiConstants.ActionQuery : ApiConstants.ActionExec;
                span.Context.Db = new Database {
                    Statement = @event.Payload,
                    Type = "MediatR"
                };
            }
            catch (Exception e) {
                //ignore
                _apmLogger.Log(LogLevel.Error, "Exception was thrown while handling 'request started event''", e, null);
            }
        }

        private void HandleRequestCompletedEvent(MediatrEventData @event) {
            try {
                if (!_processingQueries.TryRemove(@event.RequestGuid, out var span)) return;
                span.Duration = @event.TotalMilliseconds;
                span.End();
            }
            catch (Exception ex) {
                // ignore
                _apmLogger.Log(LogLevel.Error, "Exception was thrown while handling 'request succeeded event''", ex,
                    null);
            }
        }

        private void HandleRequestFailedEvent(MediatrExceptionData @event) {
            try {
                if (!_processingQueries.TryRemove(@event.RequestGuid, out var span)) return;
                span.Duration = @event.TotalMilliseconds;
                span.CaptureException(@event.Exception);
                span.End();
            }
            catch (Exception ex) {
                // ignore
                _apmLogger.Log(LogLevel.Error, "Exception was thrown while handling 'request failed event''", ex, null);
            }
        }
    }
}