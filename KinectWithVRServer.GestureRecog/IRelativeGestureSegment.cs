using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;

namespace KinectWithVRServer
{
    public interface IRelativeGestureSegment
    {
        GesturePartResult CheckGesture(Skeleton skeleton);
    }
}
