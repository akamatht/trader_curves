// using System;
// using System.Collections.Generic;
// using System.Threading;
// using tt_net_sdk;
//
// namespace tt_v1
// {
//     internal class Program
//     {
//         public static void Main(string[] args)
//         {
//             
//             try
//             {
//                 // Add your app secret Key here. It looks like: 00000000-0000-0000-0000-000000000000:00000000-0000-0000-0000-000000000000
//                 string appSecretKey = "7f6d1dac-4e09-00a4-1a60-b227db734ba9:f29145c6-d8a1-5b88-1a6f-5770ac26211a";
//                 
//                 //Set the environment the app needs to run in here
//                 tt_net_sdk.ServiceEnvironment environment = tt_net_sdk.ServiceEnvironment.ProdLive;
//
//                 tt_net_sdk.TTAPIOptions apiConfig = new tt_net_sdk.TTAPIOptions(
//                     environment,
//                     appSecretKey,
//                     5000);
//
//                 // Start the TT API on the same thread
//                 TTNetApiFunctions tf = new TTNetApiFunctions((str) => TfOnTT_Initialised());
//                
//
//                 Thread workerThread = new Thread(() => tf.Start(apiConfig));
//                 workerThread.Name = "TT NET SDK Thread";
//                 workerThread.Start();
//
//                 while (true)
//                 {
//                     string input = System.Console.ReadLine();
//                     if (input == "q")
//                         break;
//                 }
//                 tf.Dispose();
//             }
//             catch (Exception e)
//             {
//                 Console.WriteLine(e.Message + "\n" + e.StackTrace);
//             }
//         }
//
//         private static void TfOnTT_Initialised()
//         {
//             // string[] pdts = { "RB", "CL", "HO", "NG", "BRN", "G" };
//             // ProductCatalog prod_cat = new ProductCatalog(MarketId.CME, tt_net_sdk.Dispatcher.Current);
//             //
//             // ProductDataEvent e = prod_cat.Get();
//             // if (e == ProductDataEvent.Found)
//             // {
//             //     // Products were found
//             //     foreach (KeyValuePair<ProductKey, Product> kvp in prod_cat.Products)
//             //     {
//             //         Console.WriteLine("Key = {0} : Value = {1}", kvp.Key, kvp.Value);
//             //     }
//             // }
//             // else
//             // {
//             //     // Products were not found
//             //     Console.WriteLine("Cannot find product: {0}", e.ToString());
//             // }
//             
//         }
//         
//         
//     }
// }