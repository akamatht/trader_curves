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
using CommandLine;
using Newtonsoft.Json;
using Subscriber;
using tt.messaging.order.enums;
using tt_net_sdk;
using tt_v1.Settles;
using Dispatcher = tt_net_sdk.Dispatcher;

namespace tt_v1
{
    public class App
    {

        public class Options
        {
            [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
            public bool Verbose { get; set; }

            [Option('l', "live-futures", Required = false, HelpText = "Subscribe to Live Futures prices")]
            public bool LiveFutures { get; set; }

            [Option('f', "future-settles", Required = false, HelpText = "Subscribe to Future Settles")]
            public bool SettlesFutures { get; set; }

            [Option('o', "option-settles", Required = false, HelpText = "Subscribe to Option Settles")]
            public bool SettlesOptions { get; set; }

        }
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

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(o =>
                {
                    if (o.LiveFutures)
                    {
                        var liveFuturesSubscriber = new LiveFuturesSubscriber(awsKafkaClient, njKafkaClient, dispatcher);
                        liveFuturesSubscriber.start();
                    }
                    else if (o.SettlesFutures)
                    {
                        var ttSettlesFuturesSubscriber = new TTSettlesSubscriber(awsKafkaClient, njKafkaClient, dispatcher, InstrumentType.Future);
                        ttSettlesFuturesSubscriber.start();
                    }else if (o.SettlesOptions)
                    {
                        var ttSettlesOptionsSubscriber =
                            new TTSettlesSubscriber(awsKafkaClient, njKafkaClient, dispatcher, InstrumentType.Option);
                        ttSettlesOptionsSubscriber.start();
                    }
                    else
                    {
                        Console.WriteLine($"Choose one of the options (-f -l -l -o)");
                        Environment.Exit(-1);
                    }
                });
            
            Console.WriteLine("{0}", Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(Timeout.Infinite);
        }

 
    }
}