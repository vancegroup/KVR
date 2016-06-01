using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;

namespace KinectWithVRServer
{
    internal class GestureCore
    {
        internal event EventHandler GestureRecognizer;

        internal GestureCore()
        {

        }

        internal void TrainGesture(List<KinectSkeleton> trainingData)
        {

        }

        internal void UpdateRecognizer(KinectSkeleton latestSkeleton)
        {

        }

        internal void StartRecognizer()
        {

        }

        internal void StopRecognizer()
        {

        }

        internal void AddGesture()
        {

        }

        private void OnGestureRecognized(EventArgs e)
        {
            if (GestureRecognizer != null)
            {
                GestureRecognizer(this, e);
            }
        }
    }

    internal class GestureRecognizedEventArgs
    {
        string GestureName { get; set; }
        DateTime UtcTime { get; set; }
    }
}
