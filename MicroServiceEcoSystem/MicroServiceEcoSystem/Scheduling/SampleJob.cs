using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem
{
    using Serilog;
    using Quartz;

    public class SampleJob : IJob
    {
        private readonly ILogger _logger;
        private readonly Guid _tellTale = Guid.NewGuid();

        public SampleJob(ILogger logger)
        {
            _logger = logger;
        }

        public Task Execute(IJobExecutionContext context)
        {
            return Task.Run(() =>
            {
                _logger?.Information("Executing sample job");
                _logger?.Information($"Name: {context?.JobDetail?.Key?.Name}");
                _logger?.Information($"Description: '{context?.JobDetail?.Description}'");
                _logger?.Information($"Fire time utc: {context?.FireTimeUtc:yyyy-MM-dd HH:mm:ss zzz}");
                
                foreach (var data in context.JobDetail.JobDataMap)
                {
                    _logger?.Information($"\tKey: {data.Key}; Value: {data.Value}");
                }
                _logger?.Information($"Tell tale: {_tellTale}");
            });
        }
    }
}
