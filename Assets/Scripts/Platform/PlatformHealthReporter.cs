using JetBrains.Annotations;
using MVC;
using UnityEngine;

namespace Platform
{
    /// <summary>
    /// Forwards Unity's log stream to the platform health API.
    ///
    /// This is the only crash telemetry Playables offers, and it is best-effort and rate-limited on
    /// the platform side, so this deliberately sends less rather than more: errors and exceptions
    /// always, warnings only if asked, and nothing at all past a per-session cap.
    /// </summary>
    [UsedImplicitly]
    public class PlatformHealthReporter : AbstractController
    {
        /// <summary>
        /// A single broken frame can log the same exception every Update. Past this the report is
        /// noise, and the platform is dropping it anyway.
        /// </summary>
        private const int MaxReportsPerSession = 50;

        private readonly IPlatformServices _platformServices;
        private readonly bool _reportWarnings;

        private int _reportCount;
        private bool _isReporting;

        public PlatformHealthReporter(IPlatformServices platformServices, bool reportWarnings)
        {
            _platformServices = platformServices;
            _reportWarnings = reportWarnings;

            Application.logMessageReceived += OnLogMessageReceived;
        }

        public override void Dispose()
        {
            Application.logMessageReceived -= OnLogMessageReceived;

            base.Dispose();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            // Reporting can itself log - a failed save logs an error, and the platform call could
            // log too. Without this guard the first error recurses until the stack gives out.
            if (_isReporting || _reportCount >= MaxReportsPerSession)
            {
                return;
            }

            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    Report(() => _platformServices.LogError(Format(condition, stackTrace)));
                    break;

                case LogType.Warning when _reportWarnings:
                    Report(() => _platformServices.LogWarning(condition));
                    break;
            }
        }

        private void Report(System.Action report)
        {
            _isReporting = true;
            _reportCount++;

            try
            {
                report();
            }
            finally
            {
                _isReporting = false;
            }
        }

        private static string Format(string condition, string stackTrace)
        {
            return string.IsNullOrEmpty(stackTrace) ? condition : $"{condition}\n{stackTrace}";
        }
    }
}
