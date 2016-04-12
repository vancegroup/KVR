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
        internal Matrix3D skeletonTransformation = Matrix3D.Identity;
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
            //TODO: Implement translating the Kinect v2 skeleton
            return skeleton;
        }
        public KinectBase.Joint TransformJoint(KinectBase.Joint joint)
        {
            //TODO: Implement translating the Kinect v2 joint
            return joint;
        }
        public Point MapJointToColor(KinectBase.Joint joint, bool undoTransform)
        {
            //TODO: Update this to actually do the transform
            return new Point();
        }
        public Point MapJointToDepth(KinectBase.Joint joint, bool undoTransform)
        {
            //TODO: Update this to actually do the transform
            return new Point();
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
                    colorE.timeStamp = frame.RelativeTime.Ticks; //TODO: Is there a better way to handle the timestamp?  How do we keep time stamps synced?
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

            }
        }
        void skeletonReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame skelFrame = e.FrameReference.AcquireFrame())
            {
                if (skelFrame != null)
                {
                    Body[] skeletons = new Body[6];
                    skelFrame.GetAndRefreshBodyData(skeletons);

                    KinectBase.SkeletonEventArgs skelE = new KinectBase.SkeletonEventArgs();

                    //TODO: Copy the skeleton data into the event args and fire the event
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

        private delegate void launchKinectDelegate();
    }
}
