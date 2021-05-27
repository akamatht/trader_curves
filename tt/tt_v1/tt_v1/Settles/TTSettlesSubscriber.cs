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

namespace tt_v1
{
    public class TTSettlesSubscriber
    {
        static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        const string dateFormat = "yyyy-MM-dd";
        private KafkaClient _kafkaClient;
        private readonly WorkerDispatcher _dispatcher;

        public TTSettlesSubscriber(KafkaClient kafkaClient,  WorkerDispatcher dispatcher)
        {
            this._kafkaClient = kafkaClient;
            this._dispatcher = dispatcher;
        }

        public List<Instrument> GetInstruments()
        {
            var json = File.ReadAllText(@"C:\dev\github\akamatht\trader_curves\tt\tt_v1\tt_v1\Settles\OptionSubscriptions.json");
            var priceSubscriptions = (PriceSubscriptions)JsonConvert.DeserializeObject(json, typeof(PriceSubscriptions));

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


        public void start()
        {
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
                var dateTimeNow = DateTimeOffset.Now;

                // Market data snapshot - i.e. the initial set of valus from the point of first subscribing
                if (e.UpdateType == UpdateType.Snapshot)
                {
                    var settlementPrice = e.Fields.GetSettlementPriceField();
                    var indicativeSettlePrice = e.Fields.GetIndicativeSettlPrcField();

                    QueueKafkaAsync(settlementPrice, dateTimeNow, true);
                    QueueKafkaAsync(indicativeSettlePrice, dateTimeNow, true);
                }
                else
                {
                    var changedFields = e.Fields.GetChangedFieldIds();

                    // SettlementPrice field
                    if (changedFields.Contains(FieldId.SettlementPrice))
                    {
                        PriceField settlementPrice = e.Fields.GetSettlementPriceField();
                        QueueKafkaAsync(settlementPrice, dateTimeNow, false);
                    }

                    // IndicativeSettlPrc field
                    if (changedFields.Contains(FieldId.IndicativeSettlPrc))
                    {
                        PriceField indicativeSettlePrice = e.Fields.GetIndicativeSettlPrcField();
                        QueueKafkaAsync(indicativeSettlePrice, dateTimeNow, false);
                    }
                }

            });
        }
        
        async void QueueKafkaAsync(PriceField field, DateTimeOffset timestamp, bool initial)
        {
            if (field != null && field.HasValue)
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var payload = BuildPayload(field, timestamp, initial);
                        var json = JsonSerialize(payload);

                        if (payload.Type == "Future")
                        {
                            _kafkaClient.Publish("dev-tt-settles-futures", payload.InstrumentKey, json);
                        }else if (payload.Type == "Option")
                        {
                            _kafkaClient.Publish("dev-tt-settles-options", payload.InstrumentKey, json);

                        }
                    }
                    catch (Exception e)
                    {
                        _log.Error(e);
                    }
                });
            }
        }

        FieldUpdatePayload BuildPayload(PriceField field, DateTimeOffset timestamp, bool initial)
        {
            FieldUpdatePayload data = new FieldUpdatePayload();

            data.InitialSnapshot = initial;

            data.Timestamp = timestamp.UtcDateTime;     // UTC
            data.EpochTime = timestamp.ToUnixTimeSeconds();

            data.Field = field.FieldId.ToString();
            data.Market = field.Instrument.Product.Market.MarketId.ToString();

            data.InstrumentKey = field.Instrument.Key.ToString();
            data.ProductName = field.Instrument.Product.Name;
            data.ProductDescription = field.Instrument.Product.Description;
            data.ISIN = field.Instrument.InstrumentDetails.ISIN;
            data.InstrumentDetails = field.Instrument.InstrumentDetails.ToString();
            data.SeriesTerm = field.Instrument.InstrumentDetails.SeriesTerm.ToString();
            data.Term = field.Instrument.InstrumentDetails.Term;
            data.LotSize = field.Instrument.InstrumentDetails.LotSize;

            var type = field.Instrument.Product.Type;
            data.Type = type.ToString();

            if (type == ProductType.Option)
            {
                data.OptionType = field.Instrument.InstrumentDetails.OptionType.ToString();
                data.Strike = field.Instrument.InstrumentDetails.StrikePrice;
                data.OptionScheme = field.Instrument.InstrumentDetails.OptionScheme.ToString();

                var underlying = field.Instrument.InstrumentDetails.UnderlyingInstrument;

                data.UnderlyingInstrument_InstrumentKey = underlying.Key.ToString();
                data.UnderlyingInstrument_ProductName = underlying.Product.Name;
                data.UnderlyingInstrument_ProductDescription = underlying.Product.Description;
                data.UnderlyingInstrument_InstrumentDetails = underlying.InstrumentDetails.ToString();
                data.UnderlyingInstrument_SeriesTerm = underlying.InstrumentDetails.SeriesTerm.ToString();
                data.UnderlyingInstrument_Term = underlying.InstrumentDetails.Term;
                data.UnderlyingInstrument_Type = underlying.Product.Type.ToString();
                data.UnderlyingInstrument_ExpirationDate = underlying.InstrumentDetails.ExpirationDate?.Date.ToString(dateFormat);
                data.UnderlyingInstrument_LastTradingDate = underlying.InstrumentDetails.LastTradingDate_?.Date.ToString(dateFormat);
                data.UnderlyingInstrument_MaturityDate = underlying.InstrumentDetails.MaturityDate?.Date.ToString(dateFormat);
            }

            data.ExpirationDate = field.Instrument.InstrumentDetails.ExpirationDate?.Date.ToString(dateFormat);
            data.LastTradingDate = field.Instrument.InstrumentDetails.LastTradingDate_?.Date.ToString(dateFormat);
            data.MaturityDate = field.Instrument.InstrumentDetails.MaturityDate?.Date.ToString(dateFormat);
            data.Currency = field.Instrument.InstrumentDetails.Currency.ToString();
            data.FormattedValue = field.FormattedValue;
            data.ValueTicks = field.Value.ToTicks();
            data.TickSize = field.Instrument.InstrumentDetails.TickSize;
            data.DisplayFactor = field.Instrument.InstrumentDetails.DisplayFactor;

            return data;
        }

        string JsonSerialize(FieldUpdatePayload payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            return json;
        }


    }
}