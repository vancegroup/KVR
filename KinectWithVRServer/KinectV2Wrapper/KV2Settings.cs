using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;
using KinectV2Core;

namespace KinectWithVRServer.KinectV2Wrapper
{
    public class Settings : IKinectSettings
    {
        //Private variables to manage the wrapping
        private KinectV2Settings realSettings;

        //Public properties required by the interface
        public int kinectID
        {
            get { return realSettings.kinectID; }
            set { realSettings.kinectID = value; }
        }
        public KinectVersion version
        {
            get { return realSettings.version; }
        }
        public string uniqueKinectID
        {
            get { return realSettings.uniqueKinectID; }
            set { realSettings.uniqueKinectID = value; }
        }
        public bool mergeSkeletons
        {
            get { return realSettings.mergeSkeletons; }
            set { realSettings.mergeSkeletons = value; }
        }

        //Constructor to setup the real KinectV1Settings object
        public Settings(string uniqueID, int kinectNumber)
        {
            realSettings = new KinectV2Settings(uniqueID, kinectNumber);
        }
        public Settings()  //Needed for serialization
        {
            realSettings = new KinectV2Settings();
        }

        //Public properties specific to the Kinect v2
        #region Color Settings
        public bool useColorPreview
        {
            get { return realSettings.useColorPreview; }
            set { realSettings.useColorPreview = value; }
        }
        public bool useIRPreview
        {
            get { return realSettings.useIRPreview; }
            set { realSettings.useIRPreview = value; }
        }
        #endregion

        #region Depth Settings
        public bool scaleDepthToReliableRange
        {
            get { return realSettings.scaleDepthToReliableRange; }
            set { realSettings.scaleDepthToReliableRange = value; }
        }
        public bool colorizeDepth
        {
            get { return realSettings.colorizeDepth; }
            set { realSettings.colorizeDepth = value; }
        }
        #endregion

        #region Skeleton and Physical Settings
        //Note, mergeSkeleton is up above because it is required by the interface (it doesn't have to be up there, that's just how I sorted things)
        public bool sendRawSkeletons
        {
            get { return realSettings.sendRawSkeletons; }
            set { realSettings.sendRawSkeletons = value; }
        }
        public bool transformRawSkeletons
        {
            get { return realSettings.transformRawSkeletons; }
            set { realSettings.transformRawSkeletons = value; }
        }
        public SkeletonSettings rawSkeletonSettings
        {
            get { return realSettings.rawSkeletonSettings; }
            set { realSettings.rawSkeletonSettings = value; }
        }
        public System.Windows.Media.Media3D.Point3D kinectPosition
        {
            get { return realSettings.kinectPosition; }
            set { realSettings.kinectPosition = value; }
        }
        public double kinectYaw
        {
            get { return realSettings.kinectYaw; }
            set { realSettings.kinectYaw = value; }
        }
        public double kinectPitch
        {
            get { return realSettings.kinectPitch; }
            set { realSettings.kinectPitch = value; }
        }
        public double kinectRoll
        {
            get { return realSettings.kinectRoll; }
            set { realSettings.kinectRoll = value; }
        }
        //public System.Windows.Media.Media3D.Quaternion kinectOrientation
        //{
        //    get { return realSettings.kinectOrientation; }
        //    set { realSettings.kinectOrientation = value; }
        //}
        #endregion

        #region Audio Settings
        public bool sendAudioAngle
        {
            get { return realSettings.sendAudioAngle; }
            set { realSettings.sendAudioAngle = value; }
        }
        public AudioTrackingMode audioTrackMode
        {
            get { return realSettings.audioTrackMode; }
            set { realSettings.audioTrackMode = value; }
        }
        public int audioBeamTrackSkeletonNumber
        {
            get { return realSettings.audioBeamTrackSkeletonNumber; }
            set { realSettings.audioBeamTrackSkeletonNumber = value; }
        }
        public string audioAngleServerName
        {
            get { return realSettings.audioAngleServerName; }
            set { realSettings.audioAngleServerName = value; }
        }
        public int audioAngleChannel
        {
            get { return realSettings.audioAngleChannel; }
            set { realSettings.audioAngleChannel = value; }
        }
        #endregion

        //Custom conversion operator so this class can be cast as a KinectV2Settings class
        public static explicit operator KinectV2Settings(Settings settings)
        {
            return settings.realSettings;
        }
    }
}
