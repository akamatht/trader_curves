using System;
using System.Linq;
using tt_net_sdk;
using tt_v1.Models;

namespace tt_v1.Transformers
{
    public static class InstrumentTransformer
    {
        public static MosaicInstrument ToMosaicInstrument(Instrument ttInstrument)
        {
            if (ttInstrument == null)
            {
                return null;
            }
            
            
            var mosaicInstrument = new MosaicInstrument()
            {
               InstrumentName = ttInstrument.Name,
               InstrumentDetailsName = ttInstrument.InstrumentDetails.Name,
               InstrumentKey = ttInstrument.Key.ToString(),
               Isin = ttInstrument.InstrumentDetails.ISIN,
               TickSize = Convert.ToDouble(ttInstrument.InstrumentDetails.TickSize),
               ProductName = ttInstrument.Product.Name,
               Currency = ttInstrument.InstrumentDetails.Currency.ToString(),
               ExpirationDate = ttInstrument.InstrumentDetails.ExpirationDate,
               Type = ttInstrument.Product.Type.ToString(),
               Market = ttInstrument.Product.Market.ToString(),
               LastTradingDate = ttInstrument.InstrumentDetails.LastTradingDate_,
               MaturityDate = ttInstrument.InstrumentDetails.MaturityDate,
               LotSize = ttInstrument.InstrumentDetails.LotSize
            };

            if (ttInstrument.Product.Type == ProductType.MultilegInstrument)
            {
                var lt = ttInstrument.Name.Split('-');
                var ins1 = ttInstrument.GetLegs().FirstOrDefault(x => x.Instrument.Name == lt[0]);
                var ins2 = ttInstrument.GetLegs().FirstOrDefault(x => x.Instrument.Name == lt[1]);
                if (ins1 != null && ins2 != null && ins1.Instrument.Product.Name == ins2.Instrument.Product.Name) {
                    // need to write TTData.Addleg method
                    mosaicInstrument.Leg1 = new MosaicInstrument() { InstrumentName = lt[0],ProductName = ins1.Instrument.Product.Name, InstrumentDetailsName= ins1.Instrument.InstrumentDetails.Name, InstrumentTickSize=ins1.Instrument.InstrumentDetails.TickSize };
                    mosaicInstrument.Leg2 = new MosaicInstrument() { InstrumentName = lt[1],ProductName = ins2.Instrument.Product.Name, InstrumentDetailsName = ins2.Instrument.InstrumentDetails.Name, InstrumentTickSize = ins2.Instrument.InstrumentDetails.TickSize };
                }
            }

            if (ttInstrument.Product.Type == ProductType.Option)
            {
                mosaicInstrument.OptionType = ttInstrument.InstrumentDetails.OptionType.ToString();
                mosaicInstrument.Strike = Convert.ToDouble(ttInstrument.InstrumentDetails.StrikePrice);
                mosaicInstrument.OptionScheme = ttInstrument.InstrumentDetails.OptionScheme.ToString();

                var ttUnderlyingInstrument = ttInstrument.InstrumentDetails.UnderlyingInstrument;
                if (ttUnderlyingInstrument != null)
                {
                    var mosaicUnderlyingInstrument = new MosaicInstrument()
                    {
                        InstrumentKey = ttUnderlyingInstrument.Key.ToString(),
                        ProductName = ttUnderlyingInstrument.Product.Name,
                        SeriesTerm = ttUnderlyingInstrument.InstrumentDetails.SeriesTerm.ToString(),
                        Term = ttUnderlyingInstrument.InstrumentDetails.Term,
                        Type = ttUnderlyingInstrument.Product.Type.ToString(),
                        ExpirationDate = ttUnderlyingInstrument.InstrumentDetails.ExpirationDate,
                        LastTradingDate = ttUnderlyingInstrument.InstrumentDetails.LastTradingDate_,
                        MaturityDate = ttUnderlyingInstrument.InstrumentDetails.MaturityDate
                    };
                    mosaicInstrument.UnderlyingInstrument = mosaicInstrument;
                }

            }

            return mosaicInstrument;
     
        }
    }
}