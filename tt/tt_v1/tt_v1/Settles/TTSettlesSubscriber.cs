using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Subscriber;
using tt_net_sdk;
using tt_v1.Models;
using tt_v1.Settles;
using tt_v1.Transformers;

namespace tt_v1
{
    public class TTSettlesSubscriber
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const string dateFormat = "yyyy-MM-dd";
        private readonly KafkaClient _awsKafkaClient;
        private readonly KafkaClient _njKafkaClient;
        private readonly WorkerDispatcher _dispatcher;
        private readonly InstrumentType _instrumentType;

        public TTSettlesSubscriber(KafkaClient awsKafkaClient, KafkaClient njKafkaClient, WorkerDispatcher dispatcher, InstrumentType instrumentType)
        {
            this._awsKafkaClient = awsKafkaClient;
            this._njKafkaClient = njKafkaClient;
            this._dispatcher = dispatcher;
            this._instrumentType = instrumentType;
        }

        public List<Instrument> GetInstruments()
        {
            PriceSubscriptions priceSubscriptions = null;
            if (_instrumentType == InstrumentType.Option)
            {
                var json = File.ReadAllText(
                    @"C:\dev\github\akamatht\trader_curves\tt\tt_v1\tt_v1\Settles\OptionSubscriptions.json");
                priceSubscriptions =
                    (PriceSubscriptions) JsonConvert.DeserializeObject(json, typeof(PriceSubscriptions));
            }
            else if (_instrumentType == InstrumentType.Future)
            {
                var json = File.ReadAllText(
                    @"C:\dev\github\akamatht\trader_curves\tt\tt_v1\tt_v1\Settles\FuturesSubscriptions.json");
                priceSubscriptions =
                    (PriceSubscriptions) JsonConvert.DeserializeObject(json, typeof(PriceSubscriptions));
            }

            List<Instrument> contracts = new List<Instrument>();

            foreach (var p in priceSubscriptions.Products)
            {
                var exchange = (MarketId)Enum.Parse(typeof(MarketId), p.Exchange);
                var productType = (ProductType)Enum.Parse(typeof(ProductType), p.Type);
                var product = p.Code;

                contracts.AddRange(GetContracts(exchange, productType, product));
            }

            return contracts;
        }
        
        IEnumerable<Instrument> GetContracts(MarketId market, ProductType productType, string productCode)
        {
            InstrumentCatalog instruments = new InstrumentCatalog(market, productType, productCode, _dispatcher);
            ProductDataEvent e = instruments.Get();

            var subscriptionDetails = $"{market.ToString()} : {productType.ToString()} : {productCode}";

            if (e == ProductDataEvent.Found)
            {
                _log.Info($"Fetched instrument count for '{productCode}': {subscriptionDetails} : {instruments.InstrumentList.Count()}");
                return instruments.InstrumentList;
            }
            else
            {
                _log.Error($"Failed to fetch intruments for: {subscriptionDetails}");
                return Enumerable.Empty<Instrument>();
            }
        }

        private void PublishInstruments()
        {
            Console.WriteLine($"Begin publishing settles {_instrumentType} instruments");
            var instruments = GetInstruments();
            instruments.ToObservable().Subscribe(instr =>
            {
                var mosInstrument = InstrumentTransformer.ToMosaicInstrument(instr);
                var payload = JsonConvert.SerializeObject(mosInstrument, Formatting.None, new JsonSerializerSettings()
                { 
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _awsKafkaClient.Publish("dev-tt-settles-instruments", mosInstrument.InstrumentKey, payload);
                _njKafkaClient.Publish("dev-tt-instruments", mosInstrument.InstrumentKey, payload);
            });
            Console.WriteLine($"End publishing settles {instruments.Count} {_instrumentType} instruments");
        }

        public void start()
        {
            PublishInstruments();
            var instrs = GetInstruments();
            var baseSubscription = instrs.Select(instr =>
                    {
                        Console.WriteLine("Subscribing to instrument {0} on {1}", instr,
                            Thread.CurrentThread.ManagedThreadId);
                        return Observable.Create<FieldsUpdatedEventArgs>(obs =>
                            new Subscription(instr, this._dispatcher, PriceSubscriptionType.InsideMarket, obs)).Catch<FieldsUpdatedEventArgs, Exception>(
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


            baseSubscription.Subscribe(e =>
            {
                var mosaicPrice = PriceTransformer.ToMosaicPrice(e);
                var payload = JsonConvert.SerializeObject(mosaicPrice);
                //Settled and Indicative settle prices for futures should be published on njkafka. Rest all should be on 
                if(e.Fields.Instrument.Product.Type != ProductType.Option)
                    _njKafkaClient.Publish("dev-tt-live-prices", mosaicPrice.InstrumentKey, payload);

                //All prices should be published to awsk kafka
                if (e.Fields.Instrument.Product.Type == ProductType.Future)
                {
                    _awsKafkaClient.Publish("dev-tt-settles-futures", mosaicPrice.InstrumentKey, payload);
                }
                else if (e.Fields.Instrument.Product.Type == ProductType.Option)
                {
                    _awsKafkaClient.Publish("dev-tt-settles-options", mosaicPrice.InstrumentKey, payload);
                }

            });
        }

    }
}