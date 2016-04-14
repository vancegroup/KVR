using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KinectBase;

namespace NetworkKinectCore
{
    public class NetworkKinectSettings : KinectBase.IKinectSettings
    {
        //Constructors
        public NetworkKinectSettings() { } //Needed for serialization, do not use otherwise!
        public NetworkKinectSettings(string uniqueID, int kinectNumber)
        {
            uniqueKinectID = uniqueID;
            kinectID = kinectNumber;

            //Set any necessary default values here
            mergeSkeletons = false;
            kinectPosition = new Point3D(0.0, 0.0, 0.0);
            kinectYaw = 0.0;
            kinectRoll = 0.0;
            kinectPitch = 0.0;
        }

        //Properties required by the IKinectSettings interface
        public string uniqueKinectID { get; set; }
        public int kinectID { get; set; }
        public KinectVersion version
        {
            get { return KinectVersion.NetworkKinect; }
        }
        public bool mergeSkeletons { get; set; }

        #region Skeleton and Physical Settings
        public Point3D kinectPosition { get; set; }
        public double kinectYaw { get; set; }
        public double kinectPitch { get; set; }
        public double kinectRoll { get; set; }
        #endregion
    }
}
