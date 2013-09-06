using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace KinectWithVRServer
{
    public class MasterSettings
    {
        public AudioSettings audioOptions;
        public SkeletonSettings skeletonOptions;
        public FeedbackSettings feedbackOptions;
        public List<KinectSettings> kinectOptions;
        public List<AnalogServerSettings> analogServers;
        public List<ButtonServerSettings> buttonServers;
        public List<TextServerSettings> textServers;
        public List<TrackerServerSettings> trackerServers;

        public ObservableCollection<VoiceButtonCommand> voiceButtonCommands;
        public ObservableCollection<VoiceTextCommand> voiceTextCommands;
        public ObservableCollection<GestureCommand> gestureCommands;
        internal List<VoiceCommand> voiceCommands //This needs to be internal so the save method won't try to save it to the settings file
        {
            get
            {
                List<VoiceCommand> temp = new List<VoiceCommand>();
                temp.AddRange(voiceButtonCommands);
                temp.AddRange(voiceTextCommands);
                return temp;
            }
        }

        public MasterSettings()
        {
            kinectOptions = new List<KinectSettings>();
            audioOptions = new AudioSettings();
            skeletonOptions = new SkeletonSettings();
            feedbackOptions = new FeedbackSettings();
            voiceTextCommands = new ObservableCollection<VoiceTextCommand>();
            voiceButtonCommands = new ObservableCollection<VoiceButtonCommand>();
            gestureCommands = new ObservableCollection<GestureCommand>();
            analogServers = new List<AnalogServerSettings>();
            buttonServers = new List<ButtonServerSettings>();
            textServers = new List<TextServerSettings>();
            trackerServers = new List<TrackerServerSettings>();
        }

        public bool parseSettings(out string errorMessage)
        {
            bool settingsValid = true;
            errorMessage = "";

            analogServers = new List<AnalogServerSettings>();
            buttonServers = new List<ButtonServerSettings>();
            textServers = new List<TextServerSettings>();
            trackerServers = new List<TrackerServerSettings>();

            #region Parse the Skeleton servers
            for (int i = 0; i < skeletonOptions.individualSkeletons.Count; i++)
            {
                if (skeletonOptions.individualSkeletons[i].useSkeleton)
                {
                    //Add the skeleton servers
                    if (isServerNameValid(skeletonOptions.individualSkeletons[i].serverName))
                    {
                        bool serverFound = false;
                        for (int j = 0; j < trackerServers.Count; j++) //This allows for multiple skeletons to be assigned to the same tracker, although I don't know why you would want to do that...
                        {
                            if (trackerServers[j].serverName == skeletonOptions.individualSkeletons[i].serverName)
                            {
                                serverFound = true;
                            }
                        }

                        if (!serverFound)
                        {
                            trackerServers.Add(new TrackerServerSettings() { sensorCount = 24, serverName = skeletonOptions.individualSkeletons[i].serverName });
                        }
                    }
                    else
                    {
                        settingsValid = false;
                        errorMessage += "Skeleton " + i.ToString() + " server name (\"" + skeletonOptions.individualSkeletons[i].serverName + "\") is invalid.\r\n";
                    }

                    //Parse the right hand grip
                    if (skeletonOptions.individualSkeletons[i].useRightHandGrip)
                    {
                        if (isServerNameValid(skeletonOptions.individualSkeletons[i].rightGripServerName))
                        {
                            bool found = false;
                            for (int j = 0; j < buttonServers.Count; j++)
                            {
                                if (buttonServers[j].serverName == skeletonOptions.individualSkeletons[i].rightGripServerName)
                                {
                                    found = true;
                                    if (isServerButtonNumberValid(skeletonOptions.individualSkeletons[i].rightGripButtonNumber))
                                    {
                                        if (!buttonServers[j].uniqueChannels.Contains(skeletonOptions.individualSkeletons[i].rightGripButtonNumber))
                                        {
                                            buttonServers[j].uniqueChannels.Add(skeletonOptions.individualSkeletons[i].rightGripButtonNumber);
                                        }
                                    }
                                    else
                                    {
                                        settingsValid = false;
                                        errorMessage += "Skeleton " + i.ToString() + " right-hand grip server channel (" + skeletonOptions.individualSkeletons[i].rightGripButtonNumber.ToString() + ") is invalid.\r\n";
                                    }
                                }
                            }

                            //If the button server doesn't exist, create it!
                            if (!found)
                            {
                                ButtonServerSettings temp = new ButtonServerSettings();
                                temp.serverName = skeletonOptions.individualSkeletons[i].rightGripServerName;
                                temp.uniqueChannels = new List<int>();

                                if (isServerButtonNumberValid(skeletonOptions.individualSkeletons[i].rightGripButtonNumber))
                                {
                                    temp.uniqueChannels.Add(skeletonOptions.individualSkeletons[i].rightGripButtonNumber);
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Skeleton " + i.ToString() + " right-hand grip server channel (" + skeletonOptions.individualSkeletons[i].rightGripButtonNumber.ToString() + ") is invalid.\r\n";
                                }

                                buttonServers.Add(temp);
                            }
                        }
                        else
                        {
                            settingsValid = false;
                            errorMessage += "Skeleton " + i.ToString() + " right-hand grip server name (\"" + skeletonOptions.individualSkeletons[i].rightGripServerName + "\") is invalid.\r\n";
                        }
                    }

                    //Parse the left hand grip
                    if (skeletonOptions.individualSkeletons[i].useLeftHandGrip)
                    {
                        if (isServerNameValid(skeletonOptions.individualSkeletons[i].leftGripServerName))
                        {
                            bool found = false;
                            for (int j = 0; j < buttonServers.Count; j++)
                            {
                                if (buttonServers[j].serverName == skeletonOptions.individualSkeletons[i].leftGripServerName)
                                {
                                    found = true;
                                    if (isServerButtonNumberValid(skeletonOptions.individualSkeletons[i].leftGripButtonNumber))
                                    {
                                        if (!buttonServers[j].uniqueChannels.Contains(skeletonOptions.individualSkeletons[i].leftGripButtonNumber))
                                        {
                                            buttonServers[j].uniqueChannels.Add(skeletonOptions.individualSkeletons[i].leftGripButtonNumber);
                                        }
                                    }
                                    else
                                    {
                                        settingsValid = false;
                                        errorMessage += "Skeleton " + i.ToString() + " left-hand grip server channel (" + skeletonOptions.individualSkeletons[i].leftGripButtonNumber.ToString() + ") is invalid.\r\n";
                                    }
                                }
                            }

                            //If the button server doesn't exist, create it!
                            if (!found)
                            {
                                ButtonServerSettings temp = new ButtonServerSettings();
                                temp.serverName = skeletonOptions.individualSkeletons[i].leftGripServerName;
                                temp.uniqueChannels = new List<int>();

                                if (isServerButtonNumberValid(skeletonOptions.individualSkeletons[i].leftGripButtonNumber))
                                {
                                    temp.uniqueChannels.Add(skeletonOptions.individualSkeletons[i].leftGripButtonNumber);
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Skeleton " + i.ToString() + " left-hand grip server channel (" + skeletonOptions.individualSkeletons[i].leftGripButtonNumber.ToString() + ") is invalid.\r\n";
                                }

                                buttonServers.Add(temp);
                            }
                        }
                        else
                        {
                            settingsValid = false;
                            errorMessage += "Skeleton " + i.ToString() + " left-hand grip server name (\"" + skeletonOptions.individualSkeletons[i].leftGripServerName + "\") is invalid.\r\n";
                        }
                    }
                }
            }
            #endregion

            #region Parse the Voice Commands
            for (int i = 0; i < voiceCommands.Count; i++)
            {
                if (voiceCommands[i].serverType == ServerType.Button)
                {
                    bool found = false;

                    if (isServerNameValid(voiceCommands[i].serverName))
                    {
                        //Check if the button server already exists
                        for (int j = 0; j < buttonServers.Count; j++)
                        {
                            if (buttonServers[j].serverName == voiceCommands[i].serverName)
                            {
                                //The button server exists, so lets see if it is using a unique button channel
                                found = true;
                                if (isServerButtonNumberValid(((VoiceButtonCommand)voiceCommands[i]).buttonNumber))
                                {
                                    if (!buttonServers[j].uniqueChannels.Contains(((VoiceButtonCommand)voiceCommands[i]).buttonNumber))
                                    {
                                        buttonServers[j].uniqueChannels.Add(((VoiceButtonCommand)voiceCommands[i]).buttonNumber);
                                    }
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Voice command \"" + voiceCommands[i].recognizedWord + "\" server channel (\"" + ((VoiceButtonCommand)voiceCommands[i]).buttonNumber + "\") is invalid.\r\n";
                                }
                            }
                        }

                        //The button server did not exist, time to create it!
                        if (!found)
                        {
                            ButtonServerSettings temp = new ButtonServerSettings();
                            temp.serverName = voiceCommands[i].serverName;
                            temp.uniqueChannels = new List<int>();
                            if (isServerButtonNumberValid(((VoiceButtonCommand)voiceCommands[i]).buttonNumber))
                            {
                                temp.uniqueChannels.Add(((VoiceButtonCommand)voiceCommands[i]).buttonNumber);
                            }
                            else
                            {
                                settingsValid = false;
                                errorMessage += "Voice command \"" + voiceCommands[i].recognizedWord + "\" server channel (\"" + ((VoiceButtonCommand)voiceCommands[i]).buttonNumber + "\") is invalid.\r\n";
                            }
                            buttonServers.Add(temp);
                        }
                    }
                    else
                    {
                        settingsValid = false;
                        errorMessage += "Voice command \"" + voiceCommands[i].recognizedWord + "\" server name (\"" + voiceCommands[i].serverName + "\") is invalid.\r\n";
                    }
                }
                else if (voiceCommands[i].serverType == ServerType.Text)
                {
                    bool found = false;

                    if (isServerNameValid(voiceCommands[i].serverName))
                    {
                        //Check if the button server already exists
                        for (int j = 0; j < textServers.Count; j++)
                        {
                            if (textServers[j].serverName == voiceCommands[i].serverName)
                            {
                                //The text server exists!  We don't need to check channels on text servers
                                found = true;
                            }
                        }

                        if (!found)
                        {
                            TextServerSettings temp = new TextServerSettings();
                            temp.serverName = voiceCommands[i].serverName;
                            textServers.Add(temp);
                        }
                    }
                    else
                    {
                        settingsValid = false;
                        errorMessage += "Voice command \"" + voiceCommands[i].recognizedWord + "\" server name (\"" + voiceCommands[i].serverName + "\") is invalid.\r\n";
                    }
                }
            }
            #endregion

            //TODO: Implement gesture parsing and error handling
            #region Parse the Gesture Commands
            //for (int i = 0; i < gestureCommands.Count; i++)
            //{
            //    bool found = false;

            //    for (int j = 0; j < buttonServers.Count; j++)
            //    {
            //        if (buttonServers[j].serverName == gestureCommands[i].serverName)
            //        {
            //            //The button server exists, so lets see if it is using a unique button channel
            //            found = true;
            //            if (!buttonServers[j].uniqueChannels.Contains(((GestureCommand)gestureCommands[i]).buttonNumber))
            //            {
            //                buttonServers[j].uniqueChannels.Add(((GestureCommand)gestureCommands[i]).buttonNumber);
            //            }
            //        }
            //    }

            //    if (!found)
            //    {
            //        ButtonServerSettings temp = new ButtonServerSettings();
            //        temp.serverName = gestureCommands[i].serverName;
            //        temp.uniqueChannels = new List<int>();
            //        temp.uniqueChannels.Add(((GestureCommand)gestureCommands[i]).buttonNumber);
            //        buttonServers.Add(temp);
            //    }
            //}
            #endregion

            #region Parse the per Kinect (acceleration and audio angle) settings
            for (int i = 0; i < kinectOptions.Count; i++)
            {
                //Parse the acceleration options
                if (kinectOptions[i].sendAcceleration)
                {
                    if (isServerNameValid(kinectOptions[i].accelerationServerName))
                    {
                        bool found = false;

                        for (int j = 0; j < analogServers.Count; j++)
                        {
                            if (analogServers[j].serverName == kinectOptions[i].accelerationServerName)
                            {
                                found = true;

                                //Check the X acceleration channel
                                if (isServerAnalogChannelValid(kinectOptions[i].accelXChannel))
                                {
                                    //If the channel doesn't exist, create it
                                    if (!analogServers[j].uniqueChannels.Contains(kinectOptions[i].accelXChannel))
                                    {
                                        analogServers[j].uniqueChannels.Add(kinectOptions[i].accelXChannel);
                                    }
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Kinect " + i.ToString() + " X acceleration channel (" + kinectOptions[i].accelXChannel.ToString() + ") is invalid.\r\n";
                                }

                                //Check the Y acceleration channel
                                if (isServerAnalogChannelValid(kinectOptions[i].accelYChannel))
                                {
                                    //If the channel doesn't exist, create it
                                    if (!analogServers[j].uniqueChannels.Contains(kinectOptions[i].accelYChannel))
                                    {
                                        analogServers[j].uniqueChannels.Add(kinectOptions[i].accelYChannel);
                                    }
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Kinect " + i.ToString() + " Y acceleration channel (" + kinectOptions[i].accelYChannel.ToString() + ") is invalid.\r\n";
                                }

                                //Check the Z acceleration channel
                                if (isServerAnalogChannelValid(kinectOptions[i].accelZChannel))
                                {
                                    //If the channel doesn't exist, create it
                                    if (!analogServers[j].uniqueChannels.Contains(kinectOptions[i].accelZChannel))
                                    {
                                        analogServers[j].uniqueChannels.Add(kinectOptions[i].accelZChannel);
                                    }
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Kinect " + i.ToString() + " Z acceleration channel (" + kinectOptions[i].accelZChannel.ToString() + ") is invalid.\r\n";
                                }
                            }
                        }

                        if (!found)
                        {
                            AnalogServerSettings temp = new AnalogServerSettings();
                            temp.serverName = kinectOptions[i].accelerationServerName;
                            temp.uniqueChannels = new List<int>();

                            //Add the X channel, if it is valid (it is the first one added, so it must be unique)
                            if (isServerAnalogChannelValid(kinectOptions[i].accelXChannel))
                            {
                                temp.uniqueChannels.Add(kinectOptions[i].accelXChannel);
                            }
                            else
                            {
                                settingsValid = false;
                                errorMessage += "Kinect " + i.ToString() + " X acceleration channel (" + kinectOptions[i].accelXChannel.ToString() + ") is invalid.\r\n";
                            }

                            //Add the Y channel, if it is valid and unique
                            if (isServerAnalogChannelValid(kinectOptions[i].accelYChannel))
                            {
                                if (!temp.uniqueChannels.Contains(kinectOptions[i].accelYChannel))
                                {
                                    temp.uniqueChannels.Add(kinectOptions[i].accelYChannel);
                                }
                            }
                            else
                            {
                                settingsValid = false;
                                errorMessage += "Kinect " + i.ToString() + " Y acceleration channel (" + kinectOptions[i].accelYChannel.ToString() + ") is invalid.\r\n";
                            }

                            //Add the Z channel, if it is valid and unique
                            if (isServerAnalogChannelValid(kinectOptions[i].accelZChannel))
                            {
                                if (!temp.uniqueChannels.Contains(kinectOptions[i].accelZChannel))
                                {
                                    temp.uniqueChannels.Add(kinectOptions[i].accelZChannel);
                                }
                            }
                            else
                            {
                                settingsValid = false;
                                errorMessage += "Kinect " + i.ToString() + " Z acceleration channel (" + kinectOptions[i].accelZChannel.ToString() + ") is invalid.\r\n";
                            }

                            analogServers.Add(temp);
                        }
                    }
                    else
                    {
                        settingsValid = false;
                        errorMessage += "Kinect " + i.ToString() + " acceleration server name (\"" + kinectOptions[i].accelerationServerName + "\") is invalid.\r\n";
                    }
                }

                //Parse audio source angle options
                if (kinectOptions[i].sendAudioAngle)
                {
                    if (isServerNameValid(kinectOptions[i].audioAngleServerName))
                    {
                        bool found = false;

                        for (int j = 0; j < analogServers.Count; j++)
                        {
                            if (analogServers[j].serverName == kinectOptions[i].audioAngleServerName)
                            {
                                found = true;

                                if (isServerAnalogChannelValid(kinectOptions[i].audioAngleChannel))
                                {
                                    if (!analogServers[j].uniqueChannels.Contains(kinectOptions[i].audioAngleChannel))
                                    {
                                        analogServers[j].uniqueChannels.Add(kinectOptions[i].audioAngleChannel);
                                    }
                                }
                                else
                                {
                                    settingsValid = false;
                                    errorMessage += "Kinect " + i.ToString() + " audio angle server channel (" + kinectOptions[i].audioAngleChannel.ToString() + ") is invalid.\r\n";
                                }
                            }
                        }

                        if (!found)
                        {
                            AnalogServerSettings temp = new AnalogServerSettings();
                            temp.serverName = kinectOptions[i].audioAngleServerName;
                            temp.uniqueChannels = new List<int>();
                            if (isServerAnalogChannelValid(kinectOptions[i].audioAngleChannel))
                            {
                                temp.uniqueChannels.Add(kinectOptions[i].audioAngleChannel);
                            }
                            else
                            {
                                settingsValid = false;
                                errorMessage += "Kinect " + i.ToString() + " audio angle server channel (" + kinectOptions[i].audioAngleChannel.ToString() + ") is invalid.\r\n";
                            }
                            analogServers.Add(temp);
                        }
                    }
                    else
                    {
                        settingsValid = false;
                        errorMessage += "Kinect " + i.ToString() + " audio angle server name (\"" + kinectOptions[i].audioAngleServerName + "\") is invalid.\r\n";
                    }
                }
            }
            #endregion

            return settingsValid;
        }

        private bool isServerNameValid(string serverName)
        {
            bool valid = true;

            valid = serverName != null && serverName != "";
            //TODO: This checks if the servername is alphanumeric, are there any other valid characters?
            valid = valid && System.Text.RegularExpressions.Regex.IsMatch(serverName, @"^[a-zA-Z0-9]+$");

            return valid;
        }

        private bool isServerAnalogChannelValid(int channel)
        {
            bool valid = true;
            valid = channel >= 0 && channel < 128;
            return valid;
        }

        private bool isServerButtonNumberValid(int buttonNumber)
        {
            bool valid = true;
            valid = buttonNumber >= 0 && buttonNumber < 256;
            return valid;
        }
    }

    public class KinectSettings
    {
        public KinectSettings(string deviceConnectionID, int kinectNumber)
        {
            connectionID = deviceConnectionID;
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
            trackSkeletons = false;
            kinectPosition = new Point3D(0, 0, 0);
            kinectYaw = 0.0;
            sendAcceleration = false;
            sendAudioAngle = false;
            audioTrackMode = AudioTrackingMode.Loudest;
        }

        public string connectionID { get; set; }
        public int kinectID { get; set; }

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
        public bool trackSkeletons { get; set; }
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

    public class AudioSettings
    {
        public int sourceID { get; set; } //Anything <0 is the default source
        public string recognizerEngineID { get; set; }
        public EchoCancellationMode echoMode { get; set; }
        public bool noiseSurpression { get; set; }
        public bool autoGainEnabled { get; set; } //This should probably always be false, but I will put a setting in for it anyway
    }

    public class SkeletonSettings
    {
        public SkeletonSettings()
        {
            individualSkeletons = new ObservableCollection<PerSkeletonSettings>();

            //Set defaults
            isSeatedMode = false;
            skeletonSortMode = SkeletonSortMethod.NoSort;
        }

        //public bool EnableTrackingInNearRange { get; set; } //This should just implicitly be enabled
        public bool isSeatedMode { get; set; }
        public SkeletonSortMethod skeletonSortMode { get; set; }
        public ObservableCollection<PerSkeletonSettings> individualSkeletons { get; set; }
    }

    public class PerSkeletonSettings
    {
        public int skeletonNumber { get; set; }
        public bool useSkeleton {get; set;}
        public string serverName {get; set;}
        public bool useRightHandGrip { get; set; }
        public bool useLeftHandGrip { get; set; }
        public string rightGripServerName { get; set; }
        public string leftGripServerName { get; set; }
        public int rightGripButtonNumber { get; set; }
        public int leftGripButtonNumber { get; set; }
        public Color renderColor { get; set; }
    }

    public class FeedbackSettings
    {
        public FeedbackSettings()
        {
            //Set the default values
            useFeedback = false;
        }

        public bool useFeedback { get; set; }
        public string feedbackServerName { get; set; }
        public int feedbackSensorNumber { get; set; }
        public JointType sensorJointType { get; set; }
    }

    public class AnalogServerSettings
    {
        public string serverName { get; set;}
        public int trueChannelCount
        {
            get
            {
                if (uniqueChannels != null)
                {
                    return uniqueChannels.Count;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int maxChannelUsed
        {
            get
            {
                if (uniqueChannels != null)
                {
                    return uniqueChannels.Max<int>();
                }
                else
                {
                    return 0;
                }
            }
        }
        public List<int> uniqueChannels { get; set; }
    }

    public class ButtonServerSettings
    {
        public string serverName { get; set; }
        public int trueButtonCount 
        {
            get
            {
                if (uniqueChannels != null)
                {
                    return uniqueChannels.Count;
                }
                else
                {
                    return 0;
                }
            }
        }
        public int maxButtonUsed
        {
            get
            {
                if (uniqueChannels != null)
                {
                    return uniqueChannels.Max<int>();
                }
                else
                {
                    return 0;
                }
            }
        }
        public List<int> uniqueChannels { get; set; }
    }

    public class TextServerSettings
    {
        public string serverName { get; set; }
    }

    public class TrackerServerSettings
    {
        public string serverName { get; set; }
        public int sensorCount { get; set; }
    }

    public class Command
    {
        public string serverName { get; set; }
        public CommandType commandType { get; set; }
        public string comments { get; set; }  //I think this should either be changed to button name (for use in VR juggler JCONFs) or a seperate name should be added for it
    }

    public class VoiceCommand : Command
    {
        public ServerType serverType { get; set; }
        public double confidence { get; set; }
        public string recognizedWord { get; set; }
    }

    public class VoiceTextCommand : VoiceCommand
    {
        public VoiceTextCommand()
        {
            base.serverType = ServerType.Text;
            base.commandType = CommandType.Voice;
        }

        public string actionText { get; set; }
    }

    public class VoiceButtonCommand : VoiceCommand
    {
        public VoiceButtonCommand()
        {
            base.serverType = ServerType.Button;
            base.commandType = CommandType.Voice;
        }

        public ButtonType buttonType { get; set; }
        public int buttonNumber { get; set; }
        public bool initialState { get; set; }
        public bool setState { get; set; }
    }

    public class GestureCommand : Command
    {
        public GestureCommand()
        {
            base.commandType = CommandType.Gesture;
        }

        public ServerType serverType
        {
            get { return ServerType.Button; }
        }
        public GestureType gestureType { get; set; }
        public int buttonNumber { get; set; }
        //This will likely need to be added to to handle recorded gestures
    }

    public class KinectSettingsComparer : IComparer<KinectSettings>
    {
        //This just redirects the compare to a comparison on the KinectID property
        public int Compare(KinectSettings x, KinectSettings y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }

    //(*)Need to hide CommandType and ServerType from visible columns
    public enum CommandType { Voice, Gesture/*, Analog */}
    public enum ServerType { Button, Analog, Tracker, Text }
    public enum ButtonType { Setter, Toggle, Momentary }
    public enum SkeletonSortMethod { NoSort = 0, OriginXClosest = 1, OriginXFarthest = 2, OriginYClosest = 3, OriginYFarthest = 4, OriginZClosest = 5, OriginZFarthest = 6, OriginEuclidClosest = 7, OriginEuclidFarthest = 8, 
                                     FeedbackXClosest = 9, FeedbackXFarthest = 10, FeedbackYClosest = 11, FeedbackYFarthest = 12, FeedbackZClosest = 13, FeedbackZFarthest = 14, FeedbackEuclidClosest = 15, FeedbackEuclidFarthest = 16 }
    public enum GestureType { Recorded }
    public enum PressState { Pressed, Released }
    public enum AudioTrackingMode { Loudest, Feedback, SkeletonX }
}