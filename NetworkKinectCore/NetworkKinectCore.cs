using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Threading;
using KinectBase;
using Vrpn;

namespace NetworkKinectCore
{
    public class NetworkKinectCore : KinectBase.IKinectCore
    {
        //Private variables used to manage the network Kinect internally
        private string nkName; //This will be used internally to store a unique name to identify the networked kinect
        internal MasterSettings masterSettings;
        internal NetworkKinectSettings masterKinectSettings;
        private Matrix3D skeletonTransformation = Matrix3D.Identity;
        private Quaternion skeletonRotQuaternion = Quaternion.Identity;
        private bool isGUI = false;
        private bool isRunning = false;
        private bool forceStop = false;
        private KinectSkeleton lastSkeleton = new KinectSkeleton();
        private System.Timers.Timer updateTimer;
        private string rhName = null;
        private string lhName = null;

        //Public properties required by the IKinectCore interface
        public int kinectID { get; set; } //This is the index of the Kinect options in the Kinect settings list
        public string uniqueKinectID
        {
            get
            {
                return nkName;
            }
        }
        public KinectVersion version
        {
            get { return KinectBase.KinectVersion.NetworkKinect; }
        }
        public bool ColorStreamEnabled
        {
            get { return false; }
        }
        public bool DepthStreamEnabled
        {
            get { return false; }
        }

        //Public properties specific to the network Kinect
        public bool isKinectRunning
        {
            get { return isRunning; }
        }

        //Event declarations (required by the IKinectCore)
        public event KinectBase.SkeletonEventHandler SkeletonChanged;
        public event KinectBase.DepthFrameEventHandler DepthFrameReceived; //This event will never be triggered
        public event KinectBase.ColorFrameEventHandler ColorFrameReceived; //This event will never be triggered
        public event KinectBase.AudioPositionEventHandler AudioPositionChanged; //This event will never be triggered
        public event KinectBase.AccelerationEventHandler AccelerationChanged; //This event will never be triggered
        public event KinectBase.LogMessageEventHandler LogMessageGenerated;

        public NetworkKinectCore(ref MasterSettings settings, bool isGUILaunched, int kinectNumber, string name)
        {
            nkName = name;
            masterSettings = settings;
            dynamic tempSettings = masterSettings.kinectOptionsList[(int)kinectNumber];  //Because of the wrapper, we have to go through a dynamic variable
            masterKinectSettings = (NetworkKinectSettings)tempSettings;
            kinectID = kinectNumber;

            isGUI = isGUILaunched;
            //NOTE: unlike the physical Kinect sensors, network Kinect sensors are not launched on the creation of the core
        }

        //Methods required by the IKinectCore interface
        public void ShutdownSensor()
        {
            forceStop = true;

            //Stop the update timer
            if (updateTimer != null)
            {
                updateTimer.Stop();
                updateTimer.Elapsed -= updateTime_Elapsed;
                updateTimer.Dispose();
                updateTimer = null;
            }

            int count = 0;
            int maxCount = 50;
            while (count < maxCount) //Wait for the core to stop
            {
                if (!isRunning)
                {
                    break;
                }
                Thread.Sleep(10);
            }
            if (count >= maxCount && isRunning)
            {
                throw new Exception("Could not stop feedback core!");
            }

            //TODO: Somehow, the avaliable Kinects datagrid needs to be forced to update here so it knows the server is stopped
        }
        public KinectSkeleton TransformSkeleton(KinectSkeleton skeleton)
        {
            KinectSkeleton transformedSkeleton = new KinectSkeleton();
            transformedSkeleton.leftHandClosed = skeleton.leftHandClosed;
            transformedSkeleton.rightHandClosed = skeleton.rightHandClosed;
            transformedSkeleton.TrackingId = skeleton.TrackingId;
            transformedSkeleton.SkeletonTrackingState = skeleton.SkeletonTrackingState;
            //transformedSkeleton.utcSampleTime = skeleton.utcSampleTime;
            transformedSkeleton.sourceKinectID = skeleton.sourceKinectID;
            transformedSkeleton.Position = skeletonTransformation.Transform(skeleton.Position);

            //Transform the joints
            for (int i = 0; i < skeleton.skeleton.Count; i++)
            {
                transformedSkeleton.skeleton[i] = TransformJoint(skeleton.skeleton[i]);
            }

            return transformedSkeleton;
        }
        public Joint TransformJoint(Joint joint)
        {
            Joint transformedJoint = new Joint();
            transformedJoint.Confidence = joint.Confidence;
            transformedJoint.JointType = joint.JointType;
            transformedJoint.TrackingState = joint.TrackingState;
            transformedJoint.Orientation = skeletonRotQuaternion * joint.Orientation;
            transformedJoint.Position = skeletonTransformation.Transform(joint.Position);
            transformedJoint.utcTime = joint.utcTime;

            return transformedJoint;
        }
        public System.Windows.Point MapJointToColor(Joint joint, bool undoTransform)
        {
            return new System.Windows.Point(-1, -1); //Make sure the position is always off the image so it doesn't render, since we can't do a preview of the network kinect color image anyway
        }
        public System.Windows.Point MapJointToDepth(Joint joint, bool undoTransform)
        {
            return new System.Windows.Point(-1, -1); //Make sure the position is always off the image so it doesn't render, since we can't do a preview of the network kinect depth image anyway
        }

