using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Interaction;

namespace KinectWithVRServer
{
    class KinectCore
    {
        internal KinectSensor kinect;
        internal int kinectID;
        MainWindow parent;
        public WriteableBitmap depthImage;
        short[] depthImagePixels;
        byte[] colorImagePixels;
        public WriteableBitmap colorImage;
        CoordinateMapper mapper;
        bool isGUI = false;
        ServerCore server;
        public int skelcount;
        private InteractionStream interactStream;
        private List<double> depthTimeIntervals = new List<double>();
        private List<double> colorTimeIntervals = new List<double>();
        private Int64 lastDepthTime = 0;
        private Int64 lastColorTime = 0;
        private Skeleton[] skeletons = null;
        private System.Timers.Timer accelerationUpdateTimer;

        //The parent has to be optional to allow for console operation
        public KinectCore(ServerCore mainServer, MainWindow thisParent = null, int? kinectNumber = null)
        {
            if (kinectNumber != null)
            {
                server = mainServer;
                if (server == null)
                {
                    throw new Exception("Server does not exist.");
                }

                parent = thisParent;
                if (parent != null)
                {
                    isGUI = true;
                }

                if (KinectSensor.KinectSensors.Count > kinectNumber)
                {
                    //Get the sensor index in the global list
                    int globalIndex = -1;
                    for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
                    {
                        if (KinectSensor.KinectSensors[i].DeviceConnectionId == server.serverMasterOptions.kinectOptions[(int)kinectNumber].connectionID)
                        {
                            globalIndex = i;
                            break;
                        }
                    }
                    kinect = KinectSensor.KinectSensors[globalIndex];
                    kinectID = (int)kinectNumber;
                }
                else
                {
                    throw new System.IndexOutOfRangeException("Specified Kinect sensor does not exist");
                }

                if (isGUI)
                {
                    LaunchKinect();
                }
                else
                {
                    launchKinectDelegate kinectDelegate = LaunchKinect;
                    IAsyncResult result = kinectDelegate.BeginInvoke(null, null);
                    kinectDelegate.EndInvoke(result);  //Even though this is blocking, the events should be on a different thread now.
                }
            }
            else
            {
                throw new NullReferenceException("To create a KinectCore object, the KinectNumber must be valid.");
            }
        }
        public void ShutdownSensor()
        {
            if (kinect != null)
            {
                //TODO: Should these really be "new" when we are REMOVING them from the event queue?
                kinect.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                kinect.DepthFrameReady -= new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                interactStream.InteractionFrameReady -= new EventHandler<InteractionFrameReadyEventArgs>(interactStream_InteractionFrameReady);
                if (accelerationUpdateTimer != null)
                {
                    accelerationUpdateTimer.Stop();
                    accelerationUpdateTimer.Elapsed -= accelerationUpdateTimer_Elapsed;
                    accelerationUpdateTimer.Dispose();
                }

                interactStream.Dispose();
                interactStream = null;

                kinect.AudioSource.Stop();
                kinect.Stop();
            }
        }

