﻿using System;
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

namespace Sample.Server
{
    class Program : IActivateHandlers, IHandleMessages<object>, IDetermineDestination
    {
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
            var messageQueue = new MsmqMessageQueue(serverQueue);
            //var messageQueue = new AzureMessageQueue(CloudStorageAccount.DevelopmentStorageAccount, serverQueue, true);
            var inMemorySubscriptionStorage = new InMemorySubscriptionStorage();
            var jsonMessageSerializer = new JsonMessageSerializer();
            var sagaPersister = new InMemorySagaPersister();
            var inspectHandlerPipeline = new TrivialPipelineInspector();

            var bus = new RebusBus(program,
                                   messageQueue,
                                   messageQueue,
                                   inMemorySubscriptionStorage,
                                   sagaPersister,
                                   program, 
                                   jsonMessageSerializer, 
                                   inspectHandlerPipeline);

            program.Bus = bus;

            bus.Start();

            Console.WriteLine("Server listening...");
            Console.ReadKey();
        }

        public IEnumerable<IHandleMessages<T>> GetHandlerInstancesFor<T>()
        {
            if (typeof(T) == typeof(object))
            {
                return new[] { (IHandleMessages<T>)this };
            }

            return new IHandleMessages<T>[0];
        }

        public void Release(IEnumerable handlerInstances)
        {
        }

        public void Handle(dynamic message)
        {
            Console.WriteLine("Got {0}", message.Message);

            dynamic pong = new ExpandoObject();
            pong.Message = message.Message;
            Bus.Reply(pong);
        }

        public RebusBus Bus { get; set; }

        public string GetEndpointFor(Type messageType)
        {
            throw new NotImplementedException();
        }
    }
}
