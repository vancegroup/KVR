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
                if (kinect != null)
                {
                    return kinect.UniqueKinectId;
                }
                else
                {
                    return null;
                }
            }
        }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV2; }
        }
        public bool ColorStreamEnabled
        {
            //get { return isColorStreamOn; }
            get { return true; }
        }
        public bool DepthStreamEnabled
        {
            get { return isDepthStreamOn; }
        }

        KinectBase.MasterSettings masterSettings;
        internal KinectV2Settings masterKinectSettings;
        private bool isColorStreamOn = false;
        private bool isDepthStreamOn = false;
        private System.Timers.Timer updateTimer;
        public KinectBase.KinectSkeletonsData skeletonData;
        private Matrix3D skeletonTransformation = Matrix3D.Identity;
        private Quaternion skeletonRotQuaternion = Quaternion.Identity;
        private Vector4 lastAcceleration;
        private BodyFrameReader skeletonReader;  //TODO: The readers need to be disposed
        private DepthFrameReader depthReader;
        private ColorFrameReader colorReader;
        private InfraredFrameReader irReader;

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

            //TODO: Update this to open a specific Kinect v2, if the SDK is ever updated to support multiple on one machine
            kinect = KinectSensor.GetDefault();
            kinectID = kinectNumber;

            if (isGUILaunched)
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

            //Note: we don't close the Kinect here because it would remove it from the list of avaliable Kinects
        }
        public void StartKinectAudio()
        {
            
        }
        public KinectBase.KinectSkeleton TransformSkeleton(KinectBase.KinectSkeleton skeleton)
        {
            KinectBase.KinectSkeleton transformedSkeleton = new KinectBase.KinectSkeleton();
            transformedSkeleton.leftHandClosed = skeleton.leftHandClosed;
            transformedSkeleton.rightHandClosed = skeleton.rightHandClosed;
            transformedSkeleton.TrackingId = skeleton.TrackingId;
            transformedSkeleton.SkeletonTrackingState = skeleton.SkeletonTrackingState;
            transformedSkeleton.utcSampleTime = skeleton.utcSampleTime;
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
                    colorE.image = new byte[desc.LengthInPixels * colorE.bytesPerPixel];
                    frame.CopyConvertedFrameDataToArray(colorE.image, ColorImageFormat.Bgra);

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
                    depthE.image = new byte[desc.LengthInPixels * (depthE.perPixelExtra + depthE.bytesPerPixel)];
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

                    //Convert from Kinect v2 skeletons to KVR skeletons
                    KinectBase.KinectSkeleton[] kvrSkeletons = new KinectBase.KinectSkeleton[skelFrame.BodyCount];
                    for (int i = 0; i < skelFrame.BodyCount; i++)
                    {
                        kvrSkeletons[i] = new KinectBase.KinectSkeleton();
                        kvrSkeletons[i].Position = new Point3D(skeletons[i].Joints[JointType.SpineBase].Position.X, skeletons[i].Joints[JointType.SpineBase].Position.Y, skeletons[i].Joints[JointType.SpineBase].Position.Z);
                        kvrSkeletons[i].SkeletonTrackingState = convertTrackingState(skeletons[i].IsTracked);
                        kvrSkeletons[i].TrackingId = (int)skeletons[i].TrackingId;
                        kvrSkeletons[i].utcSampleTime = DateTime.UtcNow;
                        kvrSkeletons[i].sourceKinectID = kinectID;

                        for (int j = 0; j < Body.JointCount; j++)
                        {
                            KinectBase.Joint newJoint = new KinectBase.Joint();
                            newJoint.JointType = convertJointType(skeletons[i].Joints[(JointType)j].JointType);
                            newJoint.Position = convertJointPosition(skeletons[i].Joints[(JointType)j].Position);
                            newJoint.TrackingState = convertTrackingState(skeletons[i].Joints[(JointType)j].TrackingState);
                            newJoint.Orientation = convertJointOrientation(skeletons[i].JointOrientations[(JointType)j].Orientation);

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
            //throw new NotImplementedException();
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
                case JointType.SpineBase:
                {
                    return KinectBase.JointType.SpineBase;
                }
                case JointType.SpineMid:
                {
                    return KinectBase.JointType.SpineMid;
                }
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
                    //Other than the first 3, everything is numbered the same so we can just cast it
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
    }
}
