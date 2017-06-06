using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    //Many of the enumerators from the Kinect namespace have to be replicated here so we have a base set we can use with both versions of the sensor
    //The enum values have been set so, as best as possible, they can be directly cast from the KinectBase version to the Microsoft version
    //public enum JointType {HipCenter = 0, Spine = 1, ShoulderCenter = 2, Head = 3, ShoulderLeft = 4,
    //                ElbowLeft = 5, WristLeft = 6, HandLeft = 7, ShoulderRight = 8, ElbowRight = 9,
    //                WristRight = 10, HandRight = 11, HipLeft = 12, KneeLeft = 13, AnkleLeft = 14,
    //                FootLeft = 15, HipRight = 16, KneeRight = 17, AnkleRight = 18, FootRight = 19,
    //                SpineShoulder = 20, HandTipLeft = 21, ThumbLeft = 22, HandTipRight = 23, ThumbRight = 24,
    //                SpineBase = 25, SpineMid = 26, Neck = 27}
    
    //Joints that have been renamed in Kinect 2 are double mapped to the same number as the old name
    public enum JointType { HipCenter = 0, Spine = 1, ShoulderCenter = 2, Head = 3,
                            ShoulderLeft = 4, ElbowLeft = 5, WristLeft = 6, HandLeft = 7, 
                            ShoulderRight = 8, ElbowRight = 9, WristRight = 10, HandRight = 11, 
                            HipLeft = 12, KneeLeft = 13, AnkleLeft = 14, FootLeft = 15, 
                            HipRight = 16, KneeRight = 17, AnkleRight = 18, FootRight = 19,
                            HandTipLeft = 21, ThumbLeft = 22, HandTipRight = 23, ThumbRight = 24,
                            SpineShoulder = 2, SpineBase = 0, SpineMid = 1, Neck = 20 }

    public enum AudioTrackingMode { Loudest, Feedback, MergedSkeletonX, LocalSkeletonX } 
    public enum EchoCancellationMode {None = 0, CancellationOnly = 1, CancellationAndSuppression = 2}
    public enum TrackingState { NotTracked = 0, Inferred = 1, Tracked = 2, PositionOnly = 3 }
    public enum TrackingConfidence { Low = 0, High = 1, Unknown = 2}
    public enum KinectVersion { NetworkKinect = 0, KinectV1 = 1, KinectV2 = 2, }
    public enum KinectStatus { Undefined = 0, Disconnected = 1, Connected = 2, Initializing = 3, Error = 4, NotPowered = 5, NotReady = 6, DeviceNotGenuine = 7, DeviceNotSupported = 8, InsufficientBandwidth = 9 };
    public enum ColorImageFormat { Undefined = 0, RgbResolution640x480Fps30 = 1, RgbResolution1280x960Fps12 = 2, YuvResolution640x480Fps15 = 3, RawYuvResolution640x480Fps15 = 4, InfraredResolution640x480Fps30 = 5, RawBayerResolution640x480Fps30 = 6, RawBayerResolution1280x960Fps12 = 7 };
    public enum PowerLineFrequency { Disabled = 0, FiftyHertz = 1, SixtyHertz = 2 };
    public enum BacklightCompensationMode { AverageBrightness = 0, CenterPriority = 1, LowlightsPriority = 2, CenterOnly = 4 };
    public enum DepthImageFormat { Undefined = 0, Resolution640x480Fps30 = 1, Resolution320x240Fps30 = 2, Resolution80x60Fps30 = 3 };

    public enum CommandType { Voice, Gesture/*, Analog */}
    public enum ServerType { Button, Analog, Tracker, Text, Imager }
    public enum ButtonType { Setter, Toggle, Momentary }
    public enum SkeletonSortMethod
    {
        NoSort = 0, OriginXClosest = 1, OriginXFarthest = 2, OriginYClosest = 3, OriginYFarthest = 4, OriginZClosest = 5, OriginZFarthest = 6, OriginEuclidClosest = 7, OriginEuclidFarthest = 8,
        FeedbackXClosest = 9, FeedbackXFarthest = 10, FeedbackYClosest = 11, FeedbackYFarthest = 12, FeedbackZClosest = 13, FeedbackZFarthest = 14, FeedbackEuclidClosest = 15, FeedbackEuclidFarthest = 16
    }
    //public enum GestureType { Recorded }
    public enum PressState { Pressed, Released }
}
