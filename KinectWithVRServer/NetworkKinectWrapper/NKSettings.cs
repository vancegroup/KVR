using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;
using NetworkKinectCore;

namespace KinectWithVRServer.NetworkKinectWrapper
{
    class Settings : IKinectSettings
    {
        //Private variable to manage the wrapped class
        private NetworkKinectSettings realSettings;

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
            realSettings = new NetworkKinectSettings(uniqueID, kinectNumber);
        }
        public Settings()  //Needed for serialization
        {
            realSettings = new NetworkKinectSettings();
        }

        //Public properties specific to the network Kinect
        #region Skeleton and Physical Settings
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
        #endregion

        //Custom conversion operator so this class can be cast as a KinectV1Settings class
        public static explicit operator NetworkKinectSettings(Settings settings)
        {
            return settings.realSettings;
        }
    }
}
