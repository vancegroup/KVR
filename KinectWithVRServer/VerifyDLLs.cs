using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

using System.Diagnostics;

namespace KinectWithVRServer
{
    internal class VerifyDLLs
    {
        internal static bool Kinect1Avaliable()
        {
            bool isAvaliable = false;

            Assembly thisAssm = Assembly.GetExecutingAssembly();

            AssemblyName[] refs = thisAssm.GetReferencedAssemblies();
            for (int i = 0; i < refs.Length; i++)
            {
                if (refs[i].Name == "KinectV1Core")
                {
                    try
                    {
                        Assembly.Load(refs[i]);
                        isAvaliable = true;
                    }
                    catch { }
                }
            }

            //Secondary check to see if the DLL actually functions
            if (isAvaliable)
            {
                isAvaliable = KinectV1Wrapper.KV1SdkTest.IsSDKWorking();
            }
            return isAvaliable;
        }

        internal static bool Kinect2Avaliable()
        {
            bool isAvaliable = false;

            return isAvaliable;
        }

        internal static bool NetworkedKinectAvaliable()
        {
            bool isAvaliable = false;

            return isAvaliable;
        }
    }

    public class AvaliableDLLs
    {
        internal AvaliableDLLs() { }

        internal bool HasKinectV1 = false;
        internal bool HasKinectV2 = false;
        internal bool HasNetworkedKinect = false;
    }
}