        public void ChangeColorResolution(ColorImageFormat newResolution)
        {
            kinect.ColorStream.Disable();
            if (newResolution != ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(newResolution);
                if (newResolution == ColorImageFormat.InfraredResolution640x480Fps30)
                {
                    colorImage = new WriteableBitmap(kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                }
                else
                {
                    colorImage = new WriteableBitmap(kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                }

                //Re-link the writeable bitmap to the GUI if necessary
                if (isGUI && parent.ColorStreamConnectionID == kinect.DeviceConnectionId)
                {
                    parent.ColorImage.Source = colorImage;
                }
            }
        }
        public void ChangeDepthResolution(DepthImageFormat newResolution)
        {
            kinect.DepthStream.Disable();
            if (newResolution != DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable(newResolution);
                depthImage = new WriteableBitmap(kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);

                //Re-link the writeable bitmap to the GUI if necessary
                if (isGUI && parent.DepthStreamConnectionID == kinect.DeviceConnectionId)
                {
                    parent.DepthImage.Source = depthImage;
                }
            }
        }

        private void LaunchKinect()
        {
            //Setup default properties
            if (server.serverMasterOptions.kinectOptions[kinectID].colorImageMode != ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(server.serverMasterOptions.kinectOptions[kinectID].colorImageMode);
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
            }
            if (server.serverMasterOptions.kinectOptions[kinectID].depthImageMode != DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable();
                kinect.SkeletonStream.Enable(); //Note, the audio stream MUST be started AFTER this (known issue with SDK v1.7).  Currently not an issue as the audio isn't started until the server is launched later in the code.
                kinect.SkeletonStream.EnableTrackingInNearRange = true; //Explicitly enable depth tracking in near mode (this can be true when the depth mode is near or default, but if it is false, there is not skeleton data in near mode)
                interactStream = new InteractionStream(kinect, new DummyInteractionClient());
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                kinect.SkeletonStream.EnableTrackingInNearRange = true;
                interactStream.InteractionFrameReady += new EventHandler<InteractionFrameReadyEventArgs>(interactStream_InteractionFrameReady);
            }

            if (isGUI)
            {
                //Setup the images for the display
                depthImage = new WriteableBitmap(kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                parent.DepthImage.Source = depthImage;
                colorImage = new WriteableBitmap(kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                parent.ColorImage.Source = colorImage;
            }

            //Create the coordinate mapper
            mapper = new CoordinateMapper(kinect);

            kinect.Start();

            //Start the audio streams, if necessary -- NOTE: This must be after the skeleton stream is started (which it is here)
            //TODO: What if the settings are changed after the Kinect is started?
            if (server.serverMasterOptions.kinectOptions[kinectID].sendAudioAngle || server.serverMasterOptions.audioOptions.sourceID == kinectID)
            {
                if (server.serverMasterOptions.audioOptions.sourceID == kinectID)
                {
                    kinect.AudioSource.EchoCancellationMode = server.serverMasterOptions.audioOptions.echoMode;
                    kinect.AudioSource.AutomaticGainControlEnabled = server.serverMasterOptions.audioOptions.autoGainEnabled;
                    kinect.AudioSource.NoiseSuppression = server.serverMasterOptions.audioOptions.noiseSurpression;
                    if (server.serverMasterOptions.kinectOptions[kinectID].sendAudioAngle)
                    {
                        if (server.serverMasterOptions.kinectOptions[kinectID].audioTrackMode != AudioTrackingMode.Loudest)
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
                        }
                        else
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
                        }
                        kinect.AudioSource.SoundSourceAngleChanged += AudioSource_SoundSourceAngleChanged;
                    }
                }
                else if (server.serverMasterOptions.kinectOptions[kinectID].sendAudioAngle)
                {
                    kinect.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
                    kinect.AudioSource.AutomaticGainControlEnabled = false;
                    kinect.AudioSource.NoiseSuppression = true;
                    if (server.serverMasterOptions.kinectOptions[kinectID].audioTrackMode != AudioTrackingMode.Loudest)
                    {
                        kinect.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
                    }
                    else
                    {
                        kinect.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
                    }
                    kinect.AudioSource.SoundSourceAngleChanged += AudioSource_SoundSourceAngleChanged;
                }

                kinect.AudioSource.Start();
            }

            StartAccelTimer();
        }
        private void StartAccelTimer()
        {
            accelerationUpdateTimer = new System.Timers.Timer();
            accelerationUpdateTimer.AutoReset = true;
            accelerationUpdateTimer.Interval = 33.333;
            accelerationUpdateTimer.Elapsed += accelerationUpdateTimer_Elapsed;
            accelerationUpdateTimer.Start();
        }
        //Updates the acceleration on the GUI and the server
        //While 30 times per second is probably a bit fast for the GUI, something on the VRPN side may need it this fast
        private void accelerationUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool dataValid = false;
            Vector4 acceleration = new Vector4();
            int elevationAngle = 0;
            lock (kinect)
            {
                if (kinect.IsRunning)
                {
                    acceleration = kinect.AccelerometerGetCurrentReading();
                    elevationAngle = kinect.ElevationAngle;
                    dataValid = true;
                }
            }

            //Update the GUI
            if (dataValid)
            {
                if (isGUI && parent.kinectOptionGUIPages[kinectID].IsVisible)
                {
                    //Note: This method is on a different thread from the rest of the KinectCore because of the timer, thus the need for the invoke
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        parent.kinectOptionGUIPages[kinectID].AccelXTextBlock.Text = acceleration.X.ToString("F2");
                        parent.kinectOptionGUIPages[kinectID].AccelYTextBlock.Text = acceleration.Y.ToString("F2");
                        parent.kinectOptionGUIPages[kinectID].AccelZTextBlock.Text = acceleration.Z.ToString("F2");
                        parent.kinectOptionGUIPages[kinectID].AngleTextBlock.Text = elevationAngle.ToString();
                    }), null
                    );
                }

