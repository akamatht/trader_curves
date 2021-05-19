using System;
using System.Collections.Generic;
using System.Threading;
using tt_net_sdk;

namespace tt_v1
{
    public class TTDriver
    {
        private TTNetApiFunctions _ttNetApiFunctions;
        private TTAPIOptions _apiConfig;
        private WorkerDispatcher _dispatcher;

        public TTDriver(Action<string> onInit, WorkerDispatcher dispatcher)
        {
            this._dispatcher = dispatcher;
            _ttNetApiFunctions = new TTNetApiFunctions(onInit, this._dispatcher);
            // Add your app secret Key here. It looks like: 00000000-0000-0000-0000-000000000000:00000000-0000-0000-0000-000000000000
            string appSecretKey = "7f6d1dac-4e09-00a4-1a60-b227db734ba9:f29145c6-d8a1-5b88-1a6f-5770ac26211a";
                
            //Set the environment the app needs to run in here
            ServiceEnvironment environment = ServiceEnvironment.ProdLive;

            this._apiConfig = new TTAPIOptions(
                environment,
                appSecretKey,
                5000);

            

        }

        public void start()
        {
            Thread workerThread = new Thread(() => _ttNetApiFunctions.Start(this._apiConfig));
            workerThread.Name = "TT NET SDK Thread";
            workerThread.Start();
        }
        
        public IReadOnlyCollection<Instrument> Query(MarketId mktId, ProductType pdtType, string group) {
            var qry = new InstrumentCatalog(mktId, pdtType, group, this._dispatcher);
            var evt = qry.Get();
            if (evt != ProductDataEvent.Found)
                throw new Exception("ERROR");
            return qry.InstrumentList;
        }
    }
}