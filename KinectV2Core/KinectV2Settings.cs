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

        }

        public string uniqueKinectID { get; set; }
        public int kinectID { get; set; }
        public KinectVersion version
        {
            get { return KinectVersion.KinectV2; }
        }

        #region Skeleton and Physical Settings
        public bool mergeSkeletons { get; set; }
        public bool sendRawSkeletons { get; set; }
        public bool transformRawSkeletons { get; set; }
        public SkeletonSettings rawSkeletonSettings { get; set; }
        public Point3D kinectPosition { get; set; }
        public Quaternion kinectOrientation { get; set; }
        #endregion
    }
}