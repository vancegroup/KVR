using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using KinectBase;

namespace KinectV2Core
{
    public class KinectV2Settings : IKinectSettings
    {
        public KinectV2Settings() { } //Needed for serialization
        public KinectV2Settings(string deviceUniqueID, int kinectNumber)
        {
            uniqueKinectID = deviceUniqueID;
            kinectID = kinectNumber;

            //Set the default values
            mergeSkeletons = true;
            sendRawSkeletons = false;
            transformRawSkeletons = false;
            kinectPosition = new Point3D(0, 0, 0);
            scaleDepthToReliableRange = true;
            colorizeDepth = false;
            sendAudioAngle = false;
            audioTrackMode = AudioTrackingMode.Loudest;            

            //Setup the options for the raw skeleton, irrespective of use
            rawSkeletonSettings = new SkeletonSettings();
        }

        public string uniqueKinectID { get; set; }
        public int kinectID { get; set; }
        public KinectVersion version
        {
            get { return KinectVersion.KinectV2; }
        }

        #region Color Settings
        private bool isColorPreview = true;
        public bool useColorPreview
        {
            get
            {
                return isColorPreview;
            }
            set
            {
                isColorPreview = value;
            }
        }
        public bool useIRPreview
        {
            get
            {
                return !isColorPreview;
            }
            set
            {
                isColorPreview = !value;
            }
        }
        #endregion

        #region Depth Settings
        public bool scaleDepthToReliableRange { get; set; }
        public bool colorizeDepth { get; set; }
        #endregion

        #region Skeleton and Physical Settings
        public bool mergeSkeletons { get; set; }
        public bool sendRawSkeletons { get; set; }
        public bool transformRawSkeletons { get; set; }
        public SkeletonSettings rawSkeletonSettings { get; set; }
        public Point3D kinectPosition { get; set; }
        private double yaw = 0;
        private double pitch = 0;
        private double roll = 0;
        public double kinectYaw
        {
            get { return yaw; }
            set { yaw = value; }
        }
        public double kinectPitch
        {
            get { return pitch; }
            set { pitch = value; }
        }
        public double kinectRoll
        {
            get { return roll; }
            set { roll = value; }
        }
        //public Quaternion kinectOrientation 
        //{
        //    get
        //    {
        //        //TODO: Calculate the orientation quaternion from the Euler angles
        //        return Quaternion.Identity;
        //    }
        //}
        #endregion

        #region Audio Settings
        public bool sendAudioAngle { get; set; }
        public AudioTrackingMode audioTrackMode { get; set; }
        public int audioBeamTrackSkeletonNumber { get; set; }
        public string audioAngleServerName { get; set; }
        public int audioAngleChannel { get; set; }
        #endregion
    }
}