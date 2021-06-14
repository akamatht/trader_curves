using tt_net_sdk;
using tt_v1.Models;

namespace tt_v1.Transformers
{
    public static class PriceTransformer
    {
        public static MosaicPrice ToMosaicPrice(FieldsUpdatedEventArgs qt) {
            var data = new MosaicPrice() {
                InstrumentKey = qt.Fields.Instrument.Key.ToString(),
                PriceType = qt.Fields.Instrument.Product.Type.ToString(),
                Bid_px = qt.Fields.GetBestBidPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestBidPriceField().Value.Value: null,
                Bid_qty = qt.Fields.GetBestBidPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestBidQuantityField().Value.Value : null,
                Ask_px = qt.Fields.GetBestAskPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestAskPriceField().Value.Value : null,
                Ask_qty = qt.Fields.GetBestAskPriceField().Value.IsValid ? (decimal?)qt.Fields.GetBestAskQuantityField().Value.Value : null,
                SettlePrice = qt.Fields.GetSettlementPriceField().Value.IsValid ? (decimal?)qt.Fields.GetSettlementPriceField().Value.Value: null,
                IndicativePrice = qt.Fields.GetIndicativePriceField().Value.IsValid ? (decimal?)qt.Fields.GetIndicativePriceField().Value.Value: null
            };

            return data;
        }
    }
}