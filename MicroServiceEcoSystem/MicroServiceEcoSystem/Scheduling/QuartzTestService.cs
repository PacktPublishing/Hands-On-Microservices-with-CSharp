using Quartz;
using Serilog;

namespace MicroServiceEcoSystem
{
    using JetBrains.Annotations;
    using Topshelf;

    public class QuartzTestService : BaseMicroService
    {
        private readonly IScheduler _jobScheduler;
        private readonly ILogger _logger;
        private HostControl _hc;

        public QuartzTestService([NotNull] IScheduler jobScheduler, [NotNull] ILogger logger)
        {
            _jobScheduler = jobScheduler;
            _logger = logger;
        }

        public bool Start([CanBeNull] HostControl host)
        {
            base.Start(host);
            _hc = host;
            _jobScheduler?.Start();
            _logger?.Information(string.Intern("Job scheduler started"));
            return true;
        }

        public bool Stop()
        {
            base.Stop();
            _jobScheduler?.Shutdown(true);
            _logger?.Information(string.Intern("Job scheduler stopped"));
            return true;
        }
    }
}