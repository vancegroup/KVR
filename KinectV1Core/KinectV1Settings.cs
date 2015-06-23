using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using KinectBase;

namespace KinectV1Core
{
    public class KinectV1Settings : IKinectSettings
    {
        public KinectV1Settings() { } //Needed for serialization
        public KinectV1Settings(string deviceConnectionID, string uniqueID, int kinectNumber)
        {
            connectionID = deviceConnectionID;
            uniqueKinectID = uniqueID;
            kinectID = kinectNumber;

            //Set everything to the default value
            colorImageMode = ColorImageFormat.RgbResolution640x480Fps30;
            lineFreq = PowerLineFrequency.SixtyHertz;
            autoWhiteBalance = true;
            autoExposure = true;
            backlightMode = BacklightCompensationMode.AverageBrightness;
            depthImageMode = DepthImageFormat.Resolution320x240Fps30;
            isNearMode = false;
            irON = true;
            mergeSkeletons = false;
            kinectPosition = new Point3D(0, 0, 0);
            kinectYaw = 0.0;
            sendAcceleration = false;
            sendAudioAngle = false;
            audioTrackMode = AudioTrackingMode.Loudest;

            //Setup the options for the raw skeleton, irrespective of use
            rawSkeletonSettings = new SkeletonSettings();
        }

        public string connectionID { get; set; }
        public string uniqueKinectID { get; set; }
        public int kinectID { get; set; }
        public KinectVersion version
        {
            get { return KinectVersion.KinectV1; }
        }

        #region Color Settings
        public ColorImageFormat colorImageMode { get; set; }
        public PowerLineFrequency lineFreq { get; set; }
        public bool autoWhiteBalance { get; set; }
        public bool autoExposure { get; set; }
        public BacklightCompensationMode backlightMode { get; set; }
        private double brightness = 0.2156;
        public double Brightness
        {
            get { return brightness; }
            set
            {
                if (value < 0.0)
                {
                    brightness = 0.0;
                }
                else if (value > 1.0)
                {
                    brightness = 1.0;
                }
                else
                {
                    brightness = value;
                }
            }
        }
        private double contrast = 1.0;
        public double Contrast
        {
            get { return contrast; }
            set
            {
                if (value < 0.5)
                {
                    contrast = 0.5;
                }
                else if (value > 2.0)
                {
                    contrast = 2.0;
                }
                else
                {
                    contrast = value;
                }
            }
        }
        private double exposureTime = 0.0;
        public double ExposureTime
        {
            get { return exposureTime; }
            set
            {
                if (value < 0.0)
                {
                    exposureTime = 0.0;
                }
                else if (value > 4000.0)
                {
                    exposureTime = 4000.0;
                }
                else
                {
                    exposureTime = value;
                }
            }
        }
        private double frameInterval = 0.0;
        public double FrameInterval
        {
            get { return frameInterval; }
            set
            {
                if (value < 0.0)
                {
                    frameInterval = 0.0;
                }
                else if (value > 4000.0)
                {
                    frameInterval = 4000.0;
                }
                else
                {
                    frameInterval = value;
                }
            }
        }
        private double gain = 1.0;
        public double Gain
        {
            get { return gain; }
            set
            {
                if (value < 1.0)
                {
                    gain = 1.0;
                }
                else if (value > 16.0)
                {
                    gain = 16.0;
                }
                else
                {
                    gain = value;
                }
            }
        }
        private double gamma = 2.2;
        public double Gamma
        {
            get { return gamma; }
            set
            {
                if (value < 1.0)
                {
                    gamma = 1.0;
                }
                else if (value > 2.8)
                {
                    gamma = 2.8;
                }
                else
                {
                    gamma = value;
                }
            }
        }
        private double hue = 0.0;
        public double Hue
        {
            get { return hue; }
            set
            {
                if (value < -22.0)
                {
                    hue = -22.0;
                }
                else if (value > 22.0)
                {
                    hue = 22.0;
                }
                else
                {
                    hue = value;
                }
            }
        }
        private double saturation = 1.0;
        public double Saturation
        {
            get { return saturation; }
            set
            {
                if (value < 0.0)
                {
                    saturation = 0.0;
                }
                else if (value > 2.0)
                {
                    saturation = 2.0;
                }
                else
                {
                    saturation = value;
                }
            }
        }
        private double sharpness = 0.5;
        public double Sharpness
        {
            get { return sharpness; }
            set
            {
                if (value < 0.0)
                {
                    sharpness = 0.0;
                }
                else if (value > 1.0)
                {
                    sharpness = 1.0;
                }
                else
                {
                    sharpness = value;
                }
            }
        }
        private int whiteBalance = 2700;
        public int WhiteBalance
        {
            get { return whiteBalance; }
            set
            {
                if (value < 2700)
                {
                    whiteBalance = 2700;
                }
                else if (value > 6500)
                {
                    whiteBalance = 6500;
                }
                else
                {
                    whiteBalance = value;
                }
            }
        }
        #endregion
        #region Depth Settings
        public DepthImageFormat depthImageMode { get; set; }
        public bool isNearMode { get; set; }
        public bool irON { get; set; }
        #endregion
        #region Skeleton and Physical Settings
        public bool mergeSkeletons { get; set; }
        public bool sendRawSkeletons { get; set; }
        public bool transformRawSkeletons { get; set; }
        public SkeletonSettings rawSkeletonSettings { get; set; }
        public Point3D kinectPosition { get; set; }
        public double kinectYaw { get; set; }
        public bool sendAcceleration { get; set; }
        public string accelerationServerName { get; set; }
        public int accelXChannel { get; set; }
        public int accelYChannel { get; set; }
        public int accelZChannel { get; set; }
        #endregion
        #region Audio Source Settings
        public bool sendAudioAngle { get; set; }
        public AudioTrackingMode audioTrackMode { get; set; }
        public int audioBeamTrackSkeletonNumber { get; set; }
        public string audioAngleServerName { get; set; }
        public int audioAngleChannel { get; set; }
        #endregion
    }
}
