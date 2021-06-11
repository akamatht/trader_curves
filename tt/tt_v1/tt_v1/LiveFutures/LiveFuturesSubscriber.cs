using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using Newtonsoft.Json;
using Subscriber;
using tt_net_sdk;
using tt_v1.Models;
using tt_v1.Transformers;

namespace tt_v1
{
    public class LiveFuturesSubscriber
    {
        private KafkaClient _awsKafkaClient;
        private KafkaClient _njKafkaClient;
        private readonly WorkerDispatcher _dispatcher;
        private IDictionary<string,string> tt2qep = new Dictionary<string, string>();
        private IDictionary<string, string> qep2tt = new Dictionary<string, string>() { { "STAT", "STAT" } };
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        public LiveFuturesSubscriber(KafkaClient awsKafkaClient, KafkaClient njKafkaClient, WorkerDispatcher dispatcher)
        {
            _awsKafkaClient = awsKafkaClient;
            _njKafkaClient = njKafkaClient;
            _dispatcher = dispatcher;
        }


        public List<Instrument> GetInstruments()
        {
            var instrs = new List<Instrument>();
            string[] ice = { "BRN", "G", "GWM" };
            foreach (var pdt in ice) {
                instrs.AddRange(Query(MarketId.ICE, ProductType.Future, pdt));
                instrs.AddRange(Query(MarketId.ICE, ProductType.MultilegInstrument, pdt).Where(x => x.InstrumentDetails.ComboCode == ComboCode.Calendar));
            }

            string[] cme = { "RB", "CL", "HO", "NG" };
            foreach (var pdt in cme) {
                instrs.AddRange(Query(MarketId.CME, ProductType.Future, pdt));
                instrs.AddRange(Query(MarketId.CME, ProductType.MultilegInstrument, pdt).Where(x => x.InstrumentDetails.ComboCode == ComboCode.Calendar));
            }

            return instrs;
        }
        
        protected IReadOnlyCollection<Instrument> Query(MarketId mktId, ProductType pdtType, string group) {
            var qry = new InstrumentCatalog(mktId, pdtType, group, this._dispatcher);
            var evt = qry.Get();
            if (evt != ProductDataEvent.Found)
                throw new Exception("ERROR");
            return qry.InstrumentList;
        }

        public void PublishInstruments()
        {
            var instruments = GetInstruments();
            instruments.ToObservable<Instrument>()
                .Select<Instrument, MosaicInstrument>(InstrumentTransformer.ToMosaicInstrument)
                .Subscribe(
                    
                    mosInstr =>
                    {
                        Console.WriteLine($"Publishing instrument {mosInstr.InstrumentKey}");
                        _njKafkaClient.Publish("dev-tt-instruments", mosInstr.InstrumentName,
                            JsonConvert.SerializeObject(mosInstr));
                    });
        }
        

        public void start()
        {

            var instrs = GetInstruments();
            var baseSubscription = instrs.Select(instr =>
                    {
                        Console.WriteLine("Subscribing to instrument {0} on {1}", instr,
                            Thread.CurrentThread.ManagedThreadId);
                        tt2qep[instr.InstrumentDetails.Name] = instr.ToSymbol();
                        qep2tt[instr.ToSymbol()] = instr.InstrumentDetails.Name;
                        return Observable.Create<FieldsUpdatedEventArgs>(obs =>
                            new Subscription(instr, _dispatcher, PriceSubscriptionType.MarketDepth, obs)).Catch<FieldsUpdatedEventArgs, Exception>(
                            ex =>
                            {
                                Console.WriteLine(ex.Message);
                                return Observable.Empty<FieldsUpdatedEventArgs>();
                            });
                    }
                )
                .Merge()
                .Publish()
                .RefCount();
            
    
                var kafkaPublish = baseSubscription
                    .SubscribeOn(NewThreadScheduler.Default)
                    .ObserveOn(NewThreadScheduler.Default)
                    .Subscribe(d =>
                    {
                        Console.WriteLine("Data {0} {1}", d, Thread.CurrentThread.ManagedThreadId);
                        var mosaicPrice = PriceTransformer.ToMosaicPrice(d);
                        // return ttData;
                        _njKafkaClient.Publish("dev-tt-live-prices", mosaicPrice.InstrumentKey, JsonConvert.SerializeObject(mosaicPrice));
                        
                    }
                    );
                
                _disposable.Add(kafkaPublish);
    
                // var cache = new ConcurrentDictionary<string, TTData>();
                //
                //     
                //     
                // var cacheSubscription = baseSubscription
                //     .Select(f => FuturesExtension.ToTTData(f))
                //     .ObserveOn(NewThreadScheduler.Default)
                //     .Subscribe(d =>
                //     {
                //         Console.WriteLine("Caching symbol {0}", d.InstrumentName);
                //         cache[d.InstrumentName] = d;
                //     });
                //
                // disposable.Add(cacheSubscription);

        }
    }
}