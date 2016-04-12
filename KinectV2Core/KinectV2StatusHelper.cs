using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectV2Core
{
    public class KinectV2StatusHelper
    {
        public event KinectV2StatusEventHandler KinectV2StatusChanged;

        public KinectV2StatusHelper()
        {
            KinectSensor.GetDefault().IsAvailableChanged += KinectV2StatusHelper_IsAvailableChanged;
        }

        public static void TestKinectSDK()
        {
            KinectSensor tempSensor = KinectSensor.GetDefault();
        }

        //This is really goofy, but apparently the Kinect needs to be open before it can be avaliable
        public static bool StartKinectV2Service()
        {
            KinectSensor tempKinect = KinectSensor.GetDefault();
            tempKinect.Open();

            bool started = false;
            int timeoutCount = 0;
            while (!started && timeoutCount < 20)
            {
                if (tempKinect.IsAvailable)
                {
                    started = true;
                }
                System.Threading.Thread.Sleep(50);
                timeoutCount++;
            }
            

            return started;
        }

        public static void StopKinectV2Service()
        {
            KinectSensor tempKinect = KinectSensor.GetDefault();
            tempKinect.Close();
        }

        public static KinectV2StatusEventArgs[] GetAllKinectsStatus()
        {
            //For the time being, this will always be an array with one element because of the one Kinect v2 per computer limit
            KinectV2StatusEventArgs[] statusArray = new KinectV2StatusEventArgs[1];

            for (int i = 0; i < 1; i++)
            {
                KinectV2StatusEventArgs temp = new KinectV2StatusEventArgs();
                temp.KinectNumber = i;
                KinectSensor tempKinect = KinectSensor.GetDefault();
                
                //It looks like the service can be opened at any time, and only becomes available if there is actually a Kinect attached to the computer
                temp.UniqueKinectID = tempKinect.UniqueKinectId;
                if (tempKinect.IsAvailable)
                {
                    temp.Status = KinectBase.KinectStatus.Connected;
                    System.Diagnostics.Debug.WriteLine("Kinect 2 connected.");
                }
                else
                {
                    temp.Status = KinectBase.KinectStatus.Disconnected;
                    System.Diagnostics.Debug.WriteLine("Kinect 2 disconnected.");
                }
                statusArray[i] = temp;
            }

            return statusArray;
        }

        void KinectV2StatusHelper_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (KinectV2StatusChanged != null)  //Only do this is there are subscribers to the event
            {
                KinectV2StatusEventArgs args = new KinectV2StatusEventArgs();
                if (e.IsAvailable)
                {
                    args.Status = KinectBase.KinectStatus.Connected;
                    System.Diagnostics.Debug.WriteLine("Kinect 2 connected.");
                }
                else
                {
                    //TODO: Is this the correct status to give if the Kinect v2 isn't avaliable??
                    args.Status = KinectBase.KinectStatus.Disconnected;
                }
                args.KinectNumber = 0; //This is always 0 because the Kinect v2 only supports 1 Kinect
                args.UniqueKinectID = KinectSensor.GetDefault().UniqueKinectId;
                System.Diagnostics.Debug.WriteLine("Kinect 2 disconnected.");

                OnKinectV2StatusChanged(args);
            }
        }

        protected virtual void OnKinectV2StatusChanged(KinectV2StatusEventArgs e)
        {
            if (KinectV2StatusChanged != null)
            {
                KinectV2StatusChanged(this, e);
            }
        }
    }

    public delegate void KinectV2StatusEventHandler(object sender, KinectV2StatusEventArgs e);

    public class KinectV2StatusEventArgs : EventArgs
    {
        public KinectBase.KinectStatus Status;
        public string UniqueKinectID;
        public int KinectNumber; //This is the number in the SDKs list of kinects
    }
}
