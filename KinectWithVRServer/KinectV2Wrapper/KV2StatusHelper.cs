using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectV2Core;

namespace KinectWithVRServer.KinectV2Wrapper
{
    //This class wraps the KinectV2StatusHelper class to delay the DLL loading and prevent a crash in the event that the KinectV2 DLL is missing
    public class StatusHelper
    {
        public event StatusEventHandler StatusChanged;
        private KinectV2StatusHelper realHelper;

        public StatusHelper()
        {
            realHelper = new KinectV2StatusHelper();
            realHelper.KinectV2StatusChanged += realHelper_KinectV2StatusChanged;
        }

        public static StatusEventArgs[] GetAllKinectsStatus()
        {
            KinectV2StatusEventArgs[] statusArray = KinectV2StatusHelper.GetAllKinectsStatus();
            StatusEventArgs[] statuses = new StatusEventArgs[statusArray.Length];

            for (int i = 0; i < statusArray.Length; i++)
            {
                statuses[i] = new StatusEventArgs();
                statuses[i].KinectNumber = statusArray[i].KinectNumber;
                statuses[i].Status = statusArray[i].Status;
                statuses[i].UniqueKinectID = statusArray[i].UniqueKinectID;
            }

            return statuses;
        }
        public static void StartKinectV2Service()
        {
            KinectV2StatusHelper.StartKinectV2Service();
        }
        public static void StopKinectV2Service()
        {
            KinectV2StatusHelper.StopKinectV2Service();
        }

        private void realHelper_KinectV2StatusChanged(object sender, KinectV2StatusEventArgs e)
        {
            if (StatusChanged != null)
            {
                StatusEventArgs args = new StatusEventArgs();
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
        public string UniqueKinectID;
        public int KinectNumber;  //This is the number in the SDK's list of Kinects
    }
}
