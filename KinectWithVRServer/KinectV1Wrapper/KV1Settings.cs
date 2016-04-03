using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;
using KinectV1Core;

namespace KinectWithVRServer.KinectV1Wrapper
{
    public class Settings : IKinectSettings
    {
        //Private variables to manage the wrapping
        private KinectV1Settings realSettings;

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
            realSettings = new KinectV1Settings(uniqueID, kinectNumber);
        }
        public Settings()  //Needed for serialization
        {
            realSettings = new KinectV1Settings();
        }

        //Public properties specific to the Kinect v1
        #region Color Settings
        public ColorImageFormat colorImageMode
        {
            get { return realSettings.colorImageMode; }
            set { realSettings.colorImageMode = value; }
        }
        public PowerLineFrequency lineFrequency
        {
            get { return realSettings.lineFrequency; }
            set { realSettings.lineFrequency = value; }
        }
        public bool autoWhiteBalance
        {
            get { return realSettings.autoWhiteBalance; }
            set { realSettings.autoWhiteBalance = value; }
        }
        public bool autoExposure
        {
            get { return realSettings.autoExposure; }
            set { realSettings.autoExposure = value; }
        }
        public BacklightCompensationMode backlightMode
        {
            get { return realSettings.backlightMode; }
            set { realSettings.backlightMode = value; }
        }
        public double Brightness
        {
            get { return realSettings.Brightness; }
            set { realSettings.Brightness = value; }
        }
        public double Contrast
        {
            get { return realSettings.Contrast; }
            set { realSettings.Contrast = value; }
        }
        public double ExposureTime
        {
            get { return realSettings.ExposureTime; }
            set { realSettings.ExposureTime = value; }
        }
        public double FrameInterval
        {
            get { return realSettings.FrameInterval; }
            set { realSettings.FrameInterval = value; }
        }
        public double Gain
        {
            get { return realSettings.Gain; }
            set { realSettings.Gain = value; }
        }
        public double Gamma
        {
            get { return realSettings.Gamma; }
            set { realSettings.Gamma = value; }
        }
        public double Hue
        {
            get { return realSettings.Hue; }
            set { realSettings.Hue = value; }
        }
        public double Saturation
        {
            get { return realSettings.Saturation; }
            set { realSettings.Saturation = value; }
        }
        public double Sharpness
        {
            get { return realSettings.Sharpness; }
            set { realSettings.Sharpness = value; }
        }
        public int WhiteBalance
        {
            get { return realSettings.WhiteBalance; }
            set { realSettings.WhiteBalance = value; }
        }
        #endregion
        #region Depth Settings        
        public DepthImageFormat depthImageMode
        {
            get { return realSettings.depthImageMode; }
            set { realSettings.depthImageMode = value; }
        }
        public bool isNearMode
        {
            get { return realSettings.isNearMode; }
            set { realSettings.isNearMode = value; }
        }
        public bool irON
        {
            get { return realSettings.irON; }
            set { realSettings.irON = value; }
        }
        #endregion
        #region Skeleton and Physical Settings
        //Note, mergeSkeletons is up above because it is required by the interface (it doesn't have to be up there, it's just how I sorted things)
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
        public bool sendAcceleration
        {
            get { return realSettings.sendAcceleration; }
            set { realSettings.sendAcceleration = value; }
        }
        public string accelerationServerName
        {
            get { return realSettings.accelerationServerName; }
            set { realSettings.accelerationServerName = value; }
        }
        public int accelXChannel
        {
            get { return realSettings.accelXChannel; }
            set { realSettings.accelXChannel = value; }
        }
        public int accelYChannel
        {
            get { return realSettings.accelYChannel; }
            set { realSettings.accelYChannel = value; }
        }
        public int accelZChannel
        {
            get { return realSettings.accelZChannel; }
            set { realSettings.accelZChannel = value; }
        }
        #endregion
        #region Audio Source Settings
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

        //Custom conversion operator so this class can be cast as a KinectV1Settings class
        public static explicit operator KinectV1Settings(Settings settings)
        {
            return settings.realSettings;
        }
    }
}
