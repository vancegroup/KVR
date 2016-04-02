using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectV1Core;

namespace KinectWithVRServer.KinectV1Wrapper
{
    internal class KV1SdkTest
    {
        internal static bool IsSDKWorking()
        {
            bool SDKWorks = false;

            try
            {
                KinectV1StatusEventArgs[] statuses = KinectV1StatusHelper.GetAllKinectsStatus();
                SDKWorks = true;
            }
            catch { }

            return SDKWorks;
        }
    }
}
