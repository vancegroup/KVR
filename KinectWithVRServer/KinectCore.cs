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
        internal CoordinateMapper mapper;
        bool isGUI = false;
        ServerCore server;
        public int skelcount;
        private InteractionStream interactStream;
        private List<double> depthTimeIntervals = new List<double>();
        private List<double> colorTimeIntervals = new List<double>();
        private Int64 lastDepthTime = 0;
        private Int64 lastColorTime = 0;
        //private Skeleton[] skeletons = null;
        private System.Timers.Timer accelerationUpdateTimer;
        internal KinectSkeletonsData skeletonData;
        internal Matrix3D skeletonTransformation = Matrix3D.Identity;

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
                        if (KinectSensor.KinectSensors[i].DeviceConnectionId == server.serverMasterOptions.kinectOptionsList[(int)kinectNumber].connectionID)
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
                //The "new" syntax is sort of odd, but these really do remove the handlers from the specified events
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

                if (kinect.AudioSource != null)
                {
                    kinect.AudioSource.Stop();
                }
                kinect.Stop();
            }
        }
        public void StartKinectAudio()
        {
            if (kinect.IsRunning)
            {
                //Start the audio streams, if necessary -- NOTE: This must be after the skeleton stream is started (which it should be here)
                if (server.serverMasterOptions.kinectOptionsList[kinectID].sendAudioAngle || server.serverMasterOptions.audioOptions.sourceID == kinectID)
                {
                    if (server.serverMasterOptions.audioOptions.sourceID == kinectID)
                    {
                        kinect.AudioSource.EchoCancellationMode = server.serverMasterOptions.audioOptions.echoMode;
                        kinect.AudioSource.AutomaticGainControlEnabled = server.serverMasterOptions.audioOptions.autoGainEnabled;
                        kinect.AudioSource.NoiseSuppression = server.serverMasterOptions.audioOptions.noiseSurpression;
                        if (server.serverMasterOptions.kinectOptionsList[kinectID].sendAudioAngle)
                        {
                            if (server.serverMasterOptions.kinectOptionsList[kinectID].audioTrackMode != AudioTrackingMode.Loudest)
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
                    else if (server.serverMasterOptions.kinectOptionsList[kinectID].sendAudioAngle)
                    {
                        kinect.AudioSource.EchoCancellationMode = EchoCancellationMode.None;
                        kinect.AudioSource.AutomaticGainControlEnabled = false;
                        kinect.AudioSource.NoiseSuppression = true;
                        if (server.serverMasterOptions.kinectOptionsList[kinectID].audioTrackMode != AudioTrackingMode.Loudest)
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
            if (server.serverMasterOptions.kinectOptionsList[kinectID].colorImageMode != ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(server.serverMasterOptions.kinectOptionsList[kinectID].colorImageMode);
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
            }
            if (server.serverMasterOptions.kinectOptionsList[kinectID].depthImageMode != DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable();
                kinect.SkeletonStream.Enable(); //Note, the audio stream MUST be started AFTER this (known issue with SDK v1.7).  Currently not an issue as the audio isn't started until the server is launched later in the code.
                kinect.SkeletonStream.EnableTrackingInNearRange = true; //Explicitly enable depth tracking in near mode (this can be true when the depth mode is near or default, but if it is false, there is not skeleton data in near mode)
            
                //Create the coordinate mapper
                mapper = new CoordinateMapper(kinect);
                
                //Create the skeleton data container
                skeletonData = new KinectSkeletonsData(kinect.DeviceConnectionId);
                skeletonData.PropertyChanged += server.PerKinectSkeletons_PropertyChanged;
                
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

            kinect.Start();

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
        //Updates the acceleration on the GUI and the server, 30 FPS may be a little fast for the GUI, but for VRPN, it probably needs to be at least that fast
        private void accelerationUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            bool dataValid = false;
            Vector4 acceleration = new Vector4();
            int elevationAngle = 0;
            lock (kinect)
            {
                if (kinect.IsRunning)
                {
                    //TODO: Fix this, because it is messed up!
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
                if (server.isRunning && server.serverMasterOptions.kinectOptionsList[kinectID].sendAcceleration)
                {
                    for (int i = 0; i < server.analogServers.Count; i++)
                    {
                        if (server.serverMasterOptions.analogServers[i].serverName == server.serverMasterOptions.kinectOptionsList[kinectID].accelerationServerName)
                        {
                            lock (server.analogServers[i])
                            {
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptionsList[kinectID].accelXChannel].Value = acceleration.X;
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptionsList[kinectID].accelYChannel].Value = acceleration.Y;
                                server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptionsList[kinectID].accelZChannel].Value = acceleration.Z;
                                server.analogServers[i].Report();
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
                if (skelFrame != null && server.serverMasterOptions.kinectOptionsList[kinectID].trackSkeletons)
                {
                    Skeleton[] skeletons = new Skeleton[6];
                    skelFrame.CopySkeletonDataTo(skeletons);
                    Vector4 acceleration = kinect.AccelerometerGetCurrentReading();

                    if (interactStream != null)
                    {
                        interactStream.ProcessSkeleton(skeletons, acceleration, skelFrame.Timestamp);
                    }

                    
                    //Generate the transformation matrix for the skeletons
                    double kinectYaw = server.serverMasterOptions.kinectOptionsList[kinectID].kinectYaw;
                    Point3D kinectPosition = server.serverMasterOptions.kinectOptionsList[kinectID].kinectPosition;
                    Matrix3D gravityBasedKinectRotation = findRotation(new Vector3D(acceleration.X, acceleration.Y, acceleration.Z), new Vector3D(0, -1, 0));
                    AxisAngleRotation3D yawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -kinectYaw);
                    RotateTransform3D tempTrans = new RotateTransform3D(yawRotation);
                    TranslateTransform3D transTrans = new TranslateTransform3D((Vector3D)kinectPosition);
                    Matrix3D masterMatrix = Matrix3D.Multiply(Matrix3D.Multiply(tempTrans.Value, gravityBasedKinectRotation), transTrans.Value);
                    skeletonTransformation = masterMatrix;  //This may need a lock on it, but that will require a seperate lock object

                    //Transform the skeletons based on the Kinect settings
                    for (int i = 0; i < skeletons.Length; i++)
                    {
                        if (skeletons[i].TrackingState != SkeletonTrackingState.NotTracked) //Don't bother to transform untracked skeletons
                        {
                            skeletons[i] = makeTransformedSkeleton(skeletons[i], masterMatrix);
                        }
                    }

                    //Add the skeletons to the list to be merged
                    lock (skeletonData.actualSkeletons)
                    {
                        //skeletonData.actualSkeletons.Clear();
                        for (int i = 0; i < skeletons.Length; i++)
                        {
                            skeletonData.actualSkeletons[i].skeleton = skeletons[i];
                        }
                    }

                    lock (skeletonData)
                    {
                        //The skeleton data is constaintly going to change, so lets just call the update everytime we get new data
                        skeletonData.needsUpdate = true;
                    }
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
                    if (interactStream != null/* && parent.server.serverMasterOptions.kinectOptions[kinectID].trackSkeletons*/)
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
                if (interactFrame != null && server.serverMasterOptions.kinectOptionsList[kinectID].trackSkeletons)
                {
                    bool changeMade = false;
                    UserInfo[] tempUserInfo = new UserInfo[6];
                    interactFrame.CopyInteractionDataTo(tempUserInfo);

                    foreach (UserInfo interactionInfo in tempUserInfo)
                    {
                        foreach (InteractionHandPointer hand in interactionInfo.HandPointers)
                        {
                            if (hand.HandEventType == InteractionHandEventType.Grip)
                            {
                                for (int i = 0; i < skeletonData.actualSkeletons.Count; i++)
                                {
                                    if (skeletonData.actualSkeletons[i].skeleton.TrackingId == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonData.actualSkeletons[i].leftHandClosed = true;
                                            changeMade = true;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonData.actualSkeletons[i].rightHandClosed = true;
                                            changeMade = true;
                                        }
                                        break;
                                    }
                                }
                            }
                            else if (hand.HandEventType == InteractionHandEventType.GripRelease)
                            {
                                for (int i = 0; i < skeletonData.actualSkeletons.Count; i++)
                                {
                                    if (skeletonData.actualSkeletons[i].skeleton.TrackingId == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonData.actualSkeletons[i].leftHandClosed = false;
                                            changeMade = true;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonData.actualSkeletons[i].rightHandClosed = false;
                                            changeMade = true;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    lock (skeletonData)
                    {
                        //The grab isn't going to update very often, so lets just force an update when it is actually needed
                        skeletonData.needsUpdate = changeMade;
                    }
                }
            }
        }
        void AudioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            if (server.serverMasterOptions.kinectOptionsList[kinectID].sendAudioAngle && server.isRunning)
            {
                for (int i = 0; i < server.serverMasterOptions.analogServers.Count; i++)
                {
                    if (server.serverMasterOptions.analogServers[i].serverName == server.serverMasterOptions.kinectOptionsList[kinectID].audioAngleServerName)
                    {
                        lock (server.analogServers[i])
                        {
                            server.analogServers[i].AnalogChannels[server.serverMasterOptions.kinectOptionsList[kinectID].audioAngleChannel].Value = e.Angle;
                            server.analogServers[i].Report();
                        }
                    }
                }
            }
        }

        #region Methods to transform the skeletons
        private Skeleton makeTransformedSkeleton(Skeleton inputSkel, Matrix3D transformationMatrix)
        {
            Skeleton adjSkel = new Skeleton();

            //Make sure the ancillary properties are copied over
            adjSkel.TrackingState = inputSkel.TrackingState;
            adjSkel.ClippedEdges = inputSkel.ClippedEdges;
            adjSkel.TrackingId = inputSkel.TrackingId;
            //Don't copy bone orientations, it appears they are calculated on the fly from the joint positions
            
            //Transform the skeleton position
            SkeletonPoint tempPosition = transform(inputSkel.Position, transformationMatrix);
            //tempPosition.X += (float)kinectLocation.X;
            //tempPosition.Y += (float)kinectLocation.Y;
            //tempPosition.Z += (float)kinectLocation.Z;
            adjSkel.Position = tempPosition;

            //Transform all the joint positions
            for (int j = 0; j < inputSkel.Joints.Count; j++)
            {
                Joint tempJoint = adjSkel.Joints[(JointType)j];
                tempJoint.TrackingState = inputSkel.Joints[(JointType)j].TrackingState;
                SkeletonPoint tempPoint = transform(inputSkel.Joints[(JointType)j].Position, transformationMatrix);
                //tempPoint.X += (float)kinectLocation.X;
                //tempPoint.Y += (float)kinectLocation.Y;
                //tempPoint.Z += (float)kinectLocation.Z;
                tempJoint.Position = tempPoint;
                adjSkel.Joints[(JointType)j] = tempJoint;
            }

            return adjSkel;
        }
        private Matrix3D findRotation(Vector3D u, Vector3D v)
        {
            Matrix3D rotationMatrix = new Matrix3D();
            Quaternion rotationQuat = new Quaternion();

            Vector3D cross = Vector3D.CrossProduct(u, v);
            rotationQuat.X = cross.X;
            rotationQuat.Y = cross.Y;
            rotationQuat.Z = cross.Z;
            rotationQuat.W = Math.Sqrt(u.LengthSquared * v.LengthSquared) + Vector3D.DotProduct(u, v);
            rotationQuat.Normalize();

            QuaternionRotation3D tempRotation = new QuaternionRotation3D(rotationQuat);
            RotateTransform3D tempTransform = new RotateTransform3D(tempRotation);
            rotationMatrix = tempTransform.Value;  //Going through RotateTransform3D is kind of a hacky way to do this...

            return rotationMatrix;
        }
        //private Vector3D transformAndConvert(SkeletonPoint position, Matrix3D rotation)
        //{
        //    Vector3D adjustedVector = new Vector3D(position.X, position.Y, position.Z);
        //    adjustedVector = Vector3D.Multiply(adjustedVector, rotation);
        //    return adjustedVector;
        //}
        private SkeletonPoint transform(SkeletonPoint position, Matrix3D rotation)
        {
            Point3D adjustedVector = new Point3D(position.X, position.Y, position.Z);
            adjustedVector = Point3D.Multiply(adjustedVector, rotation);
            SkeletonPoint adjustedPoint = new SkeletonPoint();
            adjustedPoint.X = (float)adjustedVector.X;
            adjustedPoint.Y = (float)adjustedVector.Y;
            adjustedPoint.Z = (float)adjustedVector.Z;
            return adjustedPoint;
        }
        #endregion

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