using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Microsoft.Kinect;
using System.Windows.Media.Media3D;
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
        //internal List<KinectSkeletonsData> perKinectSkeletons = new List<KinectSkeletonsData>(); //TODO: use this to merge the skeletons
        bool verbose = false;
        bool GUI = false;
        MainWindow parent;
        internal List<KinectCore> kinects = new List<KinectCore>();
        VoiceRecogCore voiceRecog;
        FeedbackCore feedbackCore;
        internal Point3D? feedbackPosition = null;

        public ServerCore(bool isVerbose, MasterSettings serverOptions, MainWindow guiParent = null)
        {                
            parent = guiParent;
            verbose = isVerbose;
            serverMasterOptions = serverOptions;

            if (guiParent != null)
            {
                GUI = true;
            }
        }

        public void launchServer()
        {
            //These don't need a lock to be thread safe since they are volatile
            forceStop = false;
            serverState = ServerRunState.Starting;

            string errorMessage = "";
            if (serverMasterOptions.parseSettings(out errorMessage))
            {
                //Start the Kinect audio streams and create the per Kinect skeleton lists
                for (int i = 0; i < kinects.Count; i++)
                {
                    kinects[i].StartKinectAudio(); //TODO: This will crash if the Kinects are in another thread (i.e. console mode)
                }

                //Start the feedback client if necessary
                if (serverMasterOptions.feedbackOptions.useFeedback)
                {
                    feedbackCore = new FeedbackCore(verbose, this, parent);
                    feedbackCore.StartFeedbackCore(serverMasterOptions.feedbackOptions.feedbackServerName, serverMasterOptions.feedbackOptions.feedbackSensorNumber);
                }

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
            else
            {
                HelperMethods.ShowErrorMessage("Error", "Settings parsing failed!  See the log for more details.", parent);
                HelperMethods.WriteToLog(errorMessage, parent);
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

            if (feedbackCore != null)
            {
                feedbackCore.StopFeedbackCore();
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
                    //Note: This uses maxChannelUsed NOT trueChannelCount in case non-consecutive channels are used
                    analogServers.Add(new AnalogServer(serverMasterOptions.analogServers[i].serverName, vrpnConnection, serverMasterOptions.analogServers[i].maxChannelUsed + 1));
                    analogServers[i].MuteWarnings = !verbose;
                }
            }

            //Set up all the button servers
            buttonServers = new List<ButtonServer>();
            for (int i = 0; i < serverMasterOptions.buttonServers.Count; i++)
            {
                lock (serverMasterOptions.buttonServers[i])
                {
                    //Note: This uses maxButtonUsed NOT trueButtonCount in case non-consecutive buttons are used
                    buttonServers.Add(new ButtonServer(serverMasterOptions.buttonServers[i].serverName, vrpnConnection, serverMasterOptions.buttonServers[i].maxButtonUsed + 1));
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

        //Redirect the skeleton data changed event to merge and transmit the skeleton data
        public void PerKinectSkeletons_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            mergeAndTransmitSkeletons();
        }
        //Merges all the skeletons together and transmits the new master skeletons VIA vrpn
        private void mergeAndTransmitSkeletons()
        {
            List<KinectSkeleton> masterSkeletons = new List<KinectSkeleton>();
            List<int[]> pointsAverages = new List<int[]>();
            List<List<bool>> rightHandStates = new List<List<bool>>();
            List<List<bool>> leftHandStates = new List<List<bool>>();

            #region Merge the skeleton data
            for (int i = 0; i < serverMasterOptions.kinectOptionsList.Count; i++)
            {
                if (serverMasterOptions.kinectOptionsList[i].trackSkeletons)
                {
                    List<KinectSkeleton> localCopy = new List<KinectSkeleton>(kinects[i].skeletonData.actualSkeletons);
                    for (int localIndex = localCopy.Count - 1; localIndex >= 0; localIndex--)
                    {
                        if (localCopy[localIndex].skeleton == null || localCopy[localIndex].skeleton.TrackingState == SkeletonTrackingState.NotTracked)
                        {
                            localCopy.RemoveAt(localIndex);
                        }
                        else
                        {
                            bool skeletonFound = false;
                            for (int masterIndex = 0; masterIndex < masterSkeletons.Count; masterIndex++)
                            {
                                double dist = InterPointDistance(localCopy[localIndex].skeleton.Position, masterSkeletons[masterIndex].skeleton.Position);
                                if (dist < 0.3)
                                {
                                    skeletonFound = true;
                                    masterSkeletons[masterIndex].skeleton.Position = IncAverage(masterSkeletons[masterIndex].skeleton.Position, localCopy[localIndex].skeleton.Position, pointsAverages[masterIndex][20]);
                                    pointsAverages[masterIndex][20]++;

                                    //Average the joints
                                    for (int jointIndex = 0; jointIndex < masterSkeletons[masterIndex].skeleton.Joints.Count; jointIndex++)
                                    {
                                        Joint tempJoint = new Joint();

                                        //If the new skeleton joint has tracking data, and the old one doesn't, use the new one and update the tracking state
                                        if (masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.NotTracked && localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].TrackingState != JointTrackingState.NotTracked)
                                        {
                                            tempJoint = localCopy[localIndex].skeleton.Joints[(JointType)jointIndex];
                                            masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex] = tempJoint;
                                            pointsAverages[masterIndex][jointIndex] = 0;
                                        }
                                        //If they are both inferred, just average them
                                        else if (masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Inferred && localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Inferred)
                                        {
                                            tempJoint = masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex];
                                            tempJoint.Position = IncAverage(masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].Position, localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].Position, pointsAverages[masterIndex][jointIndex]);
                                            pointsAverages[masterIndex][jointIndex]++;
                                            masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex] = tempJoint;
                                        }
                                        //If they are both tracked, just average them
                                        else if (masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Tracked && localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Tracked)
                                        {
                                            tempJoint = masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex];
                                            tempJoint.Position = IncAverage(masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].Position, localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].Position, pointsAverages[masterIndex][jointIndex]);
                                            pointsAverages[masterIndex][jointIndex]++;
                                            masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex] = tempJoint;
                                        }
                                        //If they new one is tracked, and the old one is inferred, use the new one and update the tracking state - COULD DO A WEIGHTED AVERAGE HERE!!
                                        else if (masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Inferred && localCopy[localIndex].skeleton.Joints[(JointType)jointIndex].TrackingState == JointTrackingState.Tracked)
                                        {
                                            tempJoint = localCopy[localIndex].skeleton.Joints[(JointType)jointIndex];
                                            pointsAverages[masterIndex][jointIndex] = 0;
                                            masterSkeletons[masterIndex].skeleton.Joints[(JointType)jointIndex] = tempJoint;
                                        }
                                        //Otherwise, we just ignore it

                                        //Merge the hands
                                        if ((JointType)jointIndex == JointType.HandRight)
                                        {
                                            if (localCopy[localIndex].skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                                            {
                                                rightHandStates[masterIndex].Add(localCopy[localIndex].rightHandClosed);
                                            }
                                        }
                                        else if ((JointType)jointIndex == JointType.HandLeft)
                                        {
                                            if (localCopy[localIndex].skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                                            {
                                                leftHandStates[masterIndex].Add(localCopy[localIndex].leftHandClosed);
                                            }
                                        }
                                    }
                                    localCopy.RemoveAt(localIndex);
                                    break;
                                }
                            }
                            if (!skeletonFound)
                            {
                                masterSkeletons.Add(localCopy[localIndex]);
                                pointsAverages.Add(new int[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });  //Use 21 N's for the averaging, even though there at 20 joints (the 21st is for the position)
                                if (localCopy[localIndex].skeleton.Joints[JointType.HandRight].TrackingState == JointTrackingState.Tracked)
                                {
                                    rightHandStates.Add(new List<bool>(new bool[] { localCopy[localIndex].rightHandClosed }));
                                }
                                else
                                {
                                    rightHandStates.Add(new List<bool>());
                                }
                                if (localCopy[localIndex].skeleton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
                                {
                                    leftHandStates.Add(new List<bool>(new bool[] { localCopy[localIndex].leftHandClosed }));
                                }
                                else
                                {
                                    leftHandStates.Add(new List<bool>());
                                }
                                localCopy.RemoveAt(localIndex);
                            }
                        }
                    }
                }

                kinects[i].skeletonData.needsUpdate = false;
            }
            #endregion

            //Sort all the skeletons before transmitting them
            masterSkeletons = SortSkeletons(masterSkeletons, serverMasterOptions.skeletonOptions.skeletonSortMode);

            //Transmit all skeletons here
            if (isRunning)
            {
                for (int skeletonIndex = 0; skeletonIndex < masterSkeletons.Count; skeletonIndex++)
                {
                    SendSkeletonVRPN(masterSkeletons[skeletonIndex].skeleton, skeletonIndex);
                    //Transmit the hand grips, if we have good data on them
                    if (leftHandStates.Count > 0)
                    {
                        int grippedLeft = leftHandStates[skeletonIndex].FindAll(x => x == true).Count;
                        bool leftState = grippedLeft >= ((leftHandStates.Count + 1) / 2); //If at least half of the available data says it is gripped, it is gripped
                       
                        for (int j = 0; j < buttonServers.Count; j++)
                        {
                            if (serverMasterOptions.buttonServers[j].serverName == serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].leftGripServerName)
                            {
                                lock (buttonServers[j])
                                {
                                    buttonServers[j].Buttons[serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].leftGripButtonNumber] = leftState;
                                }
                                break;
                            }
                        }
                    }
                    if (rightHandStates.Count > 0)
                    {
                        int grippedRight = rightHandStates[skeletonIndex].FindAll(x => x == true).Count;
                        bool rightState = grippedRight >= ((rightHandStates.Count + 1) / 2); //If at least half of the available data says it is gripped, it is gripped

                        for (int j = 0; j < buttonServers.Count; j++)
                        {
                            if (serverMasterOptions.buttonServers[j].serverName == serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].rightGripServerName)
                            {
                                lock (buttonServers[j])
                                {
                                    buttonServers[j].Buttons[serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].rightGripButtonNumber] = rightState;
                                }
                                break;
                            }
                        }
                    }
                }
            }

            //Update the audio beam angle on each Kinect, if necessary
            for (int i = 0; i < kinects.Count; i++)
            {
                if ((serverMasterOptions.kinectOptionsList[i].sendAudioAngle || serverMasterOptions.audioOptions.sourceID == i) && serverMasterOptions.kinectOptionsList[i].audioTrackMode == AudioTrackingMode.SkeletonX)
                {
                    int audioSkeleton = serverMasterOptions.kinectOptionsList[i].audioBeamTrackSkeletonNumber;
                    if (kinects[i].kinect.AudioSource != null && masterSkeletons.Count > audioSkeleton)
                    {
                        if (masterSkeletons[audioSkeleton].skeleton.TrackingState != SkeletonTrackingState.NotTracked)
                        {
                            double angle = Math.Atan((masterSkeletons[audioSkeleton].skeleton.Position.X - serverMasterOptions.kinectOptionsList[i].kinectPosition.X) / (masterSkeletons[audioSkeleton].skeleton.Position.Z - serverMasterOptions.kinectOptionsList[i].kinectPosition.Z)) * (180.0 / Math.PI);
                            kinects[i].kinect.AudioSource.ManualBeamAngle = angle;
                        }
                    }
                }
            }

            //Update stuff on the GUI
            if (GUI)
            {
                if (parent.Dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    //Update the number of skeletons being tracked on the GUI
                    parent.TrackedSkeletonsTextBlock.Text = masterSkeletons.Count.ToString();
                    parent.ColorImageCanvas.Children.Clear();
                    for (int skeletonIndex = 0; skeletonIndex < masterSkeletons.Count; skeletonIndex++)
                    {
                        parent.RenderSkeletonOnColor(masterSkeletons[skeletonIndex].skeleton, serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].renderColor);
                    }
                }
                else
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        //Update the number of skeletons being tracked on the GUI
                        parent.TrackedSkeletonsTextBlock.Text = masterSkeletons.Count.ToString();
                        parent.ColorImageCanvas.Children.Clear();
                        for (int skeletonIndex = 0; skeletonIndex < masterSkeletons.Count; skeletonIndex++)
                        {
                            parent.RenderSkeletonOnColor(masterSkeletons[skeletonIndex].skeleton, serverMasterOptions.skeletonOptions.individualSkeletons[skeletonIndex].renderColor);
                        }
                    }), null
                    );
                }
            }

            //Call the GUI skeleton renderer
        }
        private double InterPointDistance(SkeletonPoint x, SkeletonPoint y)
        {
            double distance = double.MaxValue;
            distance = Math.Sqrt(Math.Pow((double)y.X - (double)x.X, 2.0) + Math.Pow((double)y.Y - (double)x.Y, 2.0) + Math.Pow((double)y.Z - (double)x.Z, 2.0));
            return distance;
        }
        private SkeletonPoint IncAverage(SkeletonPoint x, SkeletonPoint y, int n)
        {
            SkeletonPoint avePoint = new SkeletonPoint();
            avePoint.X = (float)((double)x.X + ((double)y.X - (double)x.X) / (double)(n + 1));
            avePoint.Y = (float)((double)x.Y + ((double)y.Y - (double)x.Y) / (double)(n + 1));
            avePoint.Z = (float)((double)x.Z + ((double)y.Z - (double)x.Z) / (double)(n + 1));
            return avePoint;
        }
        private int GetSkeletonSensorNumber(JointType joint)
        {
            int sensorNumber = -1;

            //Translates the SDK joints to the FAAST joint numbers
            switch (joint)
            {
                case JointType.Head:
                    {
                        sensorNumber = 0;
                        break;
                    }
                case JointType.ShoulderCenter:
                    {
                        sensorNumber = 1;
                        break;
                    }
                case JointType.Spine:
                    {
                        sensorNumber = 2;
                        break;
                    }
                case JointType.HipCenter:
                    {
                        sensorNumber = 3;
                        break;
                    }
                //There is no 4, in order to match with FAAST
                case JointType.ShoulderLeft:
                    {
                        sensorNumber = 5;
                        break;
                    }
                case JointType.ElbowLeft:
                    {
                        sensorNumber = 6;
                        break;
                    }
                case JointType.WristLeft:
                    {
                        sensorNumber = 7;
                        break;
                    }
                case JointType.HandLeft:
                    {
                        sensorNumber = 8;
                        break;
                    }
                //There is no 9 or 10, in order to match with FAAST
                case JointType.ShoulderRight:
                    {
                        sensorNumber = 11;
                        break;
                    }
                case JointType.ElbowRight:
                    {
                        sensorNumber = 12;
                        break;
                    }
                case JointType.WristRight:
                    {
                        sensorNumber = 13;
                        break;
                    }
                case JointType.HandRight:
                    {
                        sensorNumber = 14;
                        break;
                    }
                //There is no 15, in order to match with FAAST
                case JointType.HipLeft:
                    {
                        sensorNumber = 16;
                        break;
                    }
                case JointType.KneeLeft:
                    {
                        sensorNumber = 17;
                        break;
                    }
                case JointType.AnkleLeft:
                    {
                        sensorNumber = 18;
                        break;
                    }
                case JointType.FootLeft:
                    {
                        sensorNumber = 19;
                        break;
                    }
                case JointType.HipRight:
                    {
                        sensorNumber = 20;
                        break;
                    }
                case JointType.KneeRight:
                    {
                        sensorNumber = 21;
                        break;
                    }
                case JointType.AnkleRight:
                    {
                        sensorNumber = 22;
                        break;
                    }
                case JointType.FootRight:
                    {
                        sensorNumber = 23;
                        break;
                    }
            }

            return sensorNumber;
        }
        private void SendSkeletonVRPN(Skeleton skeleton, int id)
        {
            foreach (Joint joint in skeleton.Joints)
            {
                //I could include inferred joints as well, should I? 
                if (joint.TrackingState != JointTrackingState.NotTracked)
                {
                    Vector4 boneQuat = skeleton.BoneOrientations[joint.JointType].AbsoluteRotation.Quaternion;
                    lock (trackerServers[id])
                    {
                        trackerServers[id].ReportPose(GetSkeletonSensorNumber(joint.JointType), DateTime.Now,
                                                      new Point3D(joint.Position.X, joint.Position.Y, joint.Position.Z),
                                                      new Quaternion(boneQuat.W, boneQuat.X, boneQuat.Y, boneQuat.Z));
                    }
                }
            }
        }
        private List<KinectSkeleton> SortSkeletons(List<KinectSkeleton> unsortedSkeletons, SkeletonSortMethod sortMethod)
        {
            if (sortMethod == SkeletonSortMethod.NoSort)
            {
                return unsortedSkeletons;
            }
            else
            {
                //Seperate the tracked and untracked skeletons
                List<KinectSkeleton> trackedSkeletons = new List<KinectSkeleton>();
                List<KinectSkeleton> untrackedSkeletons = new List<KinectSkeleton>();
                for (int i = 0; i < unsortedSkeletons.Count; i++)
                {
                    if (unsortedSkeletons[i].skeleton.TrackingState == SkeletonTrackingState.NotTracked)
                    {
                        untrackedSkeletons.Add(unsortedSkeletons[i]);
                    }
                    else
                    {
                        trackedSkeletons.Add(unsortedSkeletons[i]);
                    }
                }

                if (sortMethod == SkeletonSortMethod.OriginXClosest || sortMethod == SkeletonSortMethod.OriginXFarthest)
                {
                    //We only care about the tracked skeletons, so only sort those
                    for (int i = 1; i < trackedSkeletons.Count; i++)
                    {
                        int insertIndex = i;
                        KinectSkeleton tempSkeleton = trackedSkeletons[i];

                        while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.X) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.X))
                        {
                            trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                            insertIndex--;
                        }
                        trackedSkeletons[insertIndex] = tempSkeleton;
                    }

                    if (sortMethod == SkeletonSortMethod.OriginXFarthest)
                    {
                        trackedSkeletons.Reverse();
                    }
                }
                else if (sortMethod == SkeletonSortMethod.OriginYClosest || sortMethod == SkeletonSortMethod.OriginYFarthest)
                {
                    //We only care about the tracked skeletons, so only sort those
                    for (int i = 1; i < trackedSkeletons.Count; i++)
                    {
                        int insertIndex = i;
                        KinectSkeleton tempSkeleton = trackedSkeletons[i];

                        while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.Y) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.Y))
                        {
                            trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                            insertIndex--;
                        }
                        trackedSkeletons[insertIndex] = tempSkeleton;
                    }

                    if (sortMethod == SkeletonSortMethod.OriginYFarthest)
                    {
                        trackedSkeletons.Reverse();
                    }
                }
                else if (sortMethod == SkeletonSortMethod.OriginZClosest || sortMethod == SkeletonSortMethod.OriginZFarthest)
                {
                    //We only care about the tracked skeletons, so only sort those
                    for (int i = 1; i < trackedSkeletons.Count; i++)
                    {
                        int insertIndex = i;
                        KinectSkeleton tempSkeleton = trackedSkeletons[i];

                        while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.Z) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.Z))
                        {
                            trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                            insertIndex--;
                        }
                        trackedSkeletons[insertIndex] = tempSkeleton;
                    }

                    if (sortMethod == SkeletonSortMethod.OriginZFarthest)
                    {
                        trackedSkeletons.Reverse();
                    }
                }
                else if (sortMethod == SkeletonSortMethod.OriginEuclidClosest || sortMethod == SkeletonSortMethod.OriginEuclidFarthest)
                {
                    //We only care about the tracked skeletons, so only sort those
                    for (int i = 1; i < trackedSkeletons.Count; i++)
                    {
                        int insertIndex = i;
                        KinectSkeleton tempSkeleton = trackedSkeletons[i];
                        SkeletonPoint origin = new SkeletonPoint() { X = 0, Y = 0, Z = 0 };
                        double tempDistance = InterPointDistance(origin, trackedSkeletons[i].skeleton.Position);

                        while (insertIndex > 0 && tempDistance < InterPointDistance(origin, trackedSkeletons[insertIndex - 1].skeleton.Position))
                        {
                            trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                            insertIndex--;
                        }
                        trackedSkeletons[insertIndex] = tempSkeleton;
                    }

                    if (sortMethod == SkeletonSortMethod.OriginEuclidFarthest)
                    {
                        trackedSkeletons.Reverse();
                    }
                }
                else if (feedbackPosition != null)  //Sort based on the feedback position, if it isn't null
                {
                    if (sortMethod == SkeletonSortMethod.FeedbackXClosest || sortMethod == SkeletonSortMethod.FeedbackXFarthest)
                    {
                        //We only care about the tracked skeletons, so only sort those
                        for (int i = 1; i < trackedSkeletons.Count; i++)
                        {
                            int insertIndex = i;
                            KinectSkeleton tempSkeleton = trackedSkeletons[i];

                            while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.X - feedbackPosition.Value.X) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.X - feedbackPosition.Value.X))
                            {
                                trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                                insertIndex--;
                            }
                            trackedSkeletons[insertIndex] = tempSkeleton;
                        }

                        if (sortMethod == SkeletonSortMethod.FeedbackXFarthest)
                        {
                            trackedSkeletons.Reverse();
                        }
                    }
                    else if (sortMethod == SkeletonSortMethod.FeedbackYClosest || sortMethod == SkeletonSortMethod.FeedbackYFarthest)
                    {
                        //We only care about the tracked skeletons, so only sort those
                        for (int i = 1; i < trackedSkeletons.Count; i++)
                        {
                            int insertIndex = i;
                            KinectSkeleton tempSkeleton = trackedSkeletons[i];

                            while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.Y - feedbackPosition.Value.Y) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.Y - feedbackPosition.Value.Y))
                            {
                                trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                                insertIndex--;
                            }
                            trackedSkeletons[insertIndex] = tempSkeleton;
                        }

                        if (sortMethod == SkeletonSortMethod.FeedbackYFarthest)
                        {
                            trackedSkeletons.Reverse();
                        }
                    }
                    else if (sortMethod == SkeletonSortMethod.FeedbackZClosest || sortMethod == SkeletonSortMethod.FeedbackZFarthest)
                    {
                        //We only care about the tracked skeletons, so only sort those
                        for (int i = 1; i < trackedSkeletons.Count; i++)
                        {
                            int insertIndex = i;
                            KinectSkeleton tempSkeleton = trackedSkeletons[i];

                            while (insertIndex > 0 && Math.Abs(tempSkeleton.skeleton.Position.Z - feedbackPosition.Value.Z) < Math.Abs(trackedSkeletons[insertIndex - 1].skeleton.Position.Z - feedbackPosition.Value.Z))
                            {
                                trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                                insertIndex--;
                            }
                            trackedSkeletons[insertIndex] = tempSkeleton;
                        }

                        if (sortMethod == SkeletonSortMethod.FeedbackZFarthest)
                        {
                            trackedSkeletons.Reverse();
                        }
                    }
                    else if (sortMethod == SkeletonSortMethod.FeedbackEuclidClosest || sortMethod == SkeletonSortMethod.FeedbackEuclidFarthest)
                    {
                        //We only care about the tracked skeletons, so only sort those
                        for (int i = 1; i < trackedSkeletons.Count; i++)
                        {
                            int insertIndex = i;
                            KinectSkeleton tempSkeleton = trackedSkeletons[i];
                            SkeletonPoint feedPosition = new SkeletonPoint() { X = (float)feedbackPosition.Value.X, Y = (float)feedbackPosition.Value.Y, Z = (float)feedbackPosition.Value.Z };
                            double tempDistance = InterPointDistance(feedPosition, trackedSkeletons[i].skeleton.Position);

                            while (insertIndex > 0 && tempDistance < InterPointDistance(feedPosition, trackedSkeletons[insertIndex - 1].skeleton.Position))
                            {
                                trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                                insertIndex--;
                            }
                            trackedSkeletons[insertIndex] = tempSkeleton;
                        }

                        if (sortMethod == SkeletonSortMethod.FeedbackEuclidFarthest)
                        {
                            trackedSkeletons.Reverse();
                        }
                    }
                    else
                    {
                        return unsortedSkeletons;
                    }
                }
                else
                {
                    return unsortedSkeletons;
                }

                //Add the untracked skeletons to the tracked ones before sending everything back
                trackedSkeletons.AddRange(untrackedSkeletons);

                return trackedSkeletons;
            }
        }

        private delegate void runServerCoreDelegate();
        private delegate void launchVoiceRecognizerDelegate();
    }

    class PassedSkeleton
    {
        internal Skeleton skeletonData;
        internal bool? leftHandGripped;
        internal bool? rightHandGripped;
    }

    enum ServerRunState {Starting, Running, Stopping, Stopped}
}