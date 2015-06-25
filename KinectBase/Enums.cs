using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    //Many of the enumerators from the Kinect namespace have to be replicated here so we have a base set we can use with both versions of the sensor

    public enum JointType {HipCenter = 0, Spine = 1, ShoulderCenter = 2, Head = 3, ShoulderLeft = 4,
                    ElbowLeft = 5, WristLeft = 6, HandLeft = 7, ShoulderRight = 8, ElbowRight = 9,
                    WristRight = 10, HandRight = 11, HipLeft = 12, KneeLeft = 13, AnkleLeft = 14,
                    FootLeft = 15, HipRight = 16, KneeRight = 17, AnkleRight = 18, FootRight = 19,
                    SpineShoulder = 20, HandTipLeft = 21, ThumbLeft = 22, HandTipRight = 23, ThumbRight = 24,
                    SpineBase = 25, SpineMid = 26, Neck = 27}

    public enum AudioTrackingMode { Loudest, Feedback, MergedSkeletonX, LocalSkeletonX } 
    public enum EchoCancellationMode {None = 0, CancellationOnly = 1, CancellationAndSuppression = 2}
    public enum TrackingState { NotTracked = 0, Inferred = 1, Tracked = 2, PositionOnly = 3 }
    public enum TrackingConfidence { Low = 0, High = 1, Unknown = 2}
    public enum KinectVersion { NetworkKinect = 0, KinectV1 = 1, KinectV2 = 2, }
    public enum KinectStatus { Undefined = 0, Disconnected = 1, Connected = 2, Initializing = 3, Error = 4, NotPowered = 5, NotReady = 6, DeviceNotGenuine = 7, DeviceNotSupported = 8, InsufficientBandwidth = 9 };

    //(*)Need to hide CommandType and ServerType from visible columns
    public enum CommandType { Voice, Gesture/*, Analog */}
    public enum ServerType { Button, Analog, Tracker, Text, Imager }
    public enum ButtonType { Setter, Toggle, Momentary }
    public enum SkeletonSortMethod
    {
        NoSort = 0, OriginXClosest = 1, OriginXFarthest = 2, OriginYClosest = 3, OriginYFarthest = 4, OriginZClosest = 5, OriginZFarthest = 6, OriginEuclidClosest = 7, OriginEuclidFarthest = 8,
        FeedbackXClosest = 9, FeedbackXFarthest = 10, FeedbackYClosest = 11, FeedbackYFarthest = 12, FeedbackZClosest = 13, FeedbackZFarthest = 14, FeedbackEuclidClosest = 15, FeedbackEuclidFarthest = 16
    }
    public enum GestureType { Recorded }
    public enum PressState { Pressed, Released }
}
