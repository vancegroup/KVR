using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace KinectBase
{
    public class HelperMethods
    {
        public static Key[] NumberKeys = {Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
                                    Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, 
                                    Key.Return, Key.Enter, Key.Delete, Key.Back, Key.Left, Key.Right, Key.Tab, Key.OemMinus, Key.Subtract};
        public const int TotalJointCount = 28; //This should always be the same as the number of items in the JointType enum


        //Converts a Quaternion to a rotation matrix
        //Based on https://en.wikipedia.org/wiki/Quaternions_and_spatial_rotation
        //And http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToMatrix/index.htm
        //This has been transposed for the source because we are assuming a row vector position instead of a column vector
        public static System.Windows.Media.Media3D.Matrix3D QuaternionToMatrix(System.Windows.Media.Media3D.Quaternion quat)
        {
            System.Windows.Media.Media3D.Matrix3D tempMat = System.Windows.Media.Media3D.Matrix3D.Identity;
            if (!quat.IsNormalized)
            {
                quat.Normalize();
            }

            tempMat.M11 = 1 - 2 * Math.Pow(quat.Y, 2) - 2 * Math.Pow(quat.Z, 2);
            tempMat.M12 = 2 * quat.X * quat.Y + 2 * quat.Z * quat.W;
            tempMat.M13 = 2 * quat.X * quat.Z - 2 * quat.Y * quat.W;
            tempMat.M14 = 0.0;

            tempMat.M21 = 2 * quat.X * quat.Y - 2 * quat.Z * quat.W;
            tempMat.M22 = 1 - 2 * Math.Pow(quat.X, 2) - 2 * Math.Pow(quat.Z, 2);
            tempMat.M23 = 2 * quat.Y * quat.Z + 2 * quat.X * quat.W;
            tempMat.M24 = 0.0;

            tempMat.M31 = 2 * quat.X * quat.Z + 2 * quat.Y * quat.W;
            tempMat.M32 = 2 * quat.Y * quat.Z - 2 * quat.X * quat.W;
            tempMat.M33 = 1 - 2 * Math.Pow(quat.X, 2) - 2 * Math.Pow(quat.Y, 2);
            tempMat.M34 = 0.0;

            tempMat.OffsetX = 0.0;
            tempMat.OffsetY = 0.0;
            tempMat.OffsetZ = 0.0;
            tempMat.M44 = 1.0;


            return tempMat;
        }

        //Converts a 3D rotation matrix to a normalized rotation quaternion
        //Based on http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/
        //And http://www.ee.ucr.edu/~farrell/AidedNavigation/D_App_Quaternions/Rot2Quat.pdf
        //This has been transposed from the source because we are assuming a row vector position instead of a column vector
        public static System.Windows.Media.Media3D.Quaternion MatrixToQuaternion(System.Windows.Media.Media3D.Matrix3D mat)
        {
            System.Windows.Media.Media3D.Quaternion tempQuat = System.Windows.Media.Media3D.Quaternion.Identity;

            double t = Trace3(mat);
            if (t > 0)
            {
                double r = Math.Sqrt(1 + t);
                double s = 0.5 / r;
                double w = 0.5 * r;
                double x = (mat.M23 - mat.M32) * s;
                double y = (mat.M31 - mat.M13) * s;
                double z = (mat.M12 - mat.M21) * s;
                tempQuat = new System.Windows.Media.Media3D.Quaternion(x, y, z, w);
            }
            else if (mat.M11 > mat.M22 && mat.M11 > mat.M33)
            {
                double r = Math.Sqrt(1 + mat.M11 - mat.M22 - mat.M33);
                double s = 0.5 / r;
                double w = (mat.M23 - mat.M32) * s;
                double x = 0.5 * r;
                double y = (mat.M21 + mat.M12) * s;
                double z = (mat.M31 + mat.M13) * s;
                tempQuat = new System.Windows.Media.Media3D.Quaternion(x, y, z, w);
            }
            else if (mat.M22 > mat.M33)
            {
                double r = Math.Sqrt(1 + mat.M22 - mat.M11 - mat.M33);
                double s = 0.5 / r;
                double w = (mat.M31 - mat.M13) * s;
                double x = (mat.M21 + mat.M12) * s;
                double y = 0.5 * r;
                double z = (mat.M32 + mat.M23) * s;
                tempQuat = new System.Windows.Media.Media3D.Quaternion(x, y, z, w);
            }
            else
            {
                double r = Math.Sqrt(1 + mat.M33 - mat.M11 - mat.M22);
                double s = 0.5 / r;
                double w = (mat.M12 - mat.M21) * s;
                double x = (mat.M31 + mat.M13) * s;
                double y = (mat.M32 + mat.M23) * s;
                double z = 0.5 * r;
                tempQuat = new System.Windows.Media.Media3D.Quaternion(x, y, z, w);
            }

            tempQuat.Normalize();
            return tempQuat;
        }

        //This takes the trace of a 4x4 matrix as if it were a 3x3 (i.e., it leaves out the M44 component)
        public static double Trace3(System.Windows.Media.Media3D.Matrix3D m)
        {
            return m.M11 + m.M22 + m.M33;
        }
    }

    public struct Joint
    {
        public JointType JointType;
        public System.Windows.Media.Media3D.Point3D Position;
        //public System.Windows.Media.Media3D.Quaternion Orientation;
        public JointOrientation Orientation;
        public TrackingState TrackingState;
        public TrackingConfidence Confidence;
        public DateTime utcTime;  //This is here instead of in the skeleton because networked kinects could have joints updated at different times
        public System.Windows.Media.Media3D.Point3D spatialErrorStdDev;
    }

    public struct JointOrientation
    {
        private bool matSet;  //This will initialize to false automatically
        private bool quatSet;  //This will initialize to false automatically
        private System.Windows.Media.Media3D.Matrix3D internalMatrix;  //This will initialize to identity automatically
        private System.Windows.Media.Media3D.Quaternion internalQuaternion;  //This will initialize to identity automatically
        public System.Windows.Media.Media3D.Matrix3D orientationMatrix
        {
            get
            {
                if (matSet || (!matSet && !quatSet))
                {
                    return internalMatrix;
                }
                else
                {
                    return HelperMethods.QuaternionToMatrix(internalQuaternion);
                }
            }
            set
            {
                matSet = true;
                internalMatrix = value;
            }
        }
        public System.Windows.Media.Media3D.Quaternion orientationQuaternion
        {
            get
            {
                if (quatSet || (!quatSet && !matSet))
                {
                    return internalQuaternion;
                }
                else
                {
                    return HelperMethods.MatrixToQuaternion(internalMatrix);
                }
            }
            set
            {
                quatSet = true;
                internalQuaternion = value;
            }
        }
    }

    public class KinectSkeletonsData// : INotifyPropertyChanged
    {
        public KinectSkeletonsData(string UniqueID, int skeletonCount)
        {
            //utcTime = DateTime.UtcNow;
            uID = UniqueID;
            actualSkeletons = new List<KinectSkeleton>(skeletonCount);
            for (int i = 0; i < skeletonCount; i++)
            {
                actualSkeletons.Add(new KinectSkeleton());
            }
        }
        
        private string uID = "";

        public bool processed { get; set; }
        public int kinectID { get; set; }
        public string uniqueID
        {
            get { return uID; }
        }
        //public DateTime utcTime {get; set;}

        public List<KinectSkeleton> actualSkeletons { get; set; }
    }

    public class KinectSkeletonsDataComparer : IComparer<KinectSkeletonsData>
    {
        //This just redirects the compare to a comparison on the KinectID property
        public int Compare(KinectSkeletonsData x, KinectSkeletonsData y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }

    public class KinectSkeleton
    {
        public KinectSkeleton()
        {
            skeleton = new SkeletonData();
        }

        public SkeletonData skeleton;
        public System.Windows.Media.Media3D.Point3D Position;
        public TrackingState SkeletonTrackingState;
        public bool rightHandClosed;
        public bool leftHandClosed;
        public int sourceKinectID;
        //public DateTime utcSampleTime;
        public int TrackingId;
    }

    public class SkeletonData
    {
        private Joint[] jointBacker;
        
        public SkeletonData()
        {
            jointBacker = new Joint[HelperMethods.TotalJointCount];

            for (int i = 0; i < HelperMethods.TotalJointCount; i++)
            {
                Joint temp = new Joint();
                temp.Confidence = TrackingConfidence.Unknown;
                temp.JointType = (JointType)i;
                temp.Orientation = new JointOrientation();
                temp.Position = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                temp.TrackingState = TrackingState.NotTracked;
                temp.utcTime = DateTime.MinValue;  //Flag this as never having been updated
                jointBacker[i] = temp;
            }
        }

        public int Count
        {
            get { return jointBacker.Length; }
        }

        public Joint this[JointType i]
        {
            get { return jointBacker[(int)i]; }
            set
            {
                if (value.JointType == i)
                {
                    jointBacker[(int)i] = value;
                }
                else
                {
                    throw new ArgumentException("The joint data must be of the type that is being set.");
                }
            }
        }

        public Joint this[int i]
        {
            get
            {
                if (i >= 0 && i < HelperMethods.TotalJointCount)
                {
                    return jointBacker[i];
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
            set
            {
                if (i >= 0 && i < HelperMethods.TotalJointCount)
                {
                    if (value.JointType == (JointType)i)
                    {
                        jointBacker[i] = value;
                    }
                    else
                    {
                        throw new ArgumentException("The joint type doesn't match.");
                    }
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
            }
        }
    }

    public class ObjectPool<T>
    {
        //Based on https://msdn.microsoft.com/en-us/library/ff458671(v=vs.110).aspx
        private System.Collections.Concurrent.ConcurrentStack<T> objects;
        private Func<T> objectGenerator;

        public ObjectPool(Func<T> objectGen)
        {
            if (objectGen == null)
            {
                throw new ArgumentNullException("objectGen");
            }
            objects = new System.Collections.Concurrent.ConcurrentStack<T>();
            objectGenerator = objectGen;
        }

        public T GetObject()
        {
            T obj;
            if (objects.TryPop(out obj))
            {
                return obj;
            }
            else
            {
                return objectGenerator();
            }
        }

        public void PutObject(T obj)
        {
            objects.Push(obj);
        }

        public void ResetPool(Func<T> newObjectGen)
        {
            objectGenerator = newObjectGen;
            objects.Clear();
        }
    }
}
