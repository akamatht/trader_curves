using System;
using System.Linq;
using tt_net_sdk;

namespace tt_v1
{
    public static class FuturesExtension
    {
                
        public static TTData ToTTData(FieldsUpdatedEventArgs qt) {
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

    }
}