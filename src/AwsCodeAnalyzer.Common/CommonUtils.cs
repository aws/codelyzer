using System;
using System.Runtime;
using Serilog;

namespace AwsCodeAnalyzer.Common
{
    public static class CommonUtils
    {
        public static void RunGarbageCollection(ILogger logger, string callerInfo)
        {
            try
            {
                logger.Debug("CallerInfo: " + callerInfo);
                logger.Debug("Memory used before collection:       {0:N0}",
                    GC.GetTotalMemory(false));
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            catch (System.Exception)
            {
                //sometimes GC.Collet/WaitForPendingFinalizers crashes
            }
            finally
            {
                logger.Debug("Memory used after full collection:   {0:N0}",
                    GC.GetTotalMemory(false));
            }
        }
    }
}
