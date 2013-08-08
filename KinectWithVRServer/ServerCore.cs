using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vrpn;
using System.Threading;
using System.Windows.Threading;

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
        public KinectCore kinectCore;
        VoiceRecogCore voiceRecog;
        //GestureRecogCore gestureRecog;
        //public static string printerGrunt;

        //public ServerCore(bool isVerbose, KinectCore kinectCore, MainWindow guiParent = null)
        public ServerCore(bool isVerbose, MasterSettings serverOptions, MainWindow guiParent = null)
        {                
            parent = guiParent;
            verbose = isVerbose;
            serverMasterOptions = serverOptions;

            if (guiParent != null)
            {
                GUI = true;
            }

            kinectCore = new KinectCore(this, parent);
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

        public void shutdownServer()
        {
            if (kinectCore != null)
            {
                kinectCore.ShutdownSensor();
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
                analogServers.Add(new AnalogServer(serverMasterOptions.analogServers[i].serverName, vrpnConnection, serverMasterOptions.analogServers[i].channelCount));
                analogServers[i].MuteWarnings = !verbose;
            }

            //Set up all the button servers
            buttonServers = new List<ButtonServer>();
            for (int i = 0; i < serverMasterOptions.buttonServers.Count; i++)
            {
                buttonServers.Add(new ButtonServer(serverMasterOptions.buttonServers[i].serverName, vrpnConnection, serverMasterOptions.buttonServers[i].buttonCount));
                buttonServers[i].MuteWarnings = !verbose;
            }

            //Set up all the text servers
            textServers = new List<TextSender>();
            for (int i = 0; i < serverMasterOptions.textServers.Count; i++)
            {
                textServers.Add(new TextSender(serverMasterOptions.textServers[i].serverName, vrpnConnection));
                textServers[i].MuteWarnings = !verbose;
            }

            //Set up all the tracker servers
            trackerServers = new List<TrackerServer>();
            for (int i = 0; i < serverMasterOptions.trackerServers.Count; i++)
            {
                trackerServers.Add(new TrackerServer(serverMasterOptions.trackerServers[i].serverName, vrpnConnection, serverMasterOptions.trackerServers[i].sensorCount));
                trackerServers[i].MuteWarnings = !verbose;
            }

            //The server isn't really running until everything is setup here.
            running = true;

            //Run the server
            while (running)
            {
                //Update the analog servers
                for (int i = 0; i < analogServers.Count; i++)
                {
                    analogServers[i].Update();
                }

                //Update the button servers
                for (int i = 0; i < buttonServers.Count; i++)
                {
                    buttonServers[i].Update();
                }

                //Update the text servers
                for (int i = 0; i < textServers.Count; i++)
                {
                    textServers[i].Update();
                }

                //Update the tracker servers
                for (int i = 0; i < trackerServers.Count; i++)
                {
                    trackerServers[i].Update();
                }
                vrpnConnection.Update();
                Thread.Sleep(5);  //Unfortunately, this will add some latency to the system, but this loop burns too much processor time if it isn't there
            }

            //Cleanup everything
            //Dispose the analog servers
            for (int i = 0; i < analogServers.Count; i++)
            {
                analogServers[i].Dispose();
            }

            //Dispose the button servers
            for (int i = 0; i < buttonServers.Count; i++)
            {
                buttonServers[i].Dispose();
            }

            //Dispose the text servers
            for (int i = 0; i < textServers.Count; i++)
            {
                textServers[i].Dispose();
            }

            //Dispose the tracker servers
            for (int i = 0; i < trackerServers.Count; i++)
            {
                trackerServers[i].Dispose();
            }
            vrpnConnection.Dispose();

            serverStopped = true;
        }

        private delegate void runServerCoreDelegate();
        private delegate void launchVoiceRecognizerDelegate();
    }
}