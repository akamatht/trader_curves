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
            var disposable = new CompositeDisposable();
            Action<string> onInit = str => { 
                Console.WriteLine(str);
                waitForTTInit.Set();
            };
            WorkerDispatcher dispatcher = new WorkerDispatcher();
            TTDriver driver = new TTDriver(onInit, dispatcher);
            driver.start();
            waitForTTInit.WaitOne();
            
            
            var instrs = new List<Instrument>();
            string[] ice = { "BRN", "G", "GWM" };
            foreach (var pdt in ice) {
                instrs.AddRange(driver.Query(MarketId.ICE, ProductType.Future, pdt));
                instrs.AddRange(driver.Query(MarketId.ICE, ProductType.MultilegInstrument, pdt).Where(x => x.InstrumentDetails.ComboCode == ComboCode.Calendar));
            }

            string[] cme = { "RB", "CL", "HO", "NG" };
            foreach (var pdt in cme) {
                instrs.AddRange(driver.Query(MarketId.CME, ProductType.Future, pdt));
                instrs.AddRange(driver.Query(MarketId.CME, ProductType.MultilegInstrument, pdt).Where(x => x.InstrumentDetails.ComboCode == ComboCode.Calendar));
            }

            
            var baseSubscription = instrs.Select(instr =>
                    {
                        Console.WriteLine("Subscribing to instrument {0} on {1}", instr,
                            Thread.CurrentThread.ManagedThreadId);
                        tt2qep[instr.InstrumentDetails.Name] = instr.ToSymbol();
                        qep2tt[instr.ToSymbol()] = instr.InstrumentDetails.Name;

                        return Observable.Create<TTData>(obs => 
                                new Subscription(instr, dispatcher, obs))
                            .Catch<TTData, Exception>(ex =>
                                {
                                    Console.WriteLine(ex.Message);
                                    return Observable.Empty<TTData>();
                                });
                    }
                )
                .Merge()
                .Publish()
                .RefCount();
                

            var bootstrap_servers =
                "b-1.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-2.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092,b-3.kafka01.mb3vi8.c4.kafka.us-east-1.amazonaws.com:9092";

            var kafkaClient = new KafkaClient(bootstrap_servers, topic: "dev-tt-prices-test");
            kafkaClient.Run();
            
            var kafkaPublish = baseSubscription
                    .ObserveOn(NewThreadScheduler.Default)
                    .Subscribe(d =>
                    {
                        Console.WriteLine("Data {0} {1}", d, Thread.CurrentThread.ManagedThreadId);
                        kafkaClient.Publish(d.InstrumentName, JsonConvert.SerializeObject(d));                        
                    }
                    );

            var cache = new ConcurrentDictionary<string, TTData>();

                
                
            var cacheSubscription = baseSubscription
                .ObserveOn(NewThreadScheduler.Default)
                .Subscribe(d =>
                {
                    Console.WriteLine("Caching symbol {0}", d.InstrumentName);
                    cache[d.InstrumentName] = d;
                });
                

            disposable.Add(kafkaPublish);
            disposable.Add(cacheSubscription);
            
            // Console.ReadLine();
            Console.WriteLine("{0}", Thread.CurrentThread.ManagedThreadId);
            Thread.Sleep(Timeout.Infinite);
        }

 
    }
}