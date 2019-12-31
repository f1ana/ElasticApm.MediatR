using System;
using System.Diagnostics;
using Elastic.Apm;
using MediatR.Pipeline.Diagnostics.Constants;

namespace ElasticApm.MediatR {
    internal sealed class MediatrDiagnosticInitializer : IObserver<DiagnosticListener>, IDisposable {
        private readonly IApmAgent _apmAgent;
        private IDisposable _sourceSubscription;
        internal MediatrDiagnosticInitializer(IApmAgent apmAgent) => _apmAgent = apmAgent;
        
        public void OnCompleted() {
            
        }

        public void OnError(Exception error) {
            
        }

        public void OnNext(DiagnosticListener value) {
            if (value.Name == DiagnosticListenerConstants.LISTENER_NAME) {
                _sourceSubscription = value.Subscribe(new MediatrDiagnosticListener(_apmAgent));
            }
        }

        public void Dispose() {
            _sourceSubscription?.Dispose();
        }
    }
}