using System;

namespace tt_v1.Models
{
    public class MosaicInstrument
    {
        public string InstrumentKey { get; set; } 
        public string InstrumentName { get; set; }
        public string InstrumentDetailsName { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string InstrumentType { get; set; }
        public decimal InstrumentTickSize { get; set; }
        public string Market { get; set; }
        public string Isin { get; set; }
        //Future(Single or MultiLeg) or Option 
        public string Type { get; set; }
        public string Currency { get; set; }
        public double? LotSize { get; set; }
        public double? TickSize { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? LastTradingDate { get; set; }
        public DateTime? MaturityDate { get; set; }

        public string Term { get; set; }
        public string SeriesTerm { get; set; }

        //Option specific
        public string OptionScheme { get; set; }
        public string OptionType { get; set; }
        public double? Strike { get; set; }
        public MosaicInstrument? UnderlyingInstrument { get; set; }
        //Multi-leg instrument
        public MosaicInstrument? Leg1 { get; set; }
        public MosaicInstrument? Leg2 { get; set; }

    }
}