namespace tt_v1.Models
{
    public class MosaicPrice
    {
        public string InstrumentKey { get; set; }
        //Either a FuturesPrice,IndicativeSettles,SettlesPrice
        public string PriceType { get; set; }
        public decimal? Bid_px { get; set;  }
        public decimal? Ask_px { get; set;  }
        public decimal? Bid_qty { get; set; }
        public decimal? Ask_qty { get; set; }

        public decimal? IndicativePrice { get; set; }
        public decimal? SettlePrice { get; set; }

    }
}