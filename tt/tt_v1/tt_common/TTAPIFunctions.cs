using System;
using System.Collections.Generic;
using tt_net_sdk;

namespace tt_v1
{
    
    public class TTNetApiFunctions
    {
        // Declare the API objects
        private TTAPI m_api = null;
        private WorkerDispatcher m_disp = null;
        private Action<string> onInit = null;

        public TTNetApiFunctions(Action<string> onInitialised, WorkerDispatcher dispatcher)
        {
            this.onInit = onInitialised;
            m_disp = dispatcher;
        }
        
        public void Start(TTAPIOptions apiConfig)
        {
            m_disp.DispatchAction(() =>
            {
                Init(apiConfig);
            });

            m_disp.Run();
        }

        public void Init(TTAPIOptions apiConfig)
        {
            ApiInitializeHandler apiInitializeHandler = new ApiInitializeHandler(ttNetApiInitHandler);
            TTAPI.CreateTTAPI(Dispatcher.Current, apiConfig, apiInitializeHandler);
        }

        public void ttNetApiInitHandler(TTAPI api, ApiCreationException ex)
        {
            if (ex == null)
            {
                Console.WriteLine("TT.NET SDK Initialization Complete");

                // Authenticate your credentials
                m_api = api;
                m_api.TTAPIStatusUpdate += new EventHandler<TTAPIStatusUpdateEventArgs>(m_api_TTAPIStatusUpdate);
                m_api.Start();
            }
            else if (ex.IsRecoverable)
            {
                // Initialization failed but retry is in progress...
            }
            else
            {
                Console.WriteLine("TT.NET SDK Initialization Failed: {0}", ex.Message);
                Dispose();
            }
        }

        public void m_api_TTAPIStatusUpdate(object sender, TTAPIStatusUpdateEventArgs e)
        {
            if (e.IsReady)
            {
                // connection to TT is established
                Console.WriteLine("TT.NET SDK Authenticated");
                this.onInit("Ready");

            }
            else
            {
                Console.WriteLine("TT.NET SDK Status: {0}", e);
            }
        }

        public void Dispose()
        {
            TTAPI.ShutdownCompleted += TTAPI_ShutdownCompleted;
            TTAPI.Shutdown();
        }

        public void TTAPI_ShutdownCompleted(object sender, EventArgs e)
        {
            Console.WriteLine("TTAPI Shutdown completed");
        }
        

    }}