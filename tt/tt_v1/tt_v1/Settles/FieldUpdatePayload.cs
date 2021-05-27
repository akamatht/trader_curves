using System;

namespace Subscriber
{
    public struct FieldUpdatePayload
    {
        public bool InitialSnapshot { get; set; }
        public DateTime Timestamp { get; set; }
        public long EpochTime { get; set; }
        public string Field { get; set; }
        public string Market { get; set; }
        public string InstrumentKey { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public string InstrumentDetails { get; set; }
        public string ISIN { get; set; }
        public string SeriesTerm { get; set; }
        public string Term { get; set; }
        public long? LotSize { get; set; }
        public string Type { get; set; }    // e.g. Future, Option, Spread
        public string OptionScheme { get; set; }
        public string OptionType { get; set; }
        public decimal? Strike { get; set; }
        public string UnderlyingInstrument_InstrumentKey { get; set; }
        public string UnderlyingInstrument_ProductName { get; set; }
        public string UnderlyingInstrument_ProductDescription { get; set; }
        public string UnderlyingInstrument_InstrumentDetails { get; set; }
        public string UnderlyingInstrument_SeriesTerm { get; set; }
        public string UnderlyingInstrument_Term { get; set; }
        public string UnderlyingInstrument_Type { get; set; }
        public string UnderlyingInstrument_ExpirationDate { get; set; }
        public string UnderlyingInstrument_LastTradingDate { get; set; }
        public string UnderlyingInstrument_MaturityDate { get; set; }
        public string ExpirationDate { get; set; }
        public string LastTradingDate { get; set; }
        public string MaturityDate { get; set; }
        public string Currency { get; set; }
        public string FormattedValue { get; set; }
        public int? ValueTicks { get; set; }
        public decimal? TickSize { get; set; }
        public decimal? DisplayFactor { get; set; }
    }
}
