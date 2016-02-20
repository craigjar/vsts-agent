﻿using System;
using System.Diagnostics;
using Microsoft.VisualStudio.Services.Agent;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.TeamFoundation.DistributedTask.WebApi;

namespace Microsoft.VisualStudio.Services.Agent.Worker
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            RunAsync(args).Wait();
        }        

        public static async Task RunAsync(string[] args)
        {
            HostContext hc = new HostContext("Worker");
            Console.WriteLine("Hello Worker!");
            
#if OS_WINDOWS
            Console.WriteLine("Hello Windows");
#endif
            
#if OS_OSX
            Console.WriteLine("Hello OSX");
#endif

#if OS_LINUX
            Console.WriteLine("Hello Linux");
#endif

            TraceSource m_trace = hc.Trace["WorkerProcess"];
            m_trace.Info("Info Hello Worker!");
            m_trace.Warning("Warning Hello Worker!");
            m_trace.Error("Error Hello Worker!");
            m_trace.Verbose("Verbos Hello Worker!");

            //JobRunner jobRunner = new JobRunner(hc);
            //jobRunner.Run();

            JobRunner jobRunner = null;
            Func<CancellationToken, JobCancelMessage, Task> cancelHandler = (token, message) => {
                hc.CancellationTokenSource.Cancel();
                return Task.CompletedTask;
            };

            Func<CancellationToken, JobRequestMessage, Task> newRequestHandler = async (token, message) => {
                await jobRunner.Run(message);
            };

            if (null != args && 3 == args.Length && "spawnclient".Equals(args[0].ToLower()))
            {
                using (var channel = hc.GetService<IProcessChannel>())
                {
                    channel.JobRequestMessageReceived += newRequestHandler;
                    channel.JobCancelMessageReceived += cancelHandler;
                    jobRunner = new JobRunner(hc);
                    channel.StartClient(args[1], args[2]);
                    await jobRunner.WaitToFinish(hc);
                    channel.JobRequestMessageReceived -= newRequestHandler;
                    channel.JobCancelMessageReceived -= cancelHandler;
                    await channel.Stop();
                }
            }
            
            hc.Dispose();
        }
    }
}
