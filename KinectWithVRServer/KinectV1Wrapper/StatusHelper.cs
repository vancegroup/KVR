using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectV1Core;

namespace KinectWithVRServer.KinectV1Wrapper
{
    //This class wraps the KinectV1CStatusHelper class to delay the DLL loading and prevent a crash in the event that the KinectV1 dll is missing
    public class StatusHelper
    {
        public event StatusEventHandler StatusChanged;
        private KinectV1StatusHelper realHelper;

        public StatusHelper()
        {
            realHelper = new KinectV1StatusHelper();
            realHelper.KinectV1StatusChanged += realHelper_KinectV1StatusChanged;
        }

        public static StatusEventArgs[] GetAllKinectsStatus()
        {
            KinectV1StatusEventArgs[] statusArray = KinectV1StatusHelper.GetAllKinectsStatus();
            StatusEventArgs[] statuses = new StatusEventArgs[statusArray.Length];

            for (int i = 0; i < statusArray.Length; i++)
            {
                statuses[i] = new StatusEventArgs();
                statuses[i].isXBox360Kinect = statusArray[i].isXBox360Kinect;
                statuses[i].KinectNumber = statusArray[i].KinectNumber;
                statuses[i].Status = statusArray[i].Status;
                statuses[i].UniqueKinectID = statusArray[i].UniqueKinectID;
            }

            return statuses;
        }

        private void realHelper_KinectV1StatusChanged(object sender, KinectV1StatusEventArgs e)
        {
            if (StatusChanged != null)
            {
                StatusEventArgs args = new StatusEventArgs();
                args.isXBox360Kinect = e.isXBox360Kinect;
                args.KinectNumber = e.KinectNumber;
                args.Status = e.Status;
                args.UniqueKinectID = e.UniqueKinectID;

                StatusChanged(this, args);
            }
        }
    }

    public delegate void StatusEventHandler(object sender, StatusEventArgs e);

    public class StatusEventArgs : EventArgs
    {
        public KinectBase.KinectStatus Status;
        //public string ConnectionID;
        public string UniqueKinectID;
        public int KinectNumber; //This is the number in the SDKs list of kinects
        public bool isXBox360Kinect;
    }
}
