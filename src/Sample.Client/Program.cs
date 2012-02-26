using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.WindowsAzure;
using Rebus;
using Rebus.Bus;
using Rebus.Logging;
using Rebus.Newtonsoft.JsonNET;
using Rebus.Persistence.InMemory;
using Rebus.Transports.Azure.AzureMessageQueue;
using Rebus.Transports.Msmq;
using Sample.Server.Messages;

namespace Sample.Client
{
    class Program : IActivateHandlers, IHandleMessages<object>, IDetermineDestination
    {
        private const string clientQueue = "sample-client";
        private const string serverQueue = "sample-server";

        static void Main()
        {
            try
            {
                Run();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        static void Run()
        {
            RebusLoggerFactory.Current = new TraceLoggerFactory();

            var program = new Program();
            var messageQueue = new MsmqMessageQueue(clientQueue).PurgeInputQueue();
            //var messageQueue = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, clientQueue, true);
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            var jsonMessageSerializer = new JsonMessageSerializer();
            var sagaPersister = new InMemorySagaPersister();
            var inspectHandlerPipeline = new TrivialPipelineInspector();

            var bus = new RebusBus(program,
                                   messageQueue,
                                   messageQueue,
                                   inMemorySubscriptionStorage,
                                   sagaPersister,
                                   program, jsonMessageSerializer, inspectHandlerPipeline);

            bus.Start();

            do
            {
                Console.WriteLine("How many messages would you like to send?");
                var count = int.Parse(Console.ReadLine());

                for (var counter = 1; counter <= count; counter++)
                {
                    dynamic ping = new ExpandoObject();
                    ping.Message = string.Format("Msg. {0}", counter);
                    bus.Send(ping);

                }

            } while (true);
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            return new[] { (IHandleMessages<T>)this };
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void Handle(dynamic message)
        {
            Console.WriteLine("Pong: {0}", message.Message);
        }

        public string GetEndpointFor(Type messageType)
        {
            return serverQueue;
        }
    }
}
