using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
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

            isGUI = isGUILaunched;
            //NOTE: unlike the physical Kinect sensors, network Kinect sensors are not launched on the creation of the core
        }

        //Methods required by the IKinectCore interface
        public void ShutdownSensor()
        {

        }
        public KinectSkeleton TransformSkeleton(KinectSkeleton skeleton)
        {
            KinectSkeleton transformedSkeleton = new KinectSkeleton();
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
        public Joint TransformJoint(Joint joint)
        {
            Joint transformedJoint = new Joint();
            transformedJoint.Confidence = joint.Confidence;
            transformedJoint.JointType = joint.JointType;
            transformedJoint.TrackingState = joint.TrackingState;
            transformedJoint.Orientation = skeletonRotQuaternion * joint.Orientation;
            transformedJoint.Position = skeletonTransformation.Transform(joint.Position);

            return transformedJoint;
        }
        public System.Windows.Point MapJointToColor(Joint joint, bool undoTransform)
        {
            return new System.Windows.Point(0, 0);
        }
        public System.Windows.Point MapJointToDepth(Joint joint, bool undoTransform)
        {
            return new System.Windows.Point(0, 0);
        }

        //Methods specific to the network kinect
        public bool StartNetworkKinect()
        {
            bool success = false;



            return success;
        }
    }
}