        //Methods specific to the network kinect
        public bool StartNetworkKinect()
        {
            bool success = true;

            //Parse all the settings
            string name = "";
            if (masterKinectSettings.serverName.Length >= 3)  //By my math, 3 characters is the smallest possible valid server name
            {
                name = masterKinectSettings.serverName;
            }
            else
            {
                LogMessage(string.Format("The network Kinect server name of Kinect {0} is invalid.", kinectID), false);
                success = false;
            }

            //Parse the hand settings. Hand servers aren't required, so if they don't exist, we will pass a null name and the button server just won't be started for it.
            if (masterKinectSettings.rhServerName != null && masterKinectSettings.rhServerName.Length >= 3 && masterKinectSettings.rhChannel >= 0)
            {
                rhName = masterKinectSettings.rhServerName;
            }
            if (masterKinectSettings.lhServerName != null && masterKinectSettings.lhServerName.Length >= 3 && masterKinectSettings.lhChannel >= 0)
            {
                lhName = masterKinectSettings.lhServerName;
            }

            //Launch the server
            if (success)
            {
                try
                {
                    //Launch the VRPN client on another thread
                    forceStop = false;
                    RunNetworkKinectDelegate networkKinectDelegate = RunNetworkKinect;
                    networkKinectDelegate.BeginInvoke(name, rhName, lhName, null, null);

                    //Start the update time
                    updateTimer = new System.Timers.Timer();
                    updateTimer.AutoReset = true;
                    updateTimer.Interval = 10;
                    updateTimer.Elapsed += updateTime_Elapsed;
                    updateTimer.Start();
                }
                catch (Exception error)
                {
                    LogMessage(string.Format("An error occured trying to start network Kinect with ID {0}.  The error message is: {1}", kinectID, error.Message), false);
                    success = false;
                    isRunning = false;
                }
            }

            return success;
        }
        private void updateTime_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SkeletonEventArgs args = new SkeletonEventArgs();
            args.skeletons = new KinectSkeleton[1];
            args.skeletons[0] = new KinectSkeleton();
            args.kinectID = kinectID;
            bool hasData = false;
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan oneSecond = new TimeSpan(0, 0, 1);

            //Copy the data from the last skeleton
            for (int i = 0; i < lastSkeleton.skeleton.Count; i++)
            {
                if (lastSkeleton.skeleton[i].TrackingState == TrackingState.Tracked)
                {
                    if (lastSkeleton.skeleton[i].utcTime - currentTime > oneSecond)
                    {
                        hasData = true;

                        args.skeletons[0].skeleton[i] = lastSkeleton.skeleton[i]; //Joint is a struct, which is a value type, so we don't need a copy here

                        if (lastSkeleton.skeleton[i].JointType == JointType.HandLeft)
                        {
                            args.skeletons[0].rightHandClosed = lastSkeleton.rightHandClosed;
                        }
                        if (lastSkeleton.skeleton[i].JointType == JointType.HandLeft)
                        {
                            args.skeletons[0].leftHandClosed = lastSkeleton.leftHandClosed;
                        }
                    }
                }
            }

            if (hasData)
            {
                args.skeletons[0].sourceKinectID = kinectID;
                args.skeletons[0].TrackingId = 0;
                args.skeletons[0].SkeletonTrackingState = TrackingState.Tracked;
                args.skeletons[0].Position = args.skeletons[0].skeleton[JointType.SpineBase].Position;
            }

