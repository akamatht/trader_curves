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
using tt_v1.Settles;
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
            
            var aws_bootstrap_servers =
                "b-1.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-2.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-3.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092";
            var nj_bootstrap_servers = "njxmd01.hetco.com:9092";

            var awsKafkaClient = new KafkaClient(aws_bootstrap_servers);
            var njKafkaClient = new KafkaClient(nj_bootstrap_servers);
            // kafkaClient.Run();


            var liveFuturesSubscriber = new LiveFuturesSubscriber(awsKafkaClient, njKafkaClient, dispatcher);
            liveFuturesSubscriber.start();

            // var ttSettlesFuturesSubscriber = new TTSettlesSubscriber(awsKafkaClient, njKafkaClient, dispatcher, InstrumentType.Future);
            // ttSettlesFuturesSubscriber.start();

            // var ttSettlesOptionsSubscriber =
            //     new TTSettlesSubscriber(awsKafkaClient, njKafkaClient, dispatcher, InstrumentType.Option);
            // ttSettlesOptionsSubscriber.start();
            // Console.ReadLine();
            Console.WriteLine("{0}", Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(Timeout.Infinite);
        }

 
    }
}