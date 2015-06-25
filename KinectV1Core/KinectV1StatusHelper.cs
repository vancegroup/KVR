using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectV1Core
{
    public class KinectV1StatusHelper
    {
        public event KinectV1StatusEventHandler KinectV1StatusChanged;

        public KinectV1StatusHelper()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
        }

        public static KinectV1StatusEventArgs[] GetAllKinectsStatus()
        {
            KinectV1StatusEventArgs[] statusArray = new KinectV1StatusEventArgs[KinectSensor.KinectSensors.Count];

            for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
            {
                KinectV1StatusEventArgs temp = new KinectV1StatusEventArgs();
                temp.KinectNumber = i;
                //temp.ConnectionID = KinectSensor.KinectSensors[i].DeviceConnectionId;
                temp.Status = (KinectBase.KinectStatus)KinectSensor.KinectSensors[i].Status;
                temp.UniqueKinectID = KinectSensor.KinectSensors[i].UniqueKinectId;

                //Test each Kinect to see if it is an XBox 360 Kinect
                bool isXbox360Kinect = false;
                try
                {
                    ColorCameraSettings test = KinectSensor.KinectSensors[i].ColorStream.CameraSettings;
                    test = null;
                    isXbox360Kinect = false;
                }
                catch
                {
                    isXbox360Kinect = true;
                }
                temp.isXBox360Kinect = isXbox360Kinect;

                statusArray[i] = temp;
            }

            return statusArray;
        }

        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (KinectV1StatusChanged != null)  //Okay, maybe it isn't that much work, but we may as well not do it if we don't have to
            {
                KinectV1StatusEventArgs args = new KinectV1StatusEventArgs();
                args.Status = (KinectBase.KinectStatus)e.Status;  //This case SHOULD work because the numbers in the enum are assigned the same
                int sensorNumber = -1;
                bool isXbox360Kinect = false;
                for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
                {
                    if (e.Sensor.UniqueKinectId == KinectSensor.KinectSensors[i].UniqueKinectId)
                    {
                        sensorNumber = i;
                        try
                        {
                            ColorCameraSettings test = KinectSensor.KinectSensors[i].ColorStream.CameraSettings;
                            test = null;
                            isXbox360Kinect = false;
                        }
                        catch
                        {
                            isXbox360Kinect = true;
                        }
                    }
                }
                args.KinectNumber = sensorNumber;
                //args.ConnectionID = e.Sensor.DeviceConnectionId;
                args.UniqueKinectID = e.Sensor.UniqueKinectId;
                args.isXBox360Kinect = isXbox360Kinect;

                OnKinectV1StatusChanged(args);
            }
        }

        protected virtual void OnKinectV1StatusChanged(KinectV1StatusEventArgs e)
        {
            if (KinectV1StatusChanged != null)
            {
                KinectV1StatusChanged(this, e);
            }
        }
    }

    public delegate void KinectV1StatusEventHandler(object sender, KinectV1StatusEventArgs e);

    public class KinectV1StatusEventArgs : EventArgs
    {
        public KinectBase.KinectStatus Status;
        //public string ConnectionID;
        public string UniqueKinectID;
        public int KinectNumber; //This is the number in the SDKs list of kinects
        public bool isXBox360Kinect;
    }
}
