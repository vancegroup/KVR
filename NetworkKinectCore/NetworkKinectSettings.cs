using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KinectBase;

namespace NetworkKinectCore
{
    public class NetworkKinectSettings : KinectBase.IKinectSettings
    {
        //Constructors
        public NetworkKinectSettings() { } //Needed for serialization, do not use otherwise!
        public NetworkKinectSettings(string uniqueID, int kinectNumber)
        {
            uniqueKinectID = uniqueID;
            kinectID = kinectNumber;

            //Set any necessary default values here
            mergeSkeletons = true; //this will always be true, as a networked kinects only job is to pull in skeletons to merge
            kinectPosition = new Point3D(0.0, 0.0, 0.0);
            kinectYaw = 0.0;
            kinectRoll = 0.0;
            kinectPitch = 0.0;
            lhChannel = 1;
            rhChannel = 0;
            
            //Initialize the joint mappings
            jointMappings = new ObservableCollection<JointMapping>();
            for (int i = 0; i <= 26; i++)
            {
                JointType? tempJoint = mapChannelNumberToJoint(i);
                if (tempJoint.HasValue)
                {
                    JointMapping temp = new JointMapping(tempJoint.Value, i, 0.01);
                    jointMappings.Add(temp);
                }
            }
        }

        //Properties required by the IKinectSettings interface
        public string uniqueKinectID { get; set; }
        public int kinectID { get; set; }
        public KinectVersion version
        {
            get { return KinectVersion.NetworkKinect; }
        }
        public bool mergeSkeletons { get; set; }

        //Name of VRPN skeleton server
        public string serverName { get; set; }

        #region Physical Settings
        public Point3D kinectPosition { get; set; }
        public double kinectYaw { get; set; }
        public double kinectPitch { get; set; }
        public double kinectRoll { get; set; }
        #endregion

        #region Joint Mapping
        public ObservableCollection<JointMapping> jointMappings { get; set; }
        public string lhServerName { get; set; }
        public int lhChannel { get; set; }
        public string rhServerName { get; set; }
        public int rhChannel { get; set; }
        #endregion

        //Private method to help create the initial joint mappings
        private JointType? mapChannelNumberToJoint(int channelNumber)
        {
            switch (channelNumber)
            {
                case 0:
                    return JointType.Head;
                case 1:
                    return JointType.ShoulderCenter;
                case 2:
                    return JointType.Spine;
                case 3:
                    return JointType.HipCenter;
                case 4:
                    return null;
                case 5:
                    return JointType.ShoulderLeft;
                case 6:
                    return JointType.ElbowLeft;
                case 7:
                    return JointType.WristLeft;
                case 8:
                    return JointType.HandLeft;
                case 9:
                    return JointType.HandTipLeft;
                case 10:
                    return null;
                case 11:
                    return JointType.ShoulderRight;
                case 12:
                    return JointType.ElbowRight;
                case 13:
                    return JointType.WristRight;
                case 14:
                    return JointType.HandRight;
                case 15:
                    return JointType.HandTipRight;
                case 16:
                    return JointType.HipLeft;
                case 17:
                    return JointType.KneeLeft;
                case 18:
                    return JointType.AnkleLeft;
                case 19:
                    return JointType.FootLeft;
                case 20:
                    return JointType.HipRight;
                case 21:
                    return JointType.KneeRight;
                case 22:
                    return JointType.AnkleRight;
                case 23:
                    return JointType.FootRight;
                case 24:
                    return JointType.Neck;
                case 25:
                    return JointType.ThumbLeft;
                case 26:
                    return JointType.ThumbRight;
                default:
                    return null;
            }
        }
    }

    public class JointMapping
    {
        public JointMapping(JointType jointType, int channelNumber, double positionAccuracy, bool jointEnabled = true)
        {
            joint = jointType;
            channel = channelNumber;
            accuracy = positionAccuracy;
            useJoint = jointEnabled;
            lastPosition = null;
        }

        public JointType joint { get; set; }
        public string jointName
        {
            get 
            {
                return joint.ToString();
            }
        }
        public int channel { get; set; }
        public double accuracy { get; set; }
        public bool useJoint { get; set; }
        public Point3D? lastPosition { get; set; }
        public string lastPositionString
        {
            get
            {
                string temp = "N/A";
                if (lastPosition.HasValue)
                {
                    temp = "(" + lastPosition.Value.X.ToString() + ", " + lastPosition.Value.Y.ToString() + ", " + lastPosition.Value.Z.ToString() + ")";
                }
                return temp;
            }
        }
    }
}
