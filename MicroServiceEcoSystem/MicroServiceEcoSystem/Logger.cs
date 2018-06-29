using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroServiceEcoSystem
{
    using Autofac;
    using Serilog;

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    /// <summary>   A logger. </summary>
    ///
    /// <seealso cref="T:Autofac.Module"/>
    ////////////////////////////////////////////////////////////////////////////////////////////////////

    public class Logger : Module
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Override to add registrations to the container. </summary>
        ///
        /// <remarks>   Note that the ContainerBuilder parameter is unique to this module. </remarks>
        ///
        /// <param name="builder">  The builder through which components can be registered. </param>
        ///
        /// <seealso cref="M:Autofac.Module.Load(ContainerBuilder)"/>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        protected override void Load(ContainerBuilder builder)
        {
            var config = new LoggerConfiguration().MinimumLevel.Verbose()
                .WriteTo.Trace()
                .WriteTo.LiterateConsole();

            var logger = config.CreateLogger();
            Log.Logger = logger;

            builder.RegisterInstance(logger).As<ILogger>();
        }
    }
}
