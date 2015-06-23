using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    public interface IKinectSettings
    {
        //All the Kinect cores need these properties so we can figure out what's what
        int kinectID { get; set; }
        KinectVersion version { get; }
        string uniqueKinectID { get; set; }
        bool mergeSkeletons { get; set; }
    }

    public class KinectSettingsComparer : IComparer<IKinectSettings>
    {
        public int Compare(IKinectSettings x, IKinectSettings y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }
}
