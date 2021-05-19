
using System;
using System.Linq;
using System.Reactive.Disposables;
using tt_net_sdk;

namespace tt_v1
{
    
    public class Subscription : IDisposable {

        PriceSubscription _pxSub = null;
        IObserver<TTData> _obsvr;
        IObserver<FieldsUpdatedEventArgs> _obsvrRaw;
        public Subscription(Instrument instr, Dispatcher dsptr, IObserver<TTData> obsvr) {
            _obsvr = obsvr;
            _pxSub = new PriceSubscription(instr, dsptr) {
                Settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth)
            };
            _pxSub.FieldsUpdated += PxSub_FieldsUpdated;
            _pxSub.Start();
        }
        public Subscription(Instrument instr, Dispatcher dsptr, IObserver<FieldsUpdatedEventArgs> obsvr) {
            _obsvrRaw = obsvr;
            _pxSub = new PriceSubscription(instr, dsptr) {
                Settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth)
            };
            _pxSub.FieldsUpdated += PxSub_FieldsUpdated;
            _pxSub.Start();
        }

        private void PxSub_FieldsUpdated(object sender, FieldsUpdatedEventArgs evt) {
            if (evt.Error != null) {
                _obsvr?.OnError(evt.Error);
                _obsvrRaw?.OnError(evt.Error);
            }
            else {                   
                _obsvr?.OnNext(evt.ToTTData());
                _obsvrRaw?.OnNext(evt);
            }
        }

        #region Dispose Method
        ~Subscription() { Dispose(false); }
        public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
        protected virtual void Dispose(bool disposing) {
            _pxSub.FieldsUpdated -= PxSub_FieldsUpdated;
            _pxSub?.Dispose();
        }
        #endregion
    }
    
    
        static internal class TTExtention {
        public static TTData ToTTData(this FieldsUpdatedEventArgs qt) {
            var data = new TTData() {
                InstrumentName = qt.Fields.Instrument.Name,
                InstrumentDetialsName = qt.Fields.Instrument.InstrumentDetails.Name,
                InstrumentTickSize = qt.Fields.Instrument.InstrumentDetails.TickSize,
                ProductName = qt.Fields.Instrument.Product.Name,
                RecievedTimeStamp = DateTime.UtcNow,
                TTMarketId = (TTMarketId)qt.Fields.Instrument.Product.Market.MarketId,
                TTProductType = (TTProductType)qt.Fields.Instrument.Product.Type,
                Bid_px = qt.Fields.GetBestBidPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestBidPriceField().Value.Value: null,
                Bid_qty = qt.Fields.GetBestBidPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestBidQuantityField().Value.Value : null,
                Ask_px = qt.Fields.GetBestAskPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestAskPriceField().Value.Value : null,
                Ask_qty = qt.Fields.GetBestAskPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestAskQuantityField().Value.Value : null
            };
            if (qt.Fields.Instrument.Product.Type == ProductType.MultilegInstrument) {
                var lt = qt.Fields.Instrument.Name.Split('-');
                var ins1 = qt.Fields.Instrument.GetLegs().Where(x => x.Instrument.Name == lt[0]).FirstOrDefault();
                var ins2 = qt.Fields.Instrument.GetLegs().Where(x => x.Instrument.Name == lt[1]).FirstOrDefault();
                if (ins1 != null && ins2 != null && ins1.Instrument.Product.Name == ins2.Instrument.Product.Name) {
                    // need to write TTData.Addleg method
                    data.Leg1 = new TTData() { InstrumentName = lt[0],ProductName = ins1.Instrument.Product.Name, InstrumentDetialsName= ins1.Instrument.InstrumentDetails.Name, InstrumentTickSize=ins1.Instrument.InstrumentDetails.TickSize };
                    data.Leg2 = new TTData() { InstrumentName = lt[1],ProductName = ins2.Instrument.Product.Name, InstrumentDetialsName = ins2.Instrument.InstrumentDetails.Name, InstrumentTickSize = ins2.Instrument.InstrumentDetails.TickSize };
                }
            }
            return data;
        }
        
        static void Dispose(CompositeDisposable disp) {
            disp?.Dispose();
            TTAPI.Shutdown();
            Environment.Exit(0);
        }
    }
    
}