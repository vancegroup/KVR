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
using System.Runtime.InteropServices;
//using KinectBase;

namespace KinectV1Core
{
    public class KinectCoreV1 : KinectBase.IKinectCore
    {
        internal KinectSensor kinect;
        public int kinectID { get; set; }  //This is the index of the Kinect options in the Kinect settings list
        public string uniqueKinectID
        {
            get
            {
                if (kinect != null)
                {
                    //return kinect.UniqueKinectId;
                    return kinect.DeviceConnectionId;
                }
                else
                {
                    return null;
                }
            }
        }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV1; }
        }
        public bool ColorStreamEnabled
        {
            get { return isColorStreamOn; }
        }
        public bool DepthStreamEnabled
        {
            get { return isDepthStreamOn; }
        }

        internal KinectBase.MasterSettings masterSettings;
        internal KinectV1Settings masterKinectSettings;
        private InteractionStream interactStream;
        private System.Timers.Timer updateTimer;
        private List<HandGrabInfo> skeletonHandGrabData = new List<HandGrabInfo>();
        private Matrix3D skeletonTransformation = Matrix3D.Identity;
        private Quaternion skeletonRotQuaternion = Quaternion.Identity;  //TODO: This needs to be set to rotate the joint orientations appropriately
        //private Vector4 lastAcceleration;
        private bool isColorStreamOn = false;
        private bool isDepthStreamOn = false;
        internal bool? isXbox360Kinect = null;
        private bool isGUI = false;
        private System.IO.Stream audioStream = null;
        private KinectBase.ObjectPool<byte[]> colorImagePool;
        private KinectBase.ObjectPool<byte[]> depthImagePool;
        private bool accelFilterStarted = false;
        private KinectBase.Const3DFilter accelerationFilter = new KinectBase.Const3DFilter();

        //Event declarations
        public event KinectBase.SkeletonEventHandler SkeletonChanged;
        public event KinectBase.DepthFrameEventHandler DepthFrameReceived;
        public event KinectBase.ColorFrameEventHandler ColorFrameReceived;
        public event KinectBase.AudioPositionEventHandler AudioPositionChanged;
        public event KinectBase.AccelerationEventHandler AccelerationChanged;
        public event KinectBase.LogMessageEventHandler LogMessageGenerated;

        public KinectCoreV1(ref KinectBase.MasterSettings settings, bool isGUILaunched, int? kinectNumber = null)  //Why is the kinectNumber nullable if it is requried later?
        {
            if (kinectNumber != null)
            {
                masterSettings = settings;
                dynamic tempSettings = masterSettings.kinectOptionsList[(int)kinectNumber];  //We have to use dynamic because the type of the Kinect settings in the master list isn't defined until runtime
                masterKinectSettings = (KinectV1Settings)tempSettings;

                //Initialize the object pools
                colorImagePool = new KinectBase.ObjectPool<byte[]>(() => new byte[640 * 480 * 4]);
                depthImagePool = new KinectBase.ObjectPool<byte[]>(() => new byte[640 * 480 * 4]);

                //Note: the kinectNumber could be greater than the number of Kinect v1s if there are other types of sensors in use
                //Therefore, we have to find the correct Kinect, if it exists using this loop
                int globalIndex = -1;
                for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
                {
                    if (KinectSensor.KinectSensors[i].DeviceConnectionId == masterSettings.kinectOptionsList[(int)kinectNumber].uniqueKinectID)
                    {
                        globalIndex = i;
                        break;
                    }
                }
                if (globalIndex >= 0)
                {
                    kinect = KinectSensor.KinectSensors[globalIndex];
                    kinectID = (int)kinectNumber;
                }
                else
                {
                    throw new System.IndexOutOfRangeException("Specified Kinect sensor does not exist");
                }

                if (isGUILaunched)
                {
                    isGUI = true;
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
                //TODO: Open the default Kinect?
                throw new NullReferenceException("To create a KinectCore object, the KinectNumber must be valid.");
            }
        }
        public void ShutdownSensor()
        {
            if (kinect != null)
            {
                lock (kinect)
                {
                    //The "new" syntax is sort of odd, but these really do remove the handlers from the specified events
                    kinect.ColorFrameReady -= kinect_ColorFrameReady;
                    kinect.DepthFrameReady -= kinect_DepthFrameReady;
                    kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;
                    interactStream.InteractionFrameReady -= interactStream_InteractionFrameReady;
                    if (updateTimer != null)
                    {
                        updateTimer.Stop();
                        updateTimer.Elapsed -= updateTimer_Elapsed;
                        updateTimer.Dispose();
                    }

                    interactStream.Dispose();
                    interactStream = null;

                    if (kinect.AudioSource != null)
                    {
                        if (audioStream != null)
                        {
                            audioStream.Close();
                            audioStream.Dispose();
                        }

                        kinect.AudioSource.Stop();
                    }

                    kinect.ColorStream.Disable();
                    kinect.DepthStream.Disable();
                    kinect.SkeletonStream.Disable();

                    kinect.Stop();
                    kinect.Dispose();
                    kinect = null;
                }
            }
        }
        public void StartKinectAudio()
        {
            if (isGUI)
            {
                ActuallyStartAudio();
            }
            else
            {
                //Launch the audio on a seperate thread if it is in console mode (otherwise the events never get thrown successfully)
                startAudioDelegate audioDelegate = ActuallyStartAudio;
                IAsyncResult result = audioDelegate.BeginInvoke(null, null);
                audioDelegate.EndInvoke(result);
            }
        }
        private void ActuallyStartAudio()
        {
            if (kinect.IsRunning)
            {
                //Start the audio streams, if necessary -- NOTE: This must be after the skeleton stream is started (which it should be here)
                if (masterKinectSettings.sendAudioAngle || masterSettings.audioOptions.sourceID == kinectID)
                {
                    if (masterSettings.audioOptions.sourceID == kinectID)
                    {
                        kinect.AudioSource.EchoCancellationMode = (EchoCancellationMode)masterSettings.audioOptions.echoMode;
                        kinect.AudioSource.AutomaticGainControlEnabled = masterSettings.audioOptions.autoGainEnabled;
                        kinect.AudioSource.NoiseSuppression = masterSettings.audioOptions.noiseSurpression;
                        if (masterKinectSettings.sendAudioAngle)
                        {
                            if (masterKinectSettings.audioTrackMode != KinectBase.AudioTrackingMode.Loudest)
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
                    else if (masterKinectSettings.sendAudioAngle)
                    {
                        kinect.AudioSource.EchoCancellationMode = Microsoft.Kinect.EchoCancellationMode.None;
                        kinect.AudioSource.AutomaticGainControlEnabled = false;
                        kinect.AudioSource.NoiseSuppression = true;
                        if (masterKinectSettings.audioTrackMode != KinectBase.AudioTrackingMode.Loudest)
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Manual;
                        }
                        else
                        {
                            kinect.AudioSource.BeamAngleMode = BeamAngleMode.Automatic;
                        }
                        kinect.AudioSource.SoundSourceAngleChanged += AudioSource_SoundSourceAngleChanged;
                    }

                    audioStream = kinect.AudioSource.Start();
                }
            }
        }
        public System.IO.Stream GetKinectAudioStream()
        {
            if (kinect.AudioSource != null)
            {
                return audioStream;
            }
            else
            {
                return null;
            }
        }

        internal void ChangeColorResolution(KinectBase.ColorImageFormat newResolution)
        {
            kinect.ColorStream.Disable();
            if (newResolution != KinectBase.ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(convertColorImageFormat(newResolution));

                //Get the size, in bytes, of the new image array and reset the image pool
                int size = 0;
                if (newResolution == KinectBase.ColorImageFormat.InfraredResolution640x480Fps30)
                {
                    size = 640 * 480 * 2;
                }
                else if (newResolution == KinectBase.ColorImageFormat.RawBayerResolution1280x960Fps12 || newResolution == KinectBase.ColorImageFormat.RgbResolution1280x960Fps12)
                {
                    size = 1280 * 960 * 4;
                }
                else
                {
                    size = 640 * 480 * 4;
                }
                colorImagePool.ResetPool(() => new byte[size]);
                
                isColorStreamOn = true;
            }
            else
            {
                isColorStreamOn = false;
            }
        }
        internal void ChangeDepthResolution(KinectBase.DepthImageFormat newResolution)
        {
            kinect.DepthStream.Disable();
            if (newResolution != KinectBase.DepthImageFormat.Undefined)
            {
                kinect.DepthStream.Enable(convertDepthImageFormat(newResolution));

                //Get the size, in bytes, of the new image array and reset the image pool
                int size = 0;
                if (newResolution == KinectBase.DepthImageFormat.Resolution640x480Fps30)
                {
                    size = 640 * 480 * 4;
                }
                else if (newResolution == KinectBase.DepthImageFormat.Resolution320x240Fps30)
                {
                    size = 320 * 240 * 4;
                }
                else
                {
                    size = 80 * 60 * 4;
                }
                depthImagePool.ResetPool(() => new byte[size]);

                isDepthStreamOn = true;
            }
            else
            {
                isDepthStreamOn = false;
            }
        }
        public void UpdateAudioAngle(Point3D position)
        {
            if (kinect.AudioSource != null)
            {
                //Calculate and set the audio angle, in degrees, that we want the Kinect to listen to
                double angle = Math.Atan2(position.X - masterKinectSettings.kinectPosition.X, position.Z - masterKinectSettings.kinectPosition.Z) * (180.0 / Math.PI);
                kinect.AudioSource.ManualBeamAngle = angle; //This will be rounded automatically to the nearest 10 degree increment, in the range -50 to 50 degrees
            }
        }
        public KinectBase.KinectSkeleton TransformSkeleton(KinectBase.KinectSkeleton skeleton)
        {
            KinectBase.KinectSkeleton transformedSkeleton = new KinectBase.KinectSkeleton();
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
        public KinectBase.Joint TransformJoint(KinectBase.Joint joint)
        {
            KinectBase.Joint transformedJoint = new KinectBase.Joint();
            transformedJoint.Confidence = joint.Confidence;
            transformedJoint.JointType = joint.JointType;
            transformedJoint.TrackingState = joint.TrackingState;
            transformedJoint.Orientation = skeletonRotQuaternion * joint.Orientation;
            transformedJoint.Position = skeletonTransformation.Transform(joint.Position);
            transformedJoint.utcTime = joint.utcTime;

            return transformedJoint;
        }
        public Point MapJointToColor(KinectBase.Joint joint, bool undoTransform)
        {
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            SkeletonPoint skelPoint = new SkeletonPoint();
            skelPoint.X = (float)transformedPosition.X;
            skelPoint.Y = (float)transformedPosition.Y;
            skelPoint.Z = (float)transformedPosition.Z;
            ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skelPoint, kinect.ColorStream.Format);
            mappedPoint.X = point.X;
            mappedPoint.Y = point.Y;

            return mappedPoint;
        }
        public Point MapJointToDepth(KinectBase.Joint joint, bool undoTransform)
        {
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            SkeletonPoint skelPoint = new SkeletonPoint();
            skelPoint.X = (float)transformedPosition.X;
            skelPoint.Y = (float)transformedPosition.Y;
            skelPoint.Z = (float)transformedPosition.Z;
            DepthImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToDepthPoint(skelPoint, kinect.DepthStream.Format);
            mappedPoint.X = point.X;
            mappedPoint.Y = point.Y;

            return mappedPoint;
        }

        //TODO: Ensure that all initial Kinect settings (like white balance, etc) get set on the actual Kinect for both GUI and console mode
        private void LaunchKinect()
        {
            //Setup default properties
            if (masterKinectSettings.colorImageMode != KinectBase.ColorImageFormat.Undefined)
            {
                kinect.ColorStream.Enable(convertColorImageFormat(masterKinectSettings.colorImageMode));
                kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                isColorStreamOn = true;

                //Check to see if the Kinect is a Kinect for Windows or a Xbox 360 Kinect so options can be enabled accordingly
                try
                {
                    ColorCameraSettings test = kinect.ColorStream.CameraSettings;
                    test = null;
                    isXbox360Kinect = false;
                }
                catch
                {
                    isXbox360Kinect = true;
                }
            }
            if (masterKinectSettings.depthImageMode != KinectBase.DepthImageFormat.Undefined)
            {
                //kinect.DepthStream.Enable();
                kinect.DepthStream.Enable(convertDepthImageFormat(masterKinectSettings.depthImageMode));
                isDepthStreamOn = true;

                kinect.SkeletonStream.Enable(); //Note, the audio stream MUST be started AFTER this (known issue with SDK v1.7).  Currently not an issue as the audio isn't started until the server is launched later in the code.
                kinect.SkeletonStream.EnableTrackingInNearRange = true; //Explicitly enable depth tracking in near mode (this can be true when the depth mode is near or default, but if it is false, there is no skeleton data in near mode)
                
                //Create the skeleton data container
                if (skeletonHandGrabData == null)
                {
                    skeletonHandGrabData = new List<HandGrabInfo>();
                }
                else
                {
                    skeletonHandGrabData.Clear();
                }
                
                interactStream = new InteractionStream(kinect, new DummyInteractionClient());
                kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);
                kinect.SkeletonStream.EnableTrackingInNearRange = true;
                interactStream.InteractionFrameReady += new EventHandler<InteractionFrameReadyEventArgs>(interactStream_InteractionFrameReady);
            }

            kinect.Start();

            StartUpdateTimer();
        }
        private void StartUpdateTimer()
        {
            updateTimer = new System.Timers.Timer();
            updateTimer.AutoReset = true;
            updateTimer.Interval = 33.333;
            updateTimer.Elapsed += updateTimer_Elapsed;
            updateTimer.Start();
        }
        //Updates the acceleration on the GUI and the server, 30 FPS may be a little fast for the GUI, but for VRPN, it probably needs to be at least that fast
        private void updateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //Update the acceleration data
            bool dataValid = false;
            Vector4? acceleration = null;
            int? elevationAngle = null;
            lock (kinect)
            {
                if (kinect != null && kinect.IsRunning)
                {
                    //I wish these try/catch statements weren't necessary, but these two calls seemed to fail often
                    dataValid = true;
                    try
                    {
                        acceleration = kinect.AccelerometerGetCurrentReading();
                    }
                    catch
                    {
                        acceleration = null;
                        dataValid = false;
                    }

                    if (dataValid)  //We can't even try to calculate the elevation angle if the accelerometer doesn't read right
                    {
                        try
                        {
                            elevationAngle = kinect.ElevationAngle;
                        }
                        catch
                        {
                            elevationAngle = null;
                            dataValid = false;
                        }
                    }
                }
            }

            //Update the GUI
            if (dataValid)
            {
                //Update the filtered acceleration
                EigenWrapper.Matrix accelMat = new EigenWrapper.Matrix(3, 1);
                accelMat[0, 0] = acceleration.Value.X;
                accelMat[1, 0] = acceleration.Value.Y;
                accelMat[2, 0] = acceleration.Value.Z;
                accelerationFilter.IntegrateMeasurement(accelMat, DateTime.UtcNow, 0.01);
                accelFilterStarted = true;
                //lastAcceleration = acceleration.Value;
                //Transmits the acceleration data using an event
                KinectBase.AccelerationEventArgs accelE = new KinectBase.AccelerationEventArgs();
                accelE.kinectID = this.kinectID;
                accelE.acceleration = new Vector3D(acceleration.Value.X, acceleration.Value.Y, acceleration.Value.Z);
                accelE.elevationAngle = elevationAngle.Value;
                OnAccelerationChanged(accelE);
            }
            else
            {
                KinectBase.AccelerationEventArgs accelE = new KinectBase.AccelerationEventArgs();
                accelE.kinectID = this.kinectID;

                //Send the acceleration, if it is valid
                if (acceleration.HasValue)
                {                
                    //Update the filtered acceleration
                    EigenWrapper.Matrix accelMat = new EigenWrapper.Matrix(3, 1);
                    accelMat[0, 0] = acceleration.Value.X;
                    accelMat[1, 0] = acceleration.Value.Y;
                    accelMat[2, 0] = acceleration.Value.Z;
                    accelerationFilter.IntegrateMeasurement(accelMat, DateTime.UtcNow, 0.01);
                    accelFilterStarted = true;
                    //lastAcceleration = acceleration.Value;
                    accelE.acceleration = new Vector3D(acceleration.Value.X, acceleration.Value.Y, acceleration.Value.Z);
                }
                else
                {
                    accelE.acceleration = null;
                }

                //Send the Kinect angle if it is valid
                if (elevationAngle.HasValue)
                {
                    accelE.elevationAngle = elevationAngle.Value;
                }
                else
                {
                    accelE.elevationAngle = null;
                }

                OnAccelerationChanged(accelE);
            }
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skelFrame = e.OpenSkeletonFrame())
            {
                if (skelFrame != null && masterSettings.kinectOptionsList.Count > kinectID && (masterKinectSettings.mergeSkeletons || masterKinectSettings.sendRawSkeletons))
                {
                    DateTime now = DateTime.UtcNow;
                    Skeleton[] skeletons = new Skeleton[skelFrame.SkeletonArrayLength];
                    skelFrame.CopySkeletonDataTo(skeletons);


                    EigenWrapper.Matrix predAccel = new EigenWrapper.Matrix(3, 1);
                    predAccel[2, 0] = 1;
                    if (accelFilterStarted)
                    {
                        predAccel = accelerationFilter.PredictAndDiscard(0);
                    }

                    if (interactStream != null)
                    {
                        Vector4 filteredAccel = new Vector4();
                        filteredAccel.W = 0;
                        filteredAccel.X = (float)predAccel[0, 0];
                        filteredAccel.Y = (float)predAccel[1, 0];
                        filteredAccel.Z = (float)predAccel[2, 0];
                        interactStream.ProcessSkeleton(skeletons, filteredAccel, skelFrame.Timestamp);

                        System.Diagnostics.Trace.WriteLine("[" + filteredAccel.X + ", " + filteredAccel.Y + ", " + filteredAccel.Z + "]");
                    }

                    //Generate the transformation matrix for the skeletons
                    double kinectYaw = masterKinectSettings.kinectYaw;
                    Point3D kinectPosition = masterKinectSettings.kinectPosition;
                    Matrix3D gravityBasedKinectRotation = findRotation(new Vector3D(predAccel[0, 0], predAccel[1, 0], predAccel[2, 0]), new Vector3D(0, -1, 0));
                    AxisAngleRotation3D yawRotation = new AxisAngleRotation3D(new Vector3D(0, 1, 0), -kinectYaw);
                    RotateTransform3D tempTrans = new RotateTransform3D(yawRotation);
                    TranslateTransform3D transTrans = new TranslateTransform3D((Vector3D)kinectPosition);
                    Matrix3D masterMatrix = Matrix3D.Multiply(Matrix3D.Multiply(tempTrans.Value, gravityBasedKinectRotation), transTrans.Value);
                    skeletonTransformation = masterMatrix;

                    //Convert from Kinect v1 skeletons to KVR skeletons
                    KinectBase.KinectSkeleton[] kvrSkeletons = new KinectBase.KinectSkeleton[skeletons.Length];
                    for (int i = 0; i < kvrSkeletons.Length; i++)
                    {
                        //Set the tracking ID numbers for the hand grab data
                        int grabID = -1;
                        for (int j = 0; j < skeletonHandGrabData.Count; j++)
                        {
                            if (skeletonHandGrabData[j].skeletonTrackingID == skeletons[i].TrackingId)
                            {
                                grabID = j;
                                break;
                            }
                        }
                        if (grabID < 0)
                        {
                            skeletonHandGrabData.Add(new HandGrabInfo(skeletons[i].TrackingId));
                            grabID = skeletonHandGrabData.Count - 1;
                        }

                        kvrSkeletons[i] = new KinectBase.KinectSkeleton();
                        kvrSkeletons[i].Position = new Point3D(skeletons[i].Position.X, skeletons[i].Position.Y, skeletons[i].Position.Z);
                        kvrSkeletons[i].SkeletonTrackingState = convertTrackingState(skeletons[i].TrackingState);
                        kvrSkeletons[i].TrackingId = skeletons[i].TrackingId;
                        //kvrSkeletons[i].utcSampleTime = DateTime.UtcNow;
                        kvrSkeletons[i].sourceKinectID = kinectID;

                        for (int j = 0; j < skeletons[i].Joints.Count; j++)
                        {
                            KinectBase.Joint newJoint = new KinectBase.Joint();
                            newJoint.Confidence = KinectBase.TrackingConfidence.Unknown; //The Kinect 1 doesn't support the confidence property
                            newJoint.JointType = convertJointType(skeletons[i].Joints[(JointType)j].JointType);
                            Vector4 tempQuat = skeletons[i].BoneOrientations[(JointType)j].AbsoluteRotation.Quaternion;
                            newJoint.Orientation = new Quaternion(tempQuat.X, tempQuat.Y, tempQuat.Z, tempQuat.W);
                            SkeletonPoint tempPos = skeletons[i].Joints[(JointType)j].Position;
                            newJoint.Position = new Point3D(tempPos.X, tempPos.Y, tempPos.Z);
                            newJoint.TrackingState = convertTrackingState(skeletons[i].Joints[(JointType)j].TrackingState);
                            newJoint.utcTime = now;
                            kvrSkeletons[i].skeleton[newJoint.JointType] = newJoint; //Skeleton doesn't need to be initialized because it is done in the KinectSkeleton constructor
                        }

                        //Get the hand states from the hand grab data array
                        kvrSkeletons[i].rightHandClosed = skeletonHandGrabData[grabID].rightHandClosed;
                        kvrSkeletons[i].leftHandClosed = skeletonHandGrabData[grabID].leftHandClosed;
                    }

                    //Add the skeleton data to the event handler and throw the event
                    KinectBase.SkeletonEventArgs skelE = new KinectBase.SkeletonEventArgs();
                    skelE.skeletons = kvrSkeletons;
                    skelE.kinectID = kinectID;

                    OnSkeletonChanged(skelE);
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
                    if (interactStream != null && frame.Format == DepthImageFormat.Resolution640x480Fps30)
                    {
                        interactStream.ProcessDepth(frame.GetRawPixelData(), frame.Timestamp);
                    }

                    KinectBase.DepthFrameEventArgs depthE = new KinectBase.DepthFrameEventArgs();
                    depthE.kinectID = this.kinectID;
                    depthE.perPixelExtra = 2;
                    depthE.width = frame.Width;
                    depthE.height = frame.Height;
                    depthE.bytesPerPixel = frame.BytesPerPixel;
                    depthE.reliableMin = (float)frame.MinDepth / (float)ushort.MaxValue;
                    depthE.reliableMax = (float)frame.MaxDepth / (float)ushort.MaxValue;
                    depthE.timeStamp = new TimeSpan(frame.Timestamp * 10000);  //Convert from milliseconds to ticks and set the time span

                    //The second 2 bytes of the DepthImagePixel structure hold the actual depth as a uint16, so lets get those, and put the data in the blue and green channel of the image
                    //depthE.image = new byte[frame.PixelDataLength * (depthE.perPixelExtra + depthE.bytesPerPixel)];
                    depthE.image = depthImagePool.GetObject();  //Get an image array from the object pool
                    if (depthE.image.Length != frame.PixelDataLength * (depthE.perPixelExtra + depthE.bytesPerPixel))  //If the object is the wrong size, replace it with one that is the right size
                    {
                        depthE.image = new byte[frame.PixelDataLength * (depthE.perPixelExtra + depthE.bytesPerPixel)];
                    }
                    unsafe
                    {
                        //The sizeof() operation is unsafe in this instance, otherwise this would all be safe code
                        IntPtr depthImagePtr = Marshal.AllocHGlobal(sizeof(DepthImagePixel) * frame.PixelDataLength);
                        frame.CopyDepthImagePixelDataTo(depthImagePtr, frame.PixelDataLength);
                        Marshal.Copy(depthImagePtr, depthE.image, 2, depthE.image.Length - 2);
                        Marshal.FreeHGlobal(depthImagePtr);
                    }

                    OnDepthFrameReceived(depthE);
                }
            }
        }
        private void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    KinectBase.ColorFrameEventArgs colorE = new KinectBase.ColorFrameEventArgs();
                    colorE.kinectID = this.kinectID;
                    if (frame.Format == ColorImageFormat.InfraredResolution640x480Fps30)
                    {
                        colorE.pixelFormat = PixelFormats.Gray16;
                        colorE.isIR = true;
                    }
                    else
                    {
                        colorE.pixelFormat = PixelFormats.Bgr32;
                        colorE.isIR = false;
                    }
                    colorE.width = frame.Width;
                    colorE.height = frame.Height;
                    colorE.bytesPerPixel = frame.BytesPerPixel;
                    colorE.timeStamp = new TimeSpan(frame.Timestamp * 10000);  //Convert from milliseconds to ticks and set the time span
                    //colorE.image = new byte[frame.PixelDataLength];
                    colorE.image = colorImagePool.GetObject();  //Get an array from the image pool
                    if (colorE.image.Length != frame.PixelDataLength)  //If the image array is the wrong size, create a new one (it will get cycled into the pool on its own later)
                    {
                        colorE.image = new byte[frame.PixelDataLength];
                    }
                    frame.CopyPixelDataTo(colorE.image);
                    OnColorFrameReceived(colorE);
                }
            }
        }
        private void interactStream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (InteractionFrame interactFrame = e.OpenInteractionFrame())
            {
                if (interactFrame != null && masterKinectSettings.mergeSkeletons)
                {
                    UserInfo[] tempUserInfo = new UserInfo[6];
                    interactFrame.CopyInteractionDataTo(tempUserInfo);

                    foreach (UserInfo interactionInfo in tempUserInfo)
                    {
                        foreach (InteractionHandPointer hand in interactionInfo.HandPointers)
                        {
                            if (hand.HandEventType == InteractionHandEventType.Grip)
                            {
                                for (int i = 0; i < skeletonHandGrabData.Count; i++)
                                {
                                    if (skeletonHandGrabData[i].skeletonTrackingID == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonHandGrabData[i].leftHandClosed = true;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonHandGrabData[i].rightHandClosed = true;
                                        }
                                        break;
                                    }
                                }
                            }
                            else if (hand.HandEventType == InteractionHandEventType.GripRelease)
                            {
                                for (int i = 0; i < skeletonHandGrabData.Count; i++)
                                {
                                    if (skeletonHandGrabData[i].skeletonTrackingID == interactionInfo.SkeletonTrackingId)
                                    {
                                        if (hand.HandType == InteractionHandType.Left)
                                        {
                                            skeletonHandGrabData[i].leftHandClosed = false;
                                        }
                                        else if (hand.HandType == InteractionHandType.Right)
                                        {
                                            skeletonHandGrabData[i].rightHandClosed = false;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        private void AudioSource_SoundSourceAngleChanged(object sender, SoundSourceAngleChangedEventArgs e)
        {
            KinectBase.AudioPositionEventArgs audioE = new KinectBase.AudioPositionEventArgs();
            audioE.kinectID = this.kinectID;
            audioE.audioAngle = e.Angle;
            audioE.confidence = e.ConfidenceLevel;

            OnAudioPositionChanged(audioE);
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

        //Misc Methods
        private KinectBase.TrackingState convertTrackingState(SkeletonTrackingState trackingState)
        {
            if (trackingState == SkeletonTrackingState.PositionOnly)
            {
                //The position only state is out of order, so we have to set it manually
                return KinectBase.TrackingState.PositionOnly;
            }
            else
            {
                //All the rest are numbered the same, so we can do a direct cast
                return (KinectBase.TrackingState)trackingState;
            }
        }
        private KinectBase.TrackingState convertTrackingState(JointTrackingState trackingState)
        {
            //These both have the tracking states numbered the same, so we can do a straight cast
            return (KinectBase.TrackingState)trackingState;
        }
        private KinectBase.JointType convertJointType(JointType jointType)
        {
            //The joint types are all numbered the same for the Kinect v1, so we can just do a straight cast
            return (KinectBase.JointType)jointType;
        }
        private ColorImageFormat convertColorImageFormat(KinectBase.ColorImageFormat format)
        {
            //The color formats are all numbered the same for the Kienct v1, so we can do a straight cast
            return (ColorImageFormat)format;
        }
        private DepthImageFormat convertDepthImageFormat(KinectBase.DepthImageFormat format)
        {
            //The depth formats are all numbered the same for the Kienct v1, so we can do a straight cast
            return (DepthImageFormat)format;
        }

        //Methods to fire the events
        protected virtual void OnSkeletonChanged(KinectBase.SkeletonEventArgs e)
        {
            if (SkeletonChanged != null)
            {
                SkeletonChanged(this, e);
            }
        }
        protected virtual void OnDepthFrameReceived(KinectBase.DepthFrameEventArgs e)
        {
            if (DepthFrameReceived != null)
            {
                DepthFrameReceived(this, e);
            }
        }
        protected virtual void OnColorFrameReceived(KinectBase.ColorFrameEventArgs e)
        {
            if (ColorFrameReceived != null)
            {
                ColorFrameReceived(this, e);
            }
        }
        protected virtual void OnAudioPositionChanged(KinectBase.AudioPositionEventArgs e)
        {
            if (AudioPositionChanged != null)
            {
                AudioPositionChanged(this, e);
            }
        }
        protected virtual void OnAccelerationChanged(KinectBase.AccelerationEventArgs e)
        {
            if (AccelerationChanged != null)
            {
                AccelerationChanged(this, e);
            }
        }
        protected virtual void OnLogMessageGenerated(KinectBase.LogMessageEventArgs e)
        {
            if (LogMessageGenerated != null)
            {
                LogMessageGenerated(this, e);
            }
        }

        private delegate void launchKinectDelegate();
        private delegate void startAudioDelegate();
    }

    internal class HandGrabInfo
    {
        internal HandGrabInfo(int trackingID)
        {
            skeletonTrackingID = trackingID;
        }

        internal int skeletonTrackingID;
        internal bool rightHandClosed = false;
        internal bool leftHandClosed = false;
    }

    //This dummy class is required to get the hand grab information from the Kinect
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