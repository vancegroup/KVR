using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectBase;

namespace KinectWithVRServer
{
    //TODO: The actual training and recognition parts of this need to happen in a seperate thread
    //If we try to run this all on the main thread, things will get bogged down, badly
    internal class GestureCore
    {
        internal event GestureRecognizedEventHandler GestureRecognizer;

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

        private void OnGestureRecognized(GestureRecognizedEventArgs e)
        {
            if (GestureRecognizer != null)
            {
                GestureRecognizer(this, e);
            }
        }
    }

    internal delegate void GestureRecognizedEventHandler(object sender, GestureRecognizedEventArgs e);
    internal class GestureRecognizedEventArgs
    {
        internal string GestureName { get; set; }
        internal DateTime UtcTime { get; set; }
    }
}
