using System;
using System.Diagnostics;
using System.Linq;
using tt_net_sdk;

namespace tt_v1
{
    static internal class UtilExt {
        internal static string ToSymbol(this Instrument ins) {
            switch (ins.Product.Market.MarketId) {
                //move to config
                case MarketId.ICE: {
                    string bs = "";
                    if (ins.Product.Name == "G")
                        bs = "LSGO";
                    else if (ins.Product.Name == "BRN")
                        bs = "CO";
                    else if (ins.Product.Name == "GWM")
                        bs = "NBP";
                    var lt = ins.Name.Split(new char[] { '-', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    var ret = $"{bs}{lt[1][2]}{lt[1][6]}";
                    if (lt.Count == 4)
                        ret += $"-{bs}{lt[3][2]}{lt[3][6]}";
                    return ret;
                }
            }
            return ins.Name;
        }

        // /// <summary>
        // /// Convert TT.Net Quote to Bbo
        // /// </summary>
        // /// <param name="qt"></param>
        // /// <returns></returns>
        // internal static Bbo ToBbo(this TTData qt) {
        //     var bid_px = qt.Bid_px;
        //     var ask_px = qt.Ask_px;
        //     var bid_qty = qt.Bid_qty;
        //     var ask_qty = qt.Ask_qty;
        //     Trace.WriteLine($"\n{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff")} {qt.InstrumentName} {bid_px}/{ask_px}");
        //
        //     var px_ftr = (qt.TTMarketId == TTMarketId.ICE) ? 100 : 1;
        //
        //     var bbo = new Bbo { BookType = BookType.Direct, RecievedTime = qt.RecievedTimeStamp };
        //     if (ask_px != null) {
        //         bbo.AskPr = decimal.ToDouble(ask_px.Value * px_ftr);
        //         bbo.AskQt = decimal.ToUInt32(ask_qty.Value);
        //     }
        //     if (bid_px != null) {
        //         bbo.BidPr = decimal.ToDouble(bid_px.Value * px_ftr);
        //         bbo.BidQt = decimal.ToUInt32(bid_qty.Value);
        //     }
        //
        //     return bbo;
        // }
    }

}