using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    public interface IKinectSkeletonControl
    {
        int? kinectID { get; }
        KinectVersion version { get; }
        string uniqueKinectID { get; }
    }
}
