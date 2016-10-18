using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Microsoft.Kinect;
using System.Runtime.InteropServices;

namespace KinectV2Core
{
    public class KinectCoreV2 : KinectBase.IKinectCore
    {
        internal KinectSensor kinect;
        public int kinectID { get; set; }
        public string uniqueKinectID
        {
            get
            {
                if (foundID)
                {
                    return uniqueID;
                }
                else
                {
                    if (kinect != null)
                    {
                        uniqueID = string.Copy(kinect.UniqueKinectId);
                        if (uniqueID.Length > 0)
                        {
                            foundID = true;
                            return uniqueID;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV2; }
        }
        public bool ColorStreamEnabled
        {
            //There is no option to disable the color stream on the Kinect 2
            get { return true; }
        }
        public bool DepthStreamEnabled
        {
            //There is no option to disable the depth stream on the Kinect 2
            get { return true; }
        }

        KinectBase.MasterSettings masterSettings;
        internal KinectV2Settings masterKinectSettings;
        public KinectBase.KinectSkeletonsData skeletonData;
        private Matrix3D skeletonTransformation = Matrix3D.Identity;
        private Quaternion skeletonRotQuaternion = Quaternion.Identity;
        private BodyFrameReader skeletonReader;
        private DepthFrameReader depthReader;
        private ColorFrameReader colorReader;
        private InfraredFrameReader irReader;
        private AudioBeamFrameReader audioReader;
        private string uniqueID = "";
        private bool foundID = false;
        private bool isGUI = false;
        private System.IO.Stream audioStream = null;
        private KinectBase.ObjectPool<byte[]> colorImagePool;
        private KinectBase.ObjectPool<byte[]> depthImagePool;
        private KinectBase.ObjectPool<byte[]> irImagePool;

        //Event declarations
        public event KinectBase.SkeletonEventHandler SkeletonChanged;
        public event KinectBase.DepthFrameEventHandler DepthFrameReceived;
        public event KinectBase.ColorFrameEventHandler ColorFrameReceived;
        public event KinectBase.AudioPositionEventHandler AudioPositionChanged;
        public event KinectBase.AccelerationEventHandler AccelerationChanged;
        public event KinectBase.LogMessageEventHandler LogMessageGenerated;

        public KinectCoreV2(ref KinectBase.MasterSettings settings, bool isGUILaunched, int kinectNumber)
        {
            masterSettings = settings;
            dynamic temp = masterSettings.kinectOptionsList[kinectNumber];
            masterKinectSettings = (KinectV2Settings)temp;

            //TODO: Update this to open a specific Kinect v2, if the SDK is ever updated to support multiple on one machine
            kinect = KinectSensor.GetDefault();
            kinectID = kinectNumber;

            uint tempC = kinect.ColorFrameSource.FrameDescription.LengthInPixels;
            uint tempD = kinect.DepthFrameSource.FrameDescription.LengthInPixels;
            uint tempI = kinect.InfraredFrameSource.FrameDescription.LengthInPixels;
            colorImagePool = new KinectBase.ObjectPool<byte[]>(() => new byte[tempC * 4]);
            depthImagePool = new KinectBase.ObjectPool<byte[]>(() => new byte[tempD * 4]);
            irImagePool = new KinectBase.ObjectPool<byte[]>(() => new byte[tempI * sizeof(UInt16)]);

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
        ~KinectCoreV2()
        {
            //Dispose all the objects if the shutdown sensor methdo didn't get called somehow
            if (skeletonReader != null)
            {
                skeletonReader.FrameArrived -= skeletonReader_FrameArrived;
                skeletonReader.Dispose();
                skeletonReader = null;
            }
            if (depthReader != null)
            {
                depthReader.FrameArrived -= depthReader_FrameArrived;
                depthReader.Dispose();
                depthReader = null;
            }
            if (colorReader != null)
            {
                colorReader.FrameArrived -= colorReader_FrameArrived;
                colorReader.Dispose();
                colorReader = null;
            }
            if (irReader != null)
            {
                irReader.FrameArrived -= irReader_FrameArrived;
                irReader.Dispose();
                irReader = null;
            }
            if (audioStream != null)
            {
                audioStream.Close();
                audioStream.Dispose();
                audioStream = null;
            }
            if (audioReader != null)
            {
                audioReader.FrameArrived -= audioReader_FrameArrived;
                audioReader.Dispose();
                audioReader = null;
            }
        }
        public void ShutdownSensor()
        {
            if (skeletonReader != null)
            {
                skeletonReader.FrameArrived -= skeletonReader_FrameArrived;
                skeletonReader.Dispose();
                skeletonReader = null;
            }
            if (depthReader != null)
            {
                depthReader.FrameArrived -= depthReader_FrameArrived;
                depthReader.Dispose();
                depthReader = null;
            }
            if (colorReader != null)
            {
                colorReader.FrameArrived -= colorReader_FrameArrived;
                colorReader.Dispose();
                colorReader = null;
            }
            if (irReader != null)
            {
                irReader.FrameArrived -= irReader_FrameArrived;
                irReader.Dispose();
                irReader = null;
            }
            if (audioStream != null)
            {
                audioStream.Close();
                audioStream.Dispose();
                audioStream = null;
            }
            if (audioReader != null)
            {
                audioReader.FrameArrived -= audioReader_FrameArrived;
                audioReader.Dispose();
                audioReader = null;
            }

            //Note: we don't close the Kinect here because it would remove it from the list of avaliable Kinects
        }
        public void StartKinectAudio()
        {
            if (isGUI)
            {
                ActuallyStartAudio();
            }
            else
            {
                //Launch the audio on a seperate thread if it is in console mode
                startAudioDelegate audioDelegate = ActuallyStartAudio;
                IAsyncResult result = audioDelegate.BeginInvoke(null, null);
                audioDelegate.EndInvoke(result);
            }
        }
        private void ActuallyStartAudio()
        {
            if (kinect.IsAvailable)
            {
                //Start the audio stream if necessary
                if (masterKinectSettings.sendAudioAngle || masterSettings.audioOptions.sourceID == kinectID)
                {
                    audioReader = kinect.AudioSource.OpenReader();
                    audioReader.FrameArrived += audioReader_FrameArrived;

                    if (masterKinectSettings.audioTrackMode != KinectBase.AudioTrackingMode.Loudest)
                    {
                        for (int i = 0; i < kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinect.AudioSource.AudioBeams[i].AudioBeamMode = AudioBeamMode.Manual;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinect.AudioSource.AudioBeams[i].AudioBeamMode = AudioBeamMode.Manual;
                        }
                    }

                    if (kinect.AudioSource.AudioBeams.Count > 0)
                    {
                        audioStream = kinect.AudioSource.AudioBeams[0].OpenInputStream();
                    }
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
            //TODO: Update this so it takes a joint array instead of a single joint (this is supposed to be more efficient for the Kinect 2)
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            //Setup the Kinect v2 objects to do the transformation
            CameraSpacePoint[] skelPoints = new CameraSpacePoint[1];
            skelPoints[0] = new CameraSpacePoint();
            skelPoints[0].X = (float)transformedPosition.X;
            skelPoints[0].Y = (float)transformedPosition.Y;
            skelPoints[0].Z = (float)transformedPosition.Z;
            ColorSpacePoint[] points = new ColorSpacePoint[1];
            kinect.CoordinateMapper.MapCameraPointsToColorSpace(skelPoints, points);

            //Convert back to the base object type
            mappedPoint.X = points[0].X;
            mappedPoint.Y = points[0].Y;

            return mappedPoint;
        }
        public Point MapJointToDepth(KinectBase.Joint joint, bool undoTransform)
        {
            //TODO: Update this so it takes a joint array instead of a single joint (this is supposed to be more efficient for the Kinect 2)
            Point mappedPoint = new Point(0, 0);
            Point3D transformedPosition = joint.Position;

            if (undoTransform)
            {
                Matrix3D inverseTransform = skeletonTransformation;
                inverseTransform.Invert();
                transformedPosition = inverseTransform.Transform(transformedPosition);
            }

            //Setup the Kinect v2 objects to do the transformation
            CameraSpacePoint[] skelPoints = new CameraSpacePoint[1];
            skelPoints[0] = new CameraSpacePoint();
            skelPoints[0].X = (float)transformedPosition.X;
            skelPoints[0].Y = (float)transformedPosition.Y;
            skelPoints[0].Z = (float)transformedPosition.Z;
            DepthSpacePoint[] points = new DepthSpacePoint[1];
            kinect.CoordinateMapper.MapCameraPointsToDepthSpace(skelPoints, points);

            //Convert back to the base object type
            mappedPoint.X = points[0].X;
            mappedPoint.Y = points[0].Y;

            return mappedPoint;
        }
        public void UpdateAudioAngle(Point3D position)
        {
            if (kinect.AudioSource != null)
            {
                for (int i = 0; i < kinect.AudioSource.AudioBeams.Count; i++)
                {
                    if (kinect.AudioSource.AudioBeams[i].AudioBeamMode == AudioBeamMode.Manual)
                    {
                        //Calculate and set the audio angle, in radians, that we want to Kinect to listen to
                        kinect.AudioSource.AudioBeams[i].BeamAngle = (float)Math.Atan2(position.X - masterKinectSettings.kinectPosition.X, position.Z - masterKinectSettings.kinectPosition.Z);
                    }
                }
            }
        }

        private void LaunchKinect()
        {
            //TODO: Update this Kinect v2 launch method to support loaded options

            //Note: The Kinect.Open function is not called here because it has to be opened previously to show up on the list of avaliable Kinects

            //Setup the skeleton reader
            skeletonReader = kinect.BodyFrameSource.OpenReader();
            skeletonReader.FrameArrived += skeletonReader_FrameArrived;

            //Setup the depth reader
            depthReader = kinect.DepthFrameSource.OpenReader();
            depthReader.FrameArrived += depthReader_FrameArrived;

            //Setup the color reader
            colorReader = kinect.ColorFrameSource.OpenReader();
            colorReader.FrameArrived += colorReader_FrameArrived;

            //Setup the ir reader
            irReader = kinect.InfraredFrameSource.OpenReader();
            irReader.FrameArrived += irReader_FrameArrived;
        }

        void colorReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription desc = frame.FrameDescription;

                    KinectBase.ColorFrameEventArgs colorE = new KinectBase.ColorFrameEventArgs();
                    colorE.bytesPerPixel = 4; //This is fixed to 4 because we are converting to bgra below)
                    colorE.pixelFormat = PixelFormats.Bgra32;
                    colorE.height = desc.Height;
                    colorE.width = desc.Width;
                    colorE.kinectID = kinectID;
                    colorE.timeStamp = frame.RelativeTime;
                    colorE.isIR = false;
                    colorE.image = colorImagePool.GetObject();
                    //colorE.image = new byte[desc.LengthInPixels * colorE.bytesPerPixel];
                    //frame.CopyConvertedFrameDataToArray(colorE.image, ColorImageFormat.Bgra);
                    unsafe
                    {
                        fixed (byte* ptr = colorE.image)
                        {
                            frame.CopyConvertedFrameDataToIntPtr((IntPtr)ptr, desc.LengthInPixels * sizeof(byte) * 4, ColorImageFormat.Bgra);
                        }
                    }

                    OnColorFrameReceived(colorE);
                }
            }
        }
        void depthReader_FrameArrived(object sender, DepthFrameArrivedEventArgs e)
        {
            using (DepthFrame depthFrame = e.FrameReference.AcquireFrame())
            {
                if (depthFrame != null)
                {
                    FrameDescription desc = depthFrame.FrameDescription;

                    KinectBase.DepthFrameEventArgs depthE = new KinectBase.DepthFrameEventArgs();
                    depthE.bytesPerPixel = 2;  //This is fixed to 2 because we are using a ushort to hold the depth image
                    depthE.perPixelExtra = 2;  //We always have an extra two bytes per pixel because we are storing a Gray16 in a bgr32 format
                    depthE.height = desc.Height;
                    depthE.width = desc.Width;
                    depthE.kinectID = kinectID;
                    depthE.timeStamp = depthFrame.RelativeTime;
                    depthE.reliableMin = (float)depthFrame.DepthMinReliableDistance / (float)ushort.MaxValue;
                    depthE.reliableMax = (float)depthFrame.DepthMaxReliableDistance / (float)ushort.MaxValue;

                    //Get all the data for the depth, and store the bytes for the Gray16 in the blue and green channels of a bgr32
                    IntPtr depthImagePtr = Marshal.AllocHGlobal((int)(depthE.bytesPerPixel * desc.LengthInPixels));
                    depthFrame.CopyFrameDataToIntPtr(depthImagePtr, (uint)depthE.bytesPerPixel * desc.LengthInPixels);
                    //depthE.image = new byte[desc.LengthInPixels * (depthE.perPixelExtra + depthE.bytesPerPixel)];
                    depthE.image = depthImagePool.GetObject();
                    unsafe
                    {
                        fixed (byte* pDst = depthE.image)
                        {
                            ushort* pD = (ushort*)pDst;
                            ushort* pS = (ushort*)depthImagePtr.ToPointer();

                            for (int n = 0; n < desc.LengthInPixels; n++)
                            {
                                *pD = *pS;
                                pD += 2;
                                pS++;
                            }
                        }
                    }
                    Marshal.FreeHGlobal(depthImagePtr);

                    OnDepthFrameReceived(depthE);
                }
            }
        }
        void skeletonReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame skelFrame = e.FrameReference.AcquireFrame())
            {
                if (skelFrame != null)
                {
                    Body[] skeletons = new Body[skelFrame.BodyCount];
                    skelFrame.GetAndRefreshBodyData(skeletons);
                    DateTime now = DateTime.UtcNow;

                    //Convert from Kinect v2 skeletons to KVR skeletons
                    KinectBase.KinectSkeleton[] kvrSkeletons = new KinectBase.KinectSkeleton[skelFrame.BodyCount];
                    for (int i = 0; i < skelFrame.BodyCount; i++)
                    {
                        kvrSkeletons[i] = new KinectBase.KinectSkeleton();
                        kvrSkeletons[i].Position = new Point3D(skeletons[i].Joints[JointType.SpineBase].Position.X, skeletons[i].Joints[JointType.SpineBase].Position.Y, skeletons[i].Joints[JointType.SpineBase].Position.Z);
                        kvrSkeletons[i].SkeletonTrackingState = convertTrackingState(skeletons[i].IsTracked);
                        kvrSkeletons[i].TrackingId = (int)skeletons[i].TrackingId;
                        //kvrSkeletons[i].utcSampleTime = DateTime.UtcNow;
                        kvrSkeletons[i].sourceKinectID = kinectID;

                        for (int j = 0; j < Body.JointCount; j++)
                        {
                            KinectBase.Joint newJoint = new KinectBase.Joint();
                            newJoint.JointType = convertJointType(skeletons[i].Joints[(JointType)j].JointType);
                            newJoint.Position = convertJointPosition(skeletons[i].Joints[(JointType)j].Position);
                            newJoint.TrackingState = convertTrackingState(skeletons[i].Joints[(JointType)j].TrackingState);
                            newJoint.Orientation = convertJointOrientation(skeletons[i].JointOrientations[(JointType)j].Orientation);
                            newJoint.utcTime = now;

                            //Tracking confidence only exists for the hand states, so set those and leave the rest as unknown
                            if (newJoint.JointType == KinectBase.JointType.HandLeft)
                            {
                                newJoint.Confidence = convertTrackingConfidence(skeletons[i].HandLeftConfidence);
                            }
                            else if (newJoint.JointType == KinectBase.JointType.HandRight)
                            {
                                newJoint.Confidence = convertTrackingConfidence(skeletons[i].HandRightConfidence);
                            }
                            else
                            {
                                newJoint.Confidence = KinectBase.TrackingConfidence.Unknown;
                            }

                            kvrSkeletons[i].skeleton[newJoint.JointType] = newJoint;
                        }

                        kvrSkeletons[i].rightHandClosed = convertHandState(skeletons[i].HandRightState);
                        kvrSkeletons[i].leftHandClosed = convertHandState(skeletons[i].HandLeftState);
                    }

                    //Add the skeleton data to the event handler and throw the event
                    KinectBase.SkeletonEventArgs skelE = new KinectBase.SkeletonEventArgs();
                    skelE.skeletons = kvrSkeletons;
                    skelE.kinectID = kinectID;

                    OnSkeletonChanged(skelE);
                }
            }
        }
        void irReader_FrameArrived(object sender, InfraredFrameArrivedEventArgs e)
        {
            using (InfraredFrame frame = e.FrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    FrameDescription desc = frame.FrameDescription;

                    KinectBase.ColorFrameEventArgs irE = new KinectBase.ColorFrameEventArgs();
                    irE.bytesPerPixel = (int)desc.BytesPerPixel;
                    irE.pixelFormat = PixelFormats.Gray16;
                    irE.height = desc.Height;
                    irE.width = desc.Width;
                    irE.kinectID = kinectID;
                    irE.timeStamp = frame.RelativeTime;
                    irE.isIR = true;
                    //irE.image = new byte[desc.LengthInPixels * sizeof(UInt16)];
                    irE.image = irImagePool.GetObject();
                    unsafe
                    {
                        fixed (byte* ptr = irE.image)
                        {
                            frame.CopyFrameDataToIntPtr((IntPtr)ptr, desc.LengthInPixels * sizeof(UInt16));
                        }
                    }

                    OnColorFrameReceived(irE);
                }
            }
        }
        void audioReader_FrameArrived(object sender, AudioBeamFrameArrivedEventArgs e)
        {
            using (AudioBeamFrameList frames = e.FrameReference.AcquireBeamFrames())
            {
                if (frames != null)
                {
                    for (int i = 0; i < frames.Count; i++)
                    {
                        KinectBase.AudioPositionEventArgs args = new KinectBase.AudioPositionEventArgs();
                        args.audioAngle = frames[i].AudioBeam.BeamAngle * (180.0 / Math.PI);  //Convert from radians to degress
                        args.confidence = frames[i].AudioBeam.BeamAngleConfidence;
                        args.kinectID = kinectID;
                        OnAudioPositionChanged(args);
                    }
                }
            }
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

            //Put the image array back in the image pool
            depthImagePool.PutObject(e.image);
        }
        protected virtual void OnColorFrameReceived(KinectBase.ColorFrameEventArgs e)
        {
            if (ColorFrameReceived != null)
            {
                ColorFrameReceived(this, e);
            }

            //Put the object back in the image pool
            if (e.isIR)
            {
                irImagePool.PutObject(e.image);
            }
            else
            {
                colorImagePool.PutObject(e.image);
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

        //Misc methods
        private KinectBase.TrackingState convertTrackingState(bool trackingState)
        {
            if (trackingState)
            {
                return KinectBase.TrackingState.Tracked;
            }
            else
            {
                return KinectBase.TrackingState.NotTracked;
            }
        }
        private KinectBase.TrackingState convertTrackingState(TrackingState trackingState)
        {
            //Both enums are numbered the same, so we can do a straight cast
            return (KinectBase.TrackingState)trackingState;
        }
        private KinectBase.TrackingConfidence convertTrackingConfidence(TrackingConfidence confidence)
        {
            //The enums are numbered the same, so we can do a straight cast
            return (KinectBase.TrackingConfidence)confidence;
        }
        private KinectBase.JointType convertJointType(JointType jointType)
        {
            switch (jointType)
            {
                //case JointType.SpineBase:
                //{
                //    return KinectBase.JointType.SpineBase;
                //}
                //case JointType.SpineMid:
                //{
                //    return KinectBase.JointType.SpineMid;
                //}
                case JointType.Neck:
                {
                    return KinectBase.JointType.Neck;
                }
                case JointType.SpineShoulder:
                {
                    return KinectBase.JointType.SpineShoulder;
                }
                default:
                {
                    //Other than the neck and spine shoulder, everything is numbered the same so we can just cast it
                    return (KinectBase.JointType)jointType;
                }
            }
        }
        private Point3D convertJointPosition(CameraSpacePoint position)
        {
            return new Point3D(position.X, position.Y, position.Z);
        }
        private Quaternion convertJointOrientation(Vector4 orientation)
        {
            return new Quaternion(orientation.X, orientation.Y, orientation.Z, orientation.W);
        }
        private bool convertHandState(HandState handState)
        {
            if (handState == HandState.Closed)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private delegate void launchKinectDelegate();
        private delegate void startAudioDelegate();
    }
}
