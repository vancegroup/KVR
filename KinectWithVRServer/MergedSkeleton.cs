using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KinectBase;

namespace KinectWithVRServer
{
    internal class MergedSkeleton
    {
        //Thresholds used internally to determine boolean states
        private const double handGrabTheshold = 0.45;  //NOTE: This can be made lower or higher to make the hand grab more or less sensitive

        private List<KinectSkeleton> skeletonsToMerge;
        private bool needsUpdate = false;
        private bool leftGrab = false;
        private bool rightGrab = false;
        private Point3D skeletonPosition = new Point3D(0, 0, 0);
        private TrackingState skeletonTrackingState = TrackingState.NotTracked;
        private SkeletonData mergedSkeleton = new SkeletonData();

        public int Count 
        {
            get { return skeletonsToMerge.Count; }
        }

        //These properties reimplement the same variables as in the KinectSkeleton, but they are generated from the list of skeletons to merge
        internal bool rightHandClosed
        {
            get
            {
                if (needsUpdate)
                {
                    RecalculateAllVariables();
                }
                return rightGrab;
            }
        }
        internal bool leftHandClosed
        {
            get
            {
                if (needsUpdate)
                {
                    RecalculateAllVariables();
                }
                return leftGrab;
            }
        }
        internal Point3D Position
        {
            get
            {
                if (needsUpdate)
                {
                    RecalculateAllVariables();
                }
                return skeletonPosition;
            }
        }
        internal TrackingState SkeletonTrackingState
        {
            get
            {
                if (needsUpdate)
                {
                    RecalculateAllVariables();
                }
                return skeletonTrackingState;
            }
        }
        internal SkeletonData skeleton
        {
            get
            {
                if (needsUpdate)
                {
                    RecalculateAllVariables();
                }
                return mergedSkeleton;
            }
        }


        internal MergedSkeleton()
        {
            skeletonsToMerge = new List<KinectSkeleton>();
        }
        internal void AddSkeletonToMerge(KinectSkeleton skeleton)
        {
            for (int i = skeletonsToMerge.Count - 1; i >= 0; i--)
            {
                if (skeletonsToMerge[i].sourceKinectID == skeleton.sourceKinectID)
                {
                    skeletonsToMerge.RemoveAt(i);
                }
            }

            skeletonsToMerge.Add(skeleton);
            needsUpdate = true;
        }
        internal bool validMerge
        {
            get
            {
                return skeletonsToMerge.Count > 0;
            }
        }

