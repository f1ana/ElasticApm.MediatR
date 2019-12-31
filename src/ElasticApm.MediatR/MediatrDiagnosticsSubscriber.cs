using System;
using System.Diagnostics;
using Elastic.Apm;
using Elastic.Apm.DiagnosticSource;

namespace ElasticApm.MediatR {
    public class MediatrDiagnosticsSubscriber : IDiagnosticsSubscriber {
        public IDisposable Subscribe(IApmAgent components) {
            var retVal = new CompositeDisposable();
            var initializer = new MediatrDiagnosticInitializer(components);

            retVal.Add(initializer);
            retVal.Add(DiagnosticListener.AllListeners.Subscribe(initializer));

            return retVal;
        }
    }
}