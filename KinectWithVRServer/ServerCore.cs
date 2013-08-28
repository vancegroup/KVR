using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Microsoft.Kinect;
using Vrpn;

namespace KinectWithVRServer
{
    class ServerCore
    {
        /// <summary>
        /// Flag used to indicate whether we've entered the main loop, and modified to stop the loop.
        /// Probably a race condition - you probably want to either use like a semaphore, or at least atomic data structures.
        /// </summary>
        private volatile bool forceStop = false;
        private volatile ServerRunState serverState = ServerRunState.Stopped;  //Only modify this inside a lock on the runningLock object!
        public ServerRunState ServerState
        {
            get { return serverState; }
        }
        public bool isRunning
        {
            get { return (serverState == ServerRunState.Running); }
        }
        Vrpn.Connection vrpnConnection;
        internal MasterSettings serverMasterOptions;
        internal List<Vrpn.ButtonServer> buttonServers;
        internal List<Vrpn.AnalogServer> analogServers;
        internal List<Vrpn.TextSender> textServers;
        internal List<Vrpn.TrackerServer> trackerServers;
        internal List<List<Skeleton>> perKinectSkeletons;
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
            //These don't need a lock to be thread safe since they are volatile
            forceStop = false;
            serverState = ServerRunState.Starting;

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
        /// Stops the server from running, but does not cleanup all the settings in case you want to turn the server back on
        /// </summary>
        public void stopServer()
        {
            forceStop = true ;

            if (voiceRecog != null)
            {
                voiceRecog.stopVoiceRecognizer();
            }

            int count = 0;
            while (count < 30)
            {
                if (serverState == ServerRunState.Stopped)
                {
                    break;
                }
                count++;
                Thread.Sleep(100);
            }
            if (count >= 30 && serverState != ServerRunState.Stopped)
            {
                throw new Exception("VRPN server shutdown failed!");
            }
        }

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
            //Create the connection for all the servers
            vrpnConnection = Connection.CreateServerConnection();

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
            serverState = ServerRunState.Running;

            //Run the server
            while (!forceStop)
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
            serverState = ServerRunState.Stopping;
            disposeList(ref analogServers);
            disposeList(ref buttonServers);
            disposeList(ref textServers);
            disposeList(ref trackerServers);
            lock (vrpnConnection)
            {
                vrpnConnection.Dispose();
            }

            serverState = ServerRunState.Stopped;
        }

        //Merges all the skeletons together and transmits the new master skeletons VIA vrpn
        internal void mergeAndTransmitSkeletons()
        {
            List<Skeleton> masterSkeletons = new List<Skeleton>();

            //Merge all skeletons here

            //Transmit all skeletons here
        }

        private delegate void runServerCoreDelegate();
        private delegate void launchVoiceRecognizerDelegate();
    }

    enum ServerRunState {Starting, Running, Stopping, Stopped}
}