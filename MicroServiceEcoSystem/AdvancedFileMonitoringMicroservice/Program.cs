namespace AdvancedFileMonitoringMicroservice
{
    using Grumpy.ServiceBase;

    /// <summary>   A program. </summary>
    class Program
    {
        ////////////////////////////////////////////////////////////////////////////////////////////////////
        /// <summary>   Main entry-point for this application. </summary>
        ///
        /// <param name="args"> An array of command-line argument strings. </param>
        ////////////////////////////////////////////////////////////////////////////////////////////////////

        static void Main(string[] args)
        {
            TopshelfUtility.Run<Microservice>();
        }
    }
}
