
using System;
using System.Linq;
using tt_net_sdk;

namespace tt_v1
{
    public class Subscription : IDisposable {

        PriceSubscription _pxSub = null;
        IObserver<FieldsUpdatedEventArgs> _obsvrRaw;

        public Subscription(Instrument instr, Dispatcher dsptr,  PriceSubscriptionType priceSubscriptionType, IObserver<FieldsUpdatedEventArgs> obsvr) {
            _obsvrRaw = obsvr;
            _pxSub = new PriceSubscription(instr, dsptr) {
                Settings = new PriceSubscriptionSettings(priceSubscriptionType)
            };
            _pxSub.FieldsUpdated += PxSub_FieldsUpdated;
            _pxSub.Start();
        }

        private void PxSub_FieldsUpdated(object sender, FieldsUpdatedEventArgs evt) {
            if (evt.Error != null) {
                _obsvrRaw?.OnError(evt.Error);
            }
            else {
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
    
    
}