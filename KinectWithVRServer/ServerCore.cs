using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Vrpn;

namespace KinectWithVRServer
{
    class ServerCore
    {
        /// <summary>
        /// Flag used to indicate whether we've entered the main loop, and modified to stop the loop.
        /// Probably a race condition - you probably want to either use like a semaphore, or at least atomic data structures.
        /// </summary>
        bool running = false;
        bool serverStopped = true;
        public bool isRunning
        {
            get { return running; }
        }
        Vrpn.Connection vrpnConnection;
        internal MasterSettings serverMasterOptions;
        internal List<Vrpn.ButtonServer> buttonServers;
        internal List<Vrpn.AnalogServer> analogServers;
        internal List<Vrpn.TextSender> textServers;
        internal List<Vrpn.TrackerServer> trackerServers;
        bool verbose = false;
        bool GUI = false;
        MainWindow parent;
        internal List<KinectCore> kinects = new List<KinectCore>();
        VoiceRecogCore voiceRecog;

        public ServerCore(bool isVerbose, MasterSettings serverOptions, MainWindow guiParent = null)
        {                
            parent = guiParent;
            verbose = isVerbose;
            serverMasterOptions = serverOptions;

            if (guiParent != null)
            {
                GUI = true;
            }

            //TODO: Make this actually do something useful on the GUI
            //try
            //{
            //    kinectCore = new KinectCore(this, parent);
            //}
            //catch (IndexOutOfRangeException except)
            //{
            //    Debug.WriteLine("No Kinect attached: " + except.Message);
            //}
        }

        //public void launchServer(MasterSettings serverSettings)
        public void launchServer()
        {
            //serverMasterOptions = serverSettings;
            serverMasterOptions.parseSettings();

            runServerCoreDelegate serverDelegate = runServerCore;
            serverDelegate.BeginInvoke(null, null);

            //Start voice recognition, if necessary
            if (serverMasterOptions.voiceCommands.Count > 0)
            {
                voiceRecog = new VoiceRecogCore(this, verbose, parent);
                launchVoiceRecognizerDelegate voiceDelegate = voiceRecog.launchVoiceRecognizer;
                //Dispatcher newDispatch = new Dispatcher();

                voiceDelegate.BeginInvoke(new AsyncCallback(voiceStartedCallback), null);
                //voiceRecog.launchVoiceRecognizer();
            }
            else
            {
                //Because the voice callback will not be called, we need to call this stuff here
                if (GUI)
                {
                    parent.startServerButton.Content = "Stop";
                    parent.startServerButton.IsEnabled = true;
                    parent.ServerStatusItem.Content = "Running";
                    parent.ServerStatusTextBlock.Text = "Running";
                }
            }
        }
        /// <summary>
        /// TODO why is GUI code in the server core?
        /// 
        /// </summary>
        /// <param name="ar"></param>
        private void voiceStartedCallback(IAsyncResult ar)
        {
            HelperMethods.WriteToLog("Voice started!", parent);

            if (GUI)
            {
                parent.Dispatcher.BeginInvoke((Action)(() =>
                {
                    parent.startServerButton.Content = "Stop";
                    parent.startServerButton.IsEnabled = true;
                    parent.ServerStatusItem.Content = "Running";
                    parent.ServerStatusTextBlock.Text = "Running";
                }), null
                );
            }
        }

        /// <summary>
        /// TODO who calls this?
        /// </summary>
        public void stopServer()
        {
            running = false;

            if (voiceRecog != null)
            {
                voiceRecog.stopVoiceRecognizer();
            }

            int count = 0;
            while (count < 30)
            {
                if (serverStopped)
                {
                    break;
                }
                count++;
                Thread.Sleep(100);
            }
            if (count >= 30 && !serverStopped)
            {
                throw new Exception("VRPN server shutdown failed!");
            }
        }
        /// <summary>
        /// TODO what is this?
        /// </summary>
        //public void shutdownServer()
        //{
        //    if (kinectCore != null)
        //    {
        //        kinectCore.ShutdownSensor();
        //    }
        //}

        private static void updateList<T>(ref List<T> serverlist) where T : Vrpn.IVrpnObject
        {
            for (int i = 0; i < serverlist.Count; i++)
            {
                lock (serverlist[i])
                {
                    serverlist[i].Update();
                }
            }
        }

        private static void disposeList<T>(ref List<T> list) where T : IDisposable
        {
            for (int i = 0; i < list.Count; i++)
            {
                lock (list[i])
                {
                    list[i].Dispose();
                }
            }
        }

        private void runServerCore()
        {
            serverStopped = false;

            //Create the connection for all the servers
            vrpnConnection = Connection.CreateServerConnection();

            /// TODO Can these be parameterized, since they're essentially the same thing? If this were C++ I'd use templates.

            //Set up all the analog servers
            analogServers = new List<AnalogServer>();
            for (int i = 0; i < serverMasterOptions.analogServers.Count; i++)
            {
                lock (serverMasterOptions.analogServers[i])
                {
                    analogServers.Add(new AnalogServer(serverMasterOptions.analogServers[i].serverName, vrpnConnection, serverMasterOptions.analogServers[i].channelCount));
                    analogServers[i].MuteWarnings = !verbose;
                }
            }

            //Set up all the button servers
            buttonServers = new List<ButtonServer>();
            for (int i = 0; i < serverMasterOptions.buttonServers.Count; i++)
            {
                lock (serverMasterOptions.buttonServers[i])
                {
                    buttonServers.Add(new ButtonServer(serverMasterOptions.buttonServers[i].serverName, vrpnConnection, serverMasterOptions.buttonServers[i].buttonCount));
                    buttonServers[i].MuteWarnings = !verbose;
                }
            }

            //Set up all the text servers
            textServers = new List<TextSender>();
            for (int i = 0; i < serverMasterOptions.textServers.Count; i++)
            {
                lock (serverMasterOptions.textServers[i])
                {
                    textServers.Add(new TextSender(serverMasterOptions.textServers[i].serverName, vrpnConnection));
                    textServers[i].MuteWarnings = !verbose;
                }
            }

            //Set up all the tracker servers
            trackerServers = new List<TrackerServer>();
            for (int i = 0; i < serverMasterOptions.trackerServers.Count; i++)
            {
                lock (serverMasterOptions.trackerServers[i])
                {
                    trackerServers.Add(new TrackerServer(serverMasterOptions.trackerServers[i].serverName, vrpnConnection, serverMasterOptions.trackerServers[i].sensorCount));
                    trackerServers[i].MuteWarnings = !verbose;
                }
            }

            //The server isn't really running until everything is setup here.
            running = true;

            //Run the server
            while (running)
            {
                //Update the analog servers
                updateList(ref analogServers);
                updateList(ref buttonServers);
                updateList(ref textServers);
                updateList(ref trackerServers);
                lock (vrpnConnection)
                {
                    vrpnConnection.Update();
                }
                Thread.Yield(); // Be polite, but don't add unnecessary latency.
            }

            //Cleanup everything
            //Dispose the analog servers
            disposeList(ref analogServers);
            disposeList(ref buttonServers);
            disposeList(ref textServers);
            disposeList(ref trackerServers);
            lock (vrpnConnection)
            {
                vrpnConnection.Dispose();
            }

            serverStopped = true;
        }

        private delegate void runServerCoreDelegate();
        private delegate void launchVoiceRecognizerDelegate();
    }
}