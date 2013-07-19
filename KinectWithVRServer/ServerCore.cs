using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vrpn;
using System.Threading;

namespace KinectWithVRServer
{
    class ServerCore
    {
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
        public KinectCore kinect;
        VoiceRecogCore voiceRecog;
        //GestureRecogCore gestureRecog;
        public static string printerGrunt;

        public ServerCore(bool isVerbose, KinectCore kinectCore, MainWindow guiParent = null)
        {                
            parent = guiParent;
            verbose = isVerbose;
            kinect = kinectCore;
            if (guiParent != null)
            {
                GUI = true;
            }
            if (kinect == null && GUI)
            {
                kinect = new KinectCore(this, parent);
                parent.kinect = kinect;
            }
            else if (kinect == null && !GUI)
            {
                kinect = new KinectCore(this);
            }
        }

        public void launchServer(MasterSettings serverSettings)
        {
            serverMasterOptions = serverSettings;
            serverMasterOptions.parseSettings();

            runServerCoreDelegate serverDelegate = runServerCore;
            serverDelegate.BeginInvoke(null, null);

            //Start voice recognition
            voiceRecog = new VoiceRecogCore(this, verbose, parent);
            voiceRecog.launchVoiceRecognizer();

            //gestureRecog = new GestureRecogCore(this, verbose, parent);
            //gestureRecog.launchGestureCore();
        }

        public void shutdownServer()
        {
            running = false;

            voiceRecog.stopVoiceRecognizer();

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

        private void runServerCore()
        {
            serverStopped = false;

            //Create the connection for all the servers
            vrpnConnection = Connection.CreateServerConnection();
            
            //Set up all the analog servers
            analogServers = new List<AnalogServer>();
            for (int i = 0; i < serverMasterOptions.analogServers.Count; i++)
            {
                analogServers.Add(new AnalogServer(serverMasterOptions.analogServers[i].serverName, vrpnConnection, serverMasterOptions.analogServers[i].channelCount));
                if (!verbose)
                {
                    analogServers[i].MuteWarnings = true;
                }
                else
                {
                    analogServers[i].MuteWarnings = false;
                }
            }

            //Set up all the button servers
            buttonServers = new List<ButtonServer>();
            for (int i = 0; i < serverMasterOptions.buttonServers.Count; i++)
            {
                buttonServers.Add(new ButtonServer(serverMasterOptions.buttonServers[i].serverName, vrpnConnection, serverMasterOptions.buttonServers[i].buttonCount));
                if (!verbose)
                {
                    buttonServers[i].MuteWarnings = true;
                }
                else
                {
                    buttonServers[i].MuteWarnings = false;
                }
            }

            //Set up all the text servers
            textServers = new List<TextSender>();
            for (int i = 0; i < serverMasterOptions.textServers.Count; i++)
            {
                textServers.Add(new TextSender(serverMasterOptions.textServers[i].serverName, vrpnConnection));
                if (!verbose)
                {
                    textServers[i].MuteWarnings = true;
                }
                else
                {
                    textServers[i].MuteWarnings = false;
                }
            }

            //Set up all the tracker servers
            trackerServers = new List<TrackerServer>();
            for (int i = 0; i < serverMasterOptions.trackerServers.Count; i++)
            {
                trackerServers.Add(new TrackerServer(serverMasterOptions.trackerServers[i].serverName, vrpnConnection, serverMasterOptions.trackerServers[i].sensorCount));
                if (!verbose)
                {
                    trackerServers[i].MuteWarnings = true;
                }
                else
                {
                    trackerServers[i].MuteWarnings = false;
                }
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

        //public void ConsolePrint(string outputMessage)
        //{

        //}
    }
}