using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KinectV2Core;

namespace KinectWithVRServer.KinectV2Wrapper
{
    internal class KV2SdkTest
    {
        internal static bool IsSDKWorking()
        {
            bool SDKWorks = false;

            try
            {
                KinectV2StatusHelper.TestKinectSDK();
                SDKWorks = true;
            }
            catch { }

            return SDKWorks;
        }
    }
}