            if (SkeletonChanged != null && hasData)
            {
                SkeletonChanged(this, args);
            }
        }
        private void RunNetworkKinect(string serverName, string rhServerName, string lhServerName)
        {
            using (TrackerRemote client = new TrackerRemote(serverName))
            {
                client.PositionChanged += client_PositionChanged;

                //Start the rh server, if needed
                if (rhServerName != null && lhServerName != null)
                {
                    if (rhServerName != null || lhServerName != null)
                    {
                        if (rhServerName != null)
                        {
                            //Only the right hand server should be used
                            using (ButtonRemote rhClient = new ButtonRemote(rhServerName))
                            {
                                rhClient.ButtonChanged += rhClient_ButtonChanged;
                                isRunning = true;

                                while (!forceStop)
                                {
                                    client.Update();
                                    rhClient.Update();
                                    Thread.Yield();
                                }

                                rhClient.ButtonChanged -= rhClient_ButtonChanged;
                            }
                        }
                        else
                        {
                            //Only the left hand server should be used
                            using (ButtonRemote lhClient = new ButtonRemote(lhServerName))
                            {
                                lhClient.ButtonChanged += lhClient_ButtonChanged;
                                isRunning = true;

                                while (!forceStop)
                                {
                                    client.Update();
                                    lhClient.Update();
                                    Thread.Yield();
                                }

                                lhClient.ButtonChanged -= lhClient_ButtonChanged;
                            }

                        }
                    }
                    else
                    {
                        if (rhServerName == lhServerName)
                        {
                            //The left and right hand should be used, but they are on the same server
                            using (ButtonRemote handClient = new ButtonRemote(rhServerName))
                            {
                                handClient.ButtonChanged += rhClient_ButtonChanged;
                                isRunning = true;

                                while (!forceStop)
                                {
                                    client.Update();
                                    handClient.Update();
                                    Thread.Yield();
                                }

                                handClient.ButtonChanged -= rhClient_ButtonChanged;
                            }
                        }
                        else
                        {
                            //Both the left and right hand should be used, and they are on different servers
                            using (ButtonRemote rhClient = new ButtonRemote(rhServerName))
                            {
                                using (ButtonRemote lhClient = new ButtonRemote(lhServerName))
                                {
                                    rhClient.ButtonChanged += rhClient_ButtonChanged;
                                    lhClient.ButtonChanged += lhClient_ButtonChanged;
                                    isRunning = true;

                                    while (!forceStop)
                                    {
                                        client.Update();
                                        rhClient.Update();
                                        lhClient.Update();
                                        Thread.Yield();
                                    }

                                    rhClient.ButtonChanged -= rhClient_ButtonChanged;
                                    lhClient.ButtonChanged -= lhClient_ButtonChanged;
                                }
                            }
                        }
                    }
                }
                else
                {
                    //No hand servers case
                    isRunning = true;

                    while (!forceStop)
                    {
                        client.Update();
                        Thread.Yield();
                    }
                }

                client.PositionChanged -= client_PositionChanged;
            }

            isRunning = false;
        }
        private void client_PositionChanged(object sender, TrackerChangeEventArgs e)
        {
            //If we try and pass a new skeleton for every tracker update we recieve, we'll spend way too much time and memory creating new skeleton objects
            //Therefore, this will only update the last skeleton object and the last skeleton data will be passed at regular time intervals
            Joint newJoint = new Joint();
            newJoint.utcTime = DateTime.UtcNow;  //We can't be sure if a specific VRPN server is using utc or local time, so we will grab our own time instead of using VRPNs
            JointMapping map = GetJointMapFromChannel(e.Sensor);
            newJoint.JointType = map.joint;
            newJoint.Position = new Point3D(e.Position.X, e.Position.Y, e.Position.Z);
            newJoint.Orientation = e.Orientation;
            newJoint.TrackingState = TrackingState.Tracked;

            lastSkeleton.skeleton[map.joint] = newJoint;
        }
        private void rhClient_ButtonChanged(object sender, ButtonChangeEventArgs e)
        {
            //If both hands are on this server, check the left hand channel
            if (lhName != null && rhName == lhName)
            {
                if (e.Button == masterKinectSettings.lhChannel)
                {
                    lastSkeleton.leftHandClosed = e.IsPressed;
                }
            }
            //Always check the right hand channel
            if (e.Button == masterKinectSettings.rhChannel)
            {
                lastSkeleton.rightHandClosed = e.IsPressed;
            }
        }
        private void lhClient_ButtonChanged(object sender, ButtonChangeEventArgs e)
        {
            if (e.Button == masterKinectSettings.lhChannel)
            {
                lastSkeleton.leftHandClosed = e.IsPressed;
            }
        }

        //Helper methods
        private void LogMessage(string message, bool forVerboseOnly)
        {
            LogMessageEventArgs e = new LogMessageEventArgs();
            e.errorMessage = message;
            e.verboseMessage = forVerboseOnly;
            e.kinectID = kinectID;

            if (LogMessageGenerated != null)
            {
                LogMessageGenerated(this, e);
            }
        }
        private JointMapping GetJointMapFromChannel(int channel)
        {
            JointMapping map = null;

            for (int i = 0; i < masterKinectSettings.jointMappings.Count; i++)
            {
                if (masterKinectSettings.jointMappings[i].channel == channel)
                {
                    map = masterKinectSettings.jointMappings[i];
                    break;
                }
            }

            return map;
        }

        private delegate void RunNetworkKinectDelegate(string serverName, string rhServerName, string lhServerName);
    }
}
