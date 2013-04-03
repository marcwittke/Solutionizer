using System.Diagnostics;
using NLog;
using NLog.Config;
using NLog.Targets;
using NUnit.Framework;

namespace Solutionizer.Tests {
    public abstract class TestBaseWithLogging {
        private class MyTraceTarget : TargetWithLayout {
            protected override void Write(LogEventInfo logEvent) {
                if (logEvent.Level <= LogLevel.Debug) {
                    Trace.WriteLine(Layout.Render(logEvent));
                } else if (logEvent.Level == LogLevel.Info) {
                    Trace.TraceInformation(Layout.Render(logEvent));
                } else if (logEvent.Level == LogLevel.Warn) {
                    Trace.TraceWarning(Layout.Render(logEvent));
                } else if (logEvent.Level == LogLevel.Error) {
                    Trace.TraceError(Layout.Render(logEvent));
                } else if (logEvent.Level >= LogLevel.Fatal) {
                    Trace.Fail(Layout.Render(logEvent));
                } else {
                    Trace.WriteLine(Layout.Render(logEvent));
                }
            }
        }

        [TestFixtureSetUp]
        public void SetupFixture() {
            var traceTarget = new MyTraceTarget();
            traceTarget.Layout = "${logger} ${longdate} ${message} \n${exception:format=tostring}";
            var config = new LoggingConfiguration();
            config.AddTarget("trace", traceTarget);
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, traceTarget));
            LogManager.Configuration = config;
        }
    }
}