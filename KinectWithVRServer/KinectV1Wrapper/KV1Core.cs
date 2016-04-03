using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;
using KinectV1Core;

namespace KinectWithVRServer.KinectV1Wrapper
{
    //This class wraps the KinectV1Core class to delay the DLL loading and prevent a crash in the event that the KinectV1 dll is missing
    class Core : IKinectCore
    {
        //Private variables to manage the wrapping
        private KinectCoreV1 realCore;

        //Public properties required by the interface
        public string uniqueKinectID
        {
            get { return realCore.uniqueKinectID; } 
        }
        public int kinectID
        {
            get { return realCore.kinectID; }
            set { realCore.kinectID = value; }
        }
        public KinectVersion version
        {
            get { return realCore.version; }
        }
        public bool ColorStreamEnabled
        {
            get { return realCore.ColorStreamEnabled; }
        }
        public bool DepthStreamEnabled
        {
            get { return realCore.DepthStreamEnabled; }
        }

        //Public methods required by the interface
        public void ShutdownSensor()
        {
            realCore.ShutdownSensor();
        }
        public KinectSkeleton TransformSkeleton(KinectSkeleton skeleton)
        {
            return realCore.TransformSkeleton(skeleton);
        }
        public Joint TransformJoint(Joint joint)
        {
            return realCore.TransformJoint(joint);
        }
        public System.Windows.Point MapJointToColor(Joint joint, bool undoTransform)
        {
            return realCore.MapJointToColor(joint, undoTransform);
        }
        public System.Windows.Point MapJointToDepth(Joint joint, bool undoTransform)
        {
            return realCore.MapJointToDepth(joint, undoTransform);
        }

        //Events required by the interface
        public event SkeletonEventHandler SkeletonChanged;
        public event DepthFrameEventHandler DepthFrameReceived;
        public event ColorFrameEventHandler ColorFrameReceived;
        public event AccelerationEventHandler AccelerationChanged;
        public event AudioPositionEventHandler AudioPositionChanged;
        public event LogMessageEventHandler LogMessageGenerated;

        //Constructor to setup the real KinectV1Core object
        public Core(ref MasterSettings settings, bool isGUILaunched, int? kinectNumber = null)
        {
            realCore = new KinectCoreV1(ref settings, isGUILaunched, kinectNumber);

            //Subscribe to the events so they can be forwarded
            realCore.SkeletonChanged += realCore_SkeletonChanged;
            realCore.DepthFrameReceived += realCore_DepthFrameReceived;
            realCore.ColorFrameReceived += realCore_ColorFrameReceived;
            realCore.AccelerationChanged += realCore_AccelerationChanged;
            realCore.AudioPositionChanged += realCore_AudioPositionChanged;
            realCore.LogMessageGenerated += realCore_LogMessageGenerated;
        }

        //Forward all the events
        private void realCore_SkeletonChanged(object sender, SkeletonEventArgs e)
        {
            if (SkeletonChanged != null)
            {
                SkeletonChanged(this, e);
            }
        }
        private void realCore_DepthFrameReceived(object sender, DepthFrameEventArgs e)
        {
            if (DepthFrameReceived != null)
            {
                DepthFrameReceived(this, e);
            }
        }
        private void realCore_ColorFrameReceived(object sender, ColorFrameEventArgs e)
        {
            if (ColorFrameReceived != null)
            {
                ColorFrameReceived(this, e);
            }
        }
        private void realCore_AccelerationChanged(object sender, AccelerationEventArgs e)
        {
            if (AccelerationChanged != null)
            {
                AccelerationChanged(this, e);
            }
        }
        private void realCore_AudioPositionChanged(object sender, AudioPositionEventArgs e)
        {
            if (AudioPositionChanged != null)
            {
                AudioPositionChanged(this, e);
            }
        }
        private void realCore_LogMessageGenerated(object sender, LogMessageEventArgs e)
        {
            if (LogMessageGenerated != null)
            {
                LogMessageGenerated(this, e);
            }
        }

        //Kinect v1 specific methods
        public void StartKinectAudio()
        {
            realCore.StartKinectAudio();
        }
        public System.IO.Stream GetKinectAudioStream()
        {
            return realCore.GetKinectAudioStream();
        }
        public void UpdateAudioAngle(System.Windows.Media.Media3D.Point3D position)
        {
            realCore.UpdateAudioAngle(position);
        }

        //Custom conversion operator so this class can be cast as a KinectV1Settings class
        public static explicit operator KinectCoreV1(Core kinectCore)
        {
            return kinectCore.realCore;
        }
    }
}
