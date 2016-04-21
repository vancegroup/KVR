using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    public interface IKinectCore
    {
        string uniqueKinectID
        {
            get;
        }
        int kinectID { get; set; }
        KinectVersion version { get; }

        bool ColorStreamEnabled { get; }
        bool DepthStreamEnabled { get; }

        void ShutdownSensor();
        KinectSkeleton TransformSkeleton(KinectSkeleton skeleton);
        Joint TransformJoint(Joint joint);
        System.Windows.Point MapJointToColor(Joint joint, bool undoTransform);
        System.Windows.Point MapJointToDepth(Joint joint, bool undoTransform);

        //All versions of the Kinect core need to implement these events to pass the data up to the GUI
        event SkeletonEventHandler SkeletonChanged;
        event DepthFrameEventHandler DepthFrameReceived;
        event ColorFrameEventHandler ColorFrameReceived;
        event AccelerationEventHandler AccelerationChanged;
        event AudioPositionEventHandler AudioPositionChanged;
        event LogMessageEventHandler LogMessageGenerated;
    }

    public class KinectCoreComparer : IComparer<IKinectCore>
    {
        public int Compare(IKinectCore x, IKinectCore y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }

    public delegate void SkeletonEventHandler(object sender, SkeletonEventArgs e);
    public delegate void DepthFrameEventHandler(object sender, DepthFrameEventArgs e);
    public delegate void ColorFrameEventHandler(object sender, ColorFrameEventArgs e);
    public delegate void AccelerationEventHandler(object sender, AccelerationEventArgs e);
    public delegate void AudioPositionEventHandler(object sender, AudioPositionEventArgs e);
    public delegate void LogMessageEventHandler(object sender, LogMessageEventArgs e);

    public class SkeletonEventArgs : EventArgs
    {
        public KinectSkeleton[] skeletons;
        public int kinectID;
    }
    public class DepthFrameEventArgs : EventArgs
    {
        public byte[] image;
        public int perPixelExtra;
        //public System.Windows.Media.PixelFormat pixelFormat;
        public int width;
        public int height;
        public int bytesPerPixel;
        public TimeSpan timeStamp;
        public int kinectID;
        public float reliableMin; //This should be from 0 to 1
        public float reliableMax; //This hsould be from 0 to 1
    }
    public class ColorFrameEventArgs : EventArgs
    {
        public byte[] image;
        public System.Windows.Media.PixelFormat pixelFormat;
        public bool isIR;
        public int width;
        public int height;
        public int bytesPerPixel;
        public TimeSpan timeStamp;
        public int kinectID;
    }
    public class AccelerationEventArgs : EventArgs
    {
        public System.Windows.Media.Media3D.Vector3D? acceleration;
        public int? elevationAngle;
        public int kinectID;
    }
    public class AudioPositionEventArgs : EventArgs
    {
        public double audioAngle;
        public double confidence;
        public int kinectID;
    }
    public class LogMessageEventArgs : EventArgs
    {
        public string errorMessage;
        public bool verboseMessage; //If this is true, the message should only be displayed when the system is in verbose mode
        public int kinectID;
    }
}