                //Update the VRPN server
                if (server.isRunning && server.serverMasterOptions.kinectOptions[kinectID].sendAcceleration)
                {
                    for (int i = 0; i < server.analogServers.Count; i++)
                    {
                        if (server.serverMasterOptions.analogServers[i].serverName == server.serverMasterOptions.kinectOptions[kinectID].accelerationServerName)
                        {
                            lock (server.analogServers[i])
                            {
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptions[kinectID].accelXChannel].Value = acceleration.X;
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptions[kinectID].accelYChannel].Value = acceleration.Y;
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptions[kinectID].accelZChannel].Value = acceleration.Z;
                            }
                            break;
                        }
                    }
                }
            }
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skelFrame = e.OpenSkeletonFrame())
            {
                if (skelFrame != null)
                {
                    if (isGUI)
                    {
                        parent.ColorImageCanvas.Children.Clear();
                    }

                    skeletons = new Skeleton[skelFrame.SkeletonArrayLength]; 
                    skelFrame.CopySkeletonDataTo(skeletons);
                    int index = 0;

                    skeletons = SortSkeletons(skeletons, server.serverMasterOptions.skeletonOptions.skeletonSortMode);
                    skelcount = 0;

                    foreach (Skeleton skel in skeletons)
                    {
                        //Pick a color for the bones and joints based off the player ID
                        Color renderColor = Colors.White;
                        if (index == 0)
                        {
                            renderColor = Colors.Red;
                        }
                        else if (index == 1)
                        {
                            renderColor = Colors.Blue;
                        }
                        else if (index == 2)
                        {
                            renderColor = Colors.Green;
                        }
                        else if (index == 3)
                        {
                            renderColor = Colors.Yellow;
                        }
                        else if (index == 4)
                        {
                            renderColor = Colors.Cyan;
                        }
                        else if (index == 5)
                        {
                            renderColor = Colors.Fuchsia;
                        }

                        //Send the points across if the skeleton is either tracked or has a position
                        if (skel.TrackingState != SkeletonTrackingState.NotTracked)
                        {
                            if (server.serverMasterOptions.kinectOptions[kinectID].trackSkeletons)
                            {
                                skelcount++;

                                if (server.isRunning)
                                {
                                    SendSkeletonVRPN(skel, index);
                                }
                                if (isGUI && parent.ColorStreamConnectionID == kinect.DeviceConnectionId)
                                {
                                    RenderSkeletonOnColor(skel, renderColor);
                                }
                            }
                        }

                        index++;
                    }

                    //Pass the data to the interaction stream for processing
                    if (interactStream != null && server.serverMasterOptions.kinectOptions[kinectID].trackSkeletons)
                    {
                        Vector4 accelReading = kinect.AccelerometerGetCurrentReading();
                        interactStream.ProcessSkeleton(skeletons, accelReading, skelFrame.Timestamp);
                    }
                }

                if (isGUI)
                {
                    parent.TrackedSkeletonsTextBlock.Text = skelcount.ToString();
                }
            }
        }
        private void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    //Pass the data to the interaction frame for processing
                    if (interactStream != null && parent.server.serverMasterOptions.kinectOptions[kinectID].trackSkeletons)
                    {
                        interactStream.ProcessDepth(frame.GetRawPixelData(), frame.Timestamp);
                    }

                    if (isGUI && parent.DepthStreamConnectionID == kinect.DeviceConnectionId)
                    {
                        depthImagePixels = new short[frame.PixelDataLength];
                        frame.CopyPixelDataTo(depthImagePixels);
                        depthImage.WritePixels(new System.Windows.Int32Rect(0, 0, frame.Width, frame.Height), depthImagePixels, frame.Width * frame.BytesPerPixel, 0);
                        
                        //Display the frame rate on the GUI
                        double tempFPS = CalculateFrameRate(frame.Timestamp, ref lastDepthTime, ref depthTimeIntervals);
                        parent.DepthFPSTextBlock.Text = tempFPS.ToString("F1");
                    }
                }
            }
        }
        private void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    if (isGUI && parent.ColorStreamConnectionID == kinect.DeviceConnectionId)
                    {
                        colorImagePixels = new byte[frame.PixelDataLength];
                        frame.CopyPixelDataTo(colorImagePixels);
                        colorImage.WritePixels(new System.Windows.Int32Rect(0, 0, frame.Width, frame.Height), colorImagePixels, frame.Width * frame.BytesPerPixel, 0);

                        //Display the frame rate on the GUI
                        double tempFPS = CalculateFrameRate(frame.Timestamp, ref lastColorTime, ref colorTimeIntervals);
                        parent.ColorFPSTextBlock.Text = tempFPS.ToString("F1");
                    }
                }
            }
        }
        private void interactStream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame interactFrame = e.OpenInteractionFrame())
            {
                if (interactFrame != null && skeletons != null)
                {
                    UserInfo[] tempUserInfo = new UserInfo[6];
                    interactFrame.CopyInteractionDataTo(tempUserInfo);

                    foreach (UserInfo info in tempUserInfo)
                    {
                        int skeletonIndex = -1;

                        for (int i = 0; i < skeletons.Length; i++)
                        {
                            if (info.SkeletonTrackingId == skeletons[i].TrackingId)
                            {
                                skeletonIndex = i;
                                break;
                            }
                        }

                        //TODO: Move the transmitting of the grip to the merge and transmit method in the serverCore so we can check for disagreements between various viewpoints
                        if (skeletonIndex >= 0)
                        {
                            foreach (InteractionHandPointer hand in info.HandPointers)
                            {
                                if (hand.HandEventType != InteractionHandEventType.None)
                                {
                                    
                                    int gestureIndex = -1;
                                    int serverIndex = -1;
                                    bool sendGrip = server.isRunning && server.serverMasterOptions.kinectOptions[kinectID].trackSkeletons;

                                    if (sendGrip)
                                    {
                                        //Figure out which gesture command the grip is
                                        for (int i = 0; i < server.serverMasterOptions.gestureCommands.Count; i++)
                                        {
                                            //Technically, there should be TWO gesture commands per skeletons, but we only need one for now
                                            //TODO: Make this more robust
                                            //if (server.serverMasterOptions.gestureCommands[i].skeletonNumber == skeletonIndex)
                                            //{
                                            //    gestureIndex = skeletonIndex;
                                            //    break;
                                            //}
                                        }

                                        //Figure out which tracking server the grip is to be transmitted on
                                        for (int i = 0; i < server.buttonServers.Count; i++)
                                        {
                                            if (server.serverMasterOptions.buttonServers[i].serverName == server.serverMasterOptions.gestureCommands[gestureIndex].serverName)
                                            {
                                                serverIndex = i;
                                                break;
                                            }
                                        }
                                    }

                                    if (hand.HandEventType == InteractionHandEventType.Grip)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            if (sendGrip)
                                            {
                                                server.buttonServers[serverIndex].Buttons[1] = true;
                                            }
                                            HelperMethods.WriteToLog("Skeleton " + skeletonIndex + " left hand closed!", parent);
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            if (sendGrip)
                                            {
                                                server.buttonServers[serverIndex].Buttons[0] = true;
                                            }
                                            HelperMethods.WriteToLog("Skeleton " + skeletonIndex + " right hand closed!", parent);
                                        }
                                    }
                                    else if (hand.HandEventType == InteractionHandEventType.GripRelease)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            if (sendGrip)
                                            {
                                                server.buttonServers[serverIndex].Buttons[1] = false;
                                            }
                                            HelperMethods.WriteToLog("Skeleton " + skeletonIndex + " left hand opened!", parent);
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            if (sendGrip)
                                            {
                                                server.buttonServers[serverIndex].Buttons[0] = false;
                                            }
                                            HelperMethods.WriteToLog("Skeleton " + skeletonIndex + " right hand opened!", parent);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        void AudioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            if (server.serverMasterOptions.kinectOptions[kinectID].sendAudioAngle && server.isRunning)
            {
                for (int i = 0; i < server.serverMasterOptions.analogServers.Count; i++)
                {
                    if (server.serverMasterOptions.analogServers[i].serverName == server.serverMasterOptions.kinectOptions[kinectID].audioAngleServerName)
                    {
                        lock (server.analogServers[i])
                        {
                            server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptions[kinectID].audioAngleChannel].Value = e.Angle;
                        }
                    }
                }
            }
        }
        private void SendSkeletonVRPN(Skeleton skeleton, int id)
        {
            foreach (Joint joint in skeleton.Joints)
            {
                //I could include inferred joints as well, should I? 
                if (joint.TrackingState != JointTrackingState.NotTracked)
                {
                    Vector4 boneQuat = skeleton.BoneOrientations[joint.JointType].AbsoluteRotation.Quaternion;
                    lock (server.trackerServers[id])
                    {
                        server.trackerServers[id].ReportPose(GetSkeletonSensorNumber(joint.JointType), DateTime.Now,
                                                             new Vector3D(joint.Position.X, joint.Position.Y, joint.Position.Z),
                                                             new Quaternion(boneQuat.W, boneQuat.X, boneQuat.Y, boneQuat.Z));
                    }
                }
            }
        }
        private void RenderSkeletonOnColor(Skeleton skeleton, Color renderColor)
        {
            //Calculate the offset
            Point offset = new Point(0.0, 0.0);
            if (parent.ColorImageCanvas.ActualWidth != parent.ColorImage.ActualWidth)
            {
                offset.X = (parent.ColorImageCanvas.ActualWidth - parent.ColorImage.ActualWidth) / 2;
            }

            if (parent.ColorImageCanvas.ActualHeight != parent.ColorImage.ActualHeight)
            {
                offset.Y = (parent.ColorImageCanvas.ActualHeight - parent.ColorImage.ActualHeight) / 2;
            }
                            
            //Render all the bones (this can't be looped because the enum isn't ordered in order of bone connections)
            DrawBoneOnColor(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight], renderColor, 2.0, offset);
            DrawBoneOnColor(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight], renderColor, 2.0, offset);
            
            foreach (Joint joint in skeleton.Joints)
            {
                DrawJointPointOnColor(joint, renderColor, 2.0, offset);
            }
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
                    sensorNumber =20;
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
        private void DrawBoneOnColor(Joint startJoint, Joint endJoint, Color boneColor, double thickness, Point offset)
        {
            if (startJoint.TrackingState == JointTrackingState.Tracked && endJoint.TrackingState == JointTrackingState.Tracked)
            {
                //Map the joint from the skeleton to the color image
                ColorImagePoint startPoint = mapper.MapSkeletonPointToColorPoint(startJoint.Position, kinect.ColorStream.Format);
                ColorImagePoint endPoint = mapper.MapSkeletonPointToColorPoint(endJoint.Position, kinect.ColorStream.Format);

                //Calculate the coordinates on the image (the offset of the image is added in the next section)
                Point imagePointStart = new Point(0.0, 0.0);
                imagePointStart.X = ((double)startPoint.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth;
                imagePointStart.Y = ((double)startPoint.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight;
                Point imagePointEnd = new Point(0.0, 0.0);
                imagePointEnd.X = ((double)endPoint.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth;
                imagePointEnd.Y = ((double)endPoint.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight;

                //Generate the line for the bone
                Line line = new Line();
                line.Stroke = new SolidColorBrush(boneColor);
                line.StrokeThickness = thickness;
                line.X1 = imagePointStart.X + offset.X;
                line.X2 = imagePointEnd.X + offset.X;
                line.Y1 = imagePointStart.Y + offset.Y;
                line.Y2 = imagePointEnd.Y + offset.Y;
                parent.ColorImageCanvas.Children.Add(line);
            }
        }
        private void DrawJointPointOnColor(Joint joint, Color jointColor, double radius, Point offset)
        {
            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                //Map the joint from the skeleton to the color image
                ColorImagePoint point = mapper.MapSkeletonPointToColorPoint(joint.Position, kinect.ColorStream.Format);

                //Calculate the coordinates on the image (the offset is also added in this section)
                Point imagePoint = new Point(0.0, 0.0);
                imagePoint.X = ((double)point.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth + offset.X;
                imagePoint.Y = ((double)point.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight + offset.Y;

                //Generate the circle for the joint
                Ellipse circle = new Ellipse();
                circle.Fill = new SolidColorBrush(jointColor);
                circle.StrokeThickness = 0.0;
                circle.Margin = new Thickness(imagePoint.X - radius, imagePoint.Y - radius, 0, 0);
                circle.HorizontalAlignment = HorizontalAlignment.Left;
                circle.VerticalAlignment = VerticalAlignment.Top;
                circle.Height = radius * 2;
                circle.Width = radius * 2;
                parent.ColorImageCanvas.Children.Add(circle);
            }
        }
        private static double CalculateFrameRate(Int64 currentTimeStamp, ref Int64 lastTimeStamp, ref List<double> oldIntervals)
        {
            double newInterval = (double)(currentTimeStamp - lastTimeStamp);
            lastTimeStamp = currentTimeStamp;

            if (oldIntervals.Count >= 10) //Computes a running average of 10 frames for stability
            {
                oldIntervals.RemoveAt(0);
            }
            oldIntervals.Add(newInterval);

            return (1.0 / oldIntervals.Average() * 1000.0);
        }

        private Skeleton[] SortSkeletons(Skeleton[] unsortedSkeletons, SkeletonSortMethod sortMethod)
        {
            if (sortMethod == SkeletonSortMethod.NoSort)
            {
                return unsortedSkeletons;
            }
            else
            {
                //Seperate the tracked and untracked skeletons
                List<Skeleton> trackedSkeletons = new List<Skeleton>();
                List<Skeleton> untrackedSkeletons = new List<Skeleton>();
                for (int i = 0; i < unsortedSkeletons.Length; i++)
                {
                    if (unsortedSkeletons[i].TrackingState == SkeletonTrackingState.NotTracked)
                    {
                        untrackedSkeletons.Add(unsortedSkeletons[i]);
                    }
                    else
                    {
                        trackedSkeletons.Add(unsortedSkeletons[i]);
                    }
                }

                if (sortMethod == SkeletonSortMethod.Closest || sortMethod == SkeletonSortMethod.Farthest)
                {
                    //We only care about the tracked skeletons, so only sort those
                    for (int i = 1; i < trackedSkeletons.Count; i++)
                    {
                        int insertIndex = i;
                        Skeleton tempSkeleton = trackedSkeletons[i];

                        while (insertIndex > 0 && tempSkeleton.Position.Z < trackedSkeletons[insertIndex - 1].Position.Z)
                        {
                            trackedSkeletons[insertIndex] = trackedSkeletons[insertIndex - 1];
                            insertIndex--;
                        }
                        trackedSkeletons[insertIndex] = tempSkeleton;
                    }

                    if (sortMethod == SkeletonSortMethod.Farthest)
                    {
                        trackedSkeletons.Reverse();
                    }
                }

                //Add the untracked skeletons to the tracked ones before sending everything back
                trackedSkeletons.AddRange(untrackedSkeletons);

                return trackedSkeletons.ToArray();
            }
        }

        private delegate void launchKinectDelegate();
    }

    internal class KinectCoreComparer : IComparer<KinectCore>
    {
        public int Compare(KinectCore x, KinectCore y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }

    public class DummyInteractionClient : IInteractionClient
    {
        public InteractionInfo GetInteractionInfoAtLocation(int skeletonTrackingId, InteractionHandType handType, double x, double y)
        {
            InteractionInfo result = new InteractionInfo();
            result.IsGripTarget = true;
            result.IsPressTarget = true;
            result.PressAttractionPointX = 0.5;
            result.PressAttractionPointY = 0.5;
            result.PressTargetControlId = 1;
            return result;
        }
    }
}