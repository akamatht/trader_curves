using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.ServiceModel.Configuration;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Newtonsoft.Json;
using Subscriber;
using tt_net_sdk;
using Dispatcher = tt_net_sdk.Dispatcher;

namespace tt_v1
{
    public class App
    {
        static Dictionary<string, string> tt2qep = new Dictionary<string, string>();
        static Dictionary<string, string> qep2tt = new Dictionary<string, string>() { { "STAT", "STAT" } };

        public static void Main(string[] args)
        {
            
            AutoResetEvent waitForTTInit = new AutoResetEvent(false);
            Action<string> onInit = str => { 
                Console.WriteLine(str);
                waitForTTInit.Set();
            };
            WorkerDispatcher dispatcher = new WorkerDispatcher();
            TTDriver driver = new TTDriver(onInit, dispatcher);
            driver.start();
            waitForTTInit.WaitOne();
            
            

            var bootstrap_servers =
                "b-1.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-2.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-3.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092";

            var kafkaClient = new KafkaClient(bootstrap_servers);
            // kafkaClient.Run();


            var qepSubscriber = new LiveFuturesSubscriber(kafkaClient, dispatcher);
            qepSubscriber.start();

            // var ttSettlesSubscriber = new TTSettlesSubscriber(kafkaClient, dispatcher);
            // ttSettlesSubscriber.start();
            
            // Console.ReadLine();
            Console.WriteLine("{0}", Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(Timeout.Infinite);
        }

 
    }
}