        //These methods do the actual calculations of the points
        private void RecalculateAllVariables()
        {
            rightGrab = RecalculateHandGrab(JointType.HandRight);
            leftGrab = RecalculateHandGrab(JointType.HandLeft);
            skeletonPosition = RecalculatePosition();
            skeletonTrackingState = RecalculateSkeletonTrackingState();
            mergedSkeleton = new SkeletonData();

            for (int i = 0; i < mergedSkeleton.Count; i++)
            {
                Joint tempJoint = new Joint();
                tempJoint.JointType = mergedSkeleton[i].JointType;
                tempJoint.Confidence = RecalculateJointConfidence(mergedSkeleton[i].JointType);
                tempJoint.Orientation = RecalculateJointOrientation(mergedSkeleton[i].JointType);
                tempJoint.Position = RecalculateJointPosition(mergedSkeleton[i].JointType);
                tempJoint.TrackingState = RecalculateJointTrackingState(mergedSkeleton[i].JointType);
                mergedSkeleton[i] = tempJoint;
            }

            needsUpdate = false;
        }
        private bool RecalculateHandGrab(JointType hand)
        {
            if (!(hand == JointType.HandLeft || hand == JointType.HandRight))
            {
                throw new ArgumentOutOfRangeException("hand");
            }

            int validHands = 0;
            int closedHands = 0;

            //Go through all the hands and check if 1) they are tracked, and 2) if they are closed
            for (int i = 0; i < skeletonsToMerge.Count; i++)
            {
                if (skeletonsToMerge[i].SkeletonTrackingState == TrackingState.Tracked)
                {
                    if (skeletonsToMerge[i].skeleton[(int)hand].TrackingState == TrackingState.Tracked)
                    {
                        validHands++;
                        if (skeletonsToMerge[i].rightHandClosed && hand == JointType.HandRight)
                        {
                            closedHands++;
                        }
                        else if (skeletonsToMerge[i].leftHandClosed && hand == JointType.HandLeft)
                        {
                            closedHands++;
                        }
                    }
                }
            }

            //Calculate the percentage of the tracked hands that are closed, if it is greater than 45%, there is a hand grab
            double percent = 0;
            if (validHands > 0)
            {
                percent = (double)closedHands / (double)validHands;
            }

            return percent > handGrabTheshold;
        }
        private Point3D RecalculatePosition()
        {
            Point3D newPosition = new Point3D(0, 0, 0);
            int count = 0;

            for (int i = 0; i < skeletonsToMerge.Count; i++)
            {
                if (skeletonsToMerge[i].SkeletonTrackingState == TrackingState.Tracked || skeletonsToMerge[i].SkeletonTrackingState == TrackingState.PositionOnly)
                {
                    newPosition = HelperMethods.IncAverage(newPosition, skeletonsToMerge[i].Position, count);
                    count++;
                }
            }

            return newPosition;
        }
        private TrackingState RecalculateSkeletonTrackingState()
        {
            TrackingState tempState = TrackingState.NotTracked;

            if (skeletonsToMerge.Exists(element => element.SkeletonTrackingState == TrackingState.Tracked))
            {
                tempState = TrackingState.Tracked;
            }
            else if (skeletonsToMerge.Exists(element => element.SkeletonTrackingState == TrackingState.Inferred))
            {
                tempState = TrackingState.Inferred;
            }
            else if (skeletonsToMerge.Exists(element => element.SkeletonTrackingState == TrackingState.PositionOnly))
            {
                tempState = TrackingState.PositionOnly;
            }

            return tempState;
        }
        private TrackingState RecalculateJointTrackingState(JointType joint)
        {
            TrackingState tempTrackingState = TrackingState.NotTracked;

            if (skeletonsToMerge.Exists(element => element.skeleton[joint].TrackingState == TrackingState.Tracked))
            {
                tempTrackingState = TrackingState.Tracked;
            }
            else if (skeletonsToMerge.Exists(element => element.skeleton[joint].TrackingState == TrackingState.Inferred))
            {
                tempTrackingState = TrackingState.Inferred;
            }

            return tempTrackingState;
        }
        private TrackingConfidence RecalculateJointConfidence(JointType joint)
        {
            TrackingConfidence tempConfidence = TrackingConfidence.Unknown;

            if (skeletonsToMerge.Exists(element => element.skeleton[joint].Confidence == TrackingConfidence.High))
            {
                tempConfidence = TrackingConfidence.High;

            }
            else if (skeletonsToMerge.Exists(element => element.skeleton[joint].Confidence == TrackingConfidence.Low))
            {
                tempConfidence = TrackingConfidence.Low;
            }

            return tempConfidence;
        }
        private Quaternion RecalculateJointOrientation(JointType joint)
        {
            //TODO: Test the quaternion averaging method for correctness
            Quaternion averageQuaternion = Quaternion.Identity;
            int count = 0;

            for (int i = 0; i < skeletonsToMerge.Count; i++)
            {
                if (skeletonsToMerge[i].skeleton[joint].TrackingState == TrackingState.Tracked)
                {
                    if (count == 0)
                    {
                        averageQuaternion = skeletonsToMerge[i].skeleton[joint].Orientation;
                        count++;
                    }
                    else
                    {
                        Quaternion quat = skeletonsToMerge[i].skeleton[joint].Orientation;
                        double aveWeight = count / (count + 1);
                        double quatWeight = 1.0 - aveWeight;

                        double dot = averageQuaternion.W * quat.W + averageQuaternion.X * quat.X + averageQuaternion.Y * quat.Y + averageQuaternion.Z * quat.Z;
                        if (dot != 0)
                        {
                            double z = Math.Sqrt((aveWeight - quatWeight) * (aveWeight - quatWeight) + 4.0 * aveWeight * quatWeight * dot * dot);

                            double tempW = ((aveWeight - quatWeight + z) * averageQuaternion.W + 2 * quatWeight * (dot) * quat.W);
                            double tempX = ((aveWeight - quatWeight + z) * averageQuaternion.X + 2 * quatWeight * (dot) * quat.X);
                            double tempY = ((aveWeight - quatWeight + z) * averageQuaternion.Y + 2 * quatWeight * (dot) * quat.Y);
                            double tempZ = ((aveWeight - quatWeight + z) * averageQuaternion.Z + 2 * quatWeight * (dot) * quat.Z);
                            double size = Math.Sqrt(tempW * tempW + tempX * tempX + tempY * tempY + tempZ * tempZ);
                            averageQuaternion.W = tempW / size;
                            averageQuaternion.X = tempX / size;
                            averageQuaternion.Y = tempY / size;
                            averageQuaternion.Z = tempZ / size;
                            count++;
                        }
                        else //If dot == 0, the two quaternions are directly opposed
                        {
                            double z = Math.Abs(aveWeight - quatWeight);

                            if (aveWeight > quatWeight)
                            {
                                double tempW = ((aveWeight - quatWeight + z) * averageQuaternion.W + 2 * quatWeight * (dot) * quat.W);
                                double tempX = ((aveWeight - quatWeight + z) * averageQuaternion.X + 2 * quatWeight * (dot) * quat.X);
                                double tempY = ((aveWeight - quatWeight + z) * averageQuaternion.Y + 2 * quatWeight * (dot) * quat.Y);
                                double tempZ = ((aveWeight - quatWeight + z) * averageQuaternion.Z + 2 * quatWeight * (dot) * quat.Z);
                                double size = Math.Sqrt(tempW * tempW + tempX * tempX + tempY * tempY + tempZ * tempZ);
                                averageQuaternion.W = tempW / size;
                                averageQuaternion.X = tempX / size;
                                averageQuaternion.Y = tempY / size;
                                averageQuaternion.Z = tempZ / size;
                                count++;

                            }
                            else if (quatWeight > aveWeight)
                            {
                                double tempW = (2 * aveWeight * (dot) * averageQuaternion.W + (quatWeight - aveWeight + z) * quat.W);
                                double tempX = (2 * aveWeight * (dot) * averageQuaternion.X + (quatWeight - aveWeight + z) * quat.X);
                                double tempY = (2 * aveWeight * (dot) * averageQuaternion.Y + (quatWeight - aveWeight + z) * quat.Y);
                                double tempZ = (2 * aveWeight * (dot) * averageQuaternion.Z + (quatWeight - aveWeight + z) * quat.Z);
                                double size = Math.Sqrt(tempW * tempW + tempX * tempX + tempY * tempY + tempZ * tempZ);
                                averageQuaternion.W = tempW / size;
                                averageQuaternion.X = tempX / size;
                                averageQuaternion.Y = tempY / size;
                                averageQuaternion.Z = tempZ / size;
                                count++;
                            }
                            //If the weights are the same, there are an infinite number of possible averages.  So we just don't average anything to handle it cleanly
                        }
                    }
                }
            }            

            return averageQuaternion;
        }
        private Point3D RecalculateJointPosition(JointType joint)
        {
            Point3D averagePoint = new Point3D(0, 0, 0);
            TrackingState jointTempState = TrackingState.NotTracked;
            int count = 0;

            for (int i = 0; i < skeletonsToMerge.Count; i++)
            {
                if (skeletonsToMerge[i].skeleton[joint].TrackingState == TrackingState.Tracked)
                {
                    if (jointTempState == TrackingState.Tracked)
                    {
                        averagePoint = HelperMethods.IncAverage(averagePoint, skeletonsToMerge[i].skeleton[joint].Position, count);
                        count++;
                    }
                    else
                    {
                        averagePoint = skeletonsToMerge[i].skeleton[joint].Position;
                        jointTempState = TrackingState.Tracked;
                        count = 1;
                    }
                }
                else if (skeletonsToMerge[i].skeleton[joint].TrackingState == TrackingState.Inferred)
                {
                    if (jointTempState == TrackingState.Inferred)
                    {
                        averagePoint = HelperMethods.IncAverage(averagePoint, skeletonsToMerge[i].skeleton[joint].Position, count);
                        count++;
                    }
                    else if (jointTempState != TrackingState.Tracked)
                    {
                        averagePoint = skeletonsToMerge[i].skeleton[joint].Position;
                        jointTempState = TrackingState.Inferred;
                        count = 1;
                    }
                }
            }

            return averagePoint;
        }

    }
}
