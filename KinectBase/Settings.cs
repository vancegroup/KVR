using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
//using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System;

namespace KinectBase
{
    public class MasterSettings
    {
        public AudioSettings audioOptions;
        public SkeletonSettings mergedSkeletonOptions;
        public FeedbackSettings feedbackOptions;
        
        //These fields are used internally in the code, but can't be written to the XML settings file
        [XmlIgnore]
        public List<IKinectSettings> kinectOptionsList;
        [XmlIgnore]
        public List<AnalogServerSettings> analogServers;
        [XmlIgnore]
        public List<ButtonServerSettings> buttonServers;
        [XmlIgnore]
        public List<TextServerSettings> textServers;
        [XmlIgnore]
        public List<TrackerServerSettings> trackerServers;
        [XmlIgnore]
        public List<ImagerServerSettings> imagerServers;

        public ObservableCollection<VoiceButtonCommand> voiceButtonCommands;
        public ObservableCollection<VoiceTextCommand> voiceTextCommands;
        public ObservableCollection<GestureCommand> gestureCommands;
        [XmlIgnore]
        public List<VoiceCommand> voiceCommands //This has to be non-serialized since it is a composite property.  XmlIgnore should take care of that
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
            kinectOptionsList = new List<IKinectSettings>();
            audioOptions = new AudioSettings();
            mergedSkeletonOptions = new SkeletonSettings();
            feedbackOptions = new FeedbackSettings();
            voiceTextCommands = new ObservableCollection<VoiceTextCommand>();
            voiceButtonCommands = new ObservableCollection<VoiceButtonCommand>();
            gestureCommands = new ObservableCollection<GestureCommand>();
            analogServers = new List<AnalogServerSettings>();
            buttonServers = new List<ButtonServerSettings>();
            textServers = new List<TextServerSettings>();
            trackerServers = new List<TrackerServerSettings>();
        }
    }

    public class AudioSettings
    {
        public int sourceID { get; set; } //Anything <0 is the default source
        public string recognizerEngineID { get; set; }
        public KinectBase.EchoCancellationMode echoMode { get; set; }
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
            predictAheadMS = 0;
            skeletonSortMode = SkeletonSortMethod.NoSort;
        }

        //public bool EnableTrackingInNearRange { get; set; } //This should just implicitly be enabled
        public bool isSeatedMode { get; set; }
        public double predictAheadMS { get; set; }
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
        public KinectBase.JointType sensorJointType { get; set; }
    }

    public interface IServerSettings
    {
        string serverName { get; set; }
        ServerType serverType { get; }
    }

    public class AnalogServerSettings : IServerSettings
    {
        public string serverName { get; set;}
        public ServerType serverType
        {
            get { return ServerType.Analog; }
        }
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

    public class ButtonServerSettings : IServerSettings
    {
        public string serverName { get; set; }
        public ServerType serverType
        {
            get { return ServerType.Button; }
        }
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

    public class TextServerSettings : IServerSettings
    {
        public string serverName { get; set; }
        public ServerType serverType
        {
            get { return ServerType.Text; }
        }
    }

    public class TrackerServerSettings
    {
        public string serverName { get; set; }
        public ServerType serverType
        {
            get { return ServerType.Tracker; }
        }
        public int sensorCount { get; set; }
    }

    public class ImagerServerSettings
    {
        public string serverName { get; set; }
        public ServerType serverType
        {
            get { return ServerType.Imager; }
        }
        public int rows { get; set; }
        public int columns { get; set; }
        public bool isColor { get; set; }
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
        public string gestureName { get; set; }

        //Since the gestures will be sent as button commands, we need the button options here
        public int buttonNumber { get; set; }
        public ButtonType buttonType { get; set; }
        public bool initialState { get; set; }
        public bool setState { get; set; }

        //This will likely need to be added to to handle recorded gestures
    }
}