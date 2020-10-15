using System;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Codelyzer.Analysis.Common
{
    public static class CommonUtils
    {
        public static void RunGarbageCollection(ILogger logger, string callerInfo)
        {
            try
            {
                logger.LogDebug("CallerInfo: " + callerInfo);
                logger.LogDebug("Memory used before collection:       {0:N0}",
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
                logger.LogDebug("Memory used after full collection:   {0:N0}",
                    GC.GetTotalMemory(false));
            }
        }


        //public static byte[] DownloadFromGitHub(string owner, string repo, string tag)
        //{
        //    using (var client = new HttpClient())
        //    {
        //        client.DefaultRequestHeaders.Add(HttpRequestHeader.Authorization.ToString(), string.Concat("token ", GithubInfo.TestGithubToken));
        //        client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.v3.raw"));
        //        client.DefaultRequestHeaders.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("TestApp", "1.0.0.0"));

        //        var content = client.GetByteArrayAsync(string.Concat("https://api.github.com/repos/", owner, "/", repo, "/zipball/", tag)).Result;
        //        return content;
        //    }
        //}

        //public static void SaveFileFromGitHub(string destination, string owner, string repo, string tag)
        //{
        //    var content = CommonUtils.DownloadFromGitHub(owner, repo, tag);
        //    File.WriteAllBytes(destination, content);
        //}
    }
}
