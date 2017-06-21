using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using KinectBase;

namespace KinectWithVRServer
{
    //TODO: The actual training and recognition parts of this need to happen in a seperate thread
    //If we try to run this all on the main thread, things will get bogged down, badly
    internal class GestureCore
    {
        private bool running = false;
        private List<GestureRecognizer> recognizers = new List<GestureRecognizer>();
        private List<bool> inGesture = new List<bool>();
        private MasterSettings masterSettings;
        internal event GestureRecognizedEventHandler GestureRecognizer;

        internal GestureCore(ref MasterSettings serverMasterSettings)
        {
            masterSettings = serverMasterSettings;
        }

        //Note, this is fairly processor intensive, try to run in a background thread if possible
        internal void TrainGesture(List<List<KinectSkeleton>> trainingData, int gestureNumber)
        {
            if (gestureNumber < masterSettings.gestureCommands.Count)
            {
                recognizers[gestureNumber].TrainGesture(trainingData, masterSettings.gestureCommands[gestureNumber].monitoredJoint);
                masterSettings.gestureCommands[gestureNumber].isTrained = true;
            }
        }

        //Note, this is fairly processor intensive, try to run in a background thread if possible
        internal void UpdateRecognizer(KinectSkeleton latestSkeleton)
        {
            if (running )
            {
                for (int i = 0; i < masterSettings.gestureCommands.Count; i++)
                {
                    if (latestSkeleton.TrackingId == masterSettings.gestureCommands[i].trackedSkeleton)
                    {
                        recognizers[i].AddDataPoint(latestSkeleton, masterSettings.gestureCommands[i].monitoredJoint);
                        double relativeProb = recognizers[i].TestGesture(masterSettings.gestureCommands[i].sensitivity);
                        if (relativeProb < 1.0)
                        {
                            if (!inGesture[i])
                            {
                                inGesture[i] = true;
                                GestureRecognizedEventArgs args = new GestureRecognizedEventArgs();
                                args.UtcTime = DateTime.UtcNow;
                                args.GestureName = masterSettings.gestureCommands[i].gestureName;
                                OnGestureRecognized(args);
                            }
                        }
                        else
                        {
                            if (inGesture[i])
                            {
                                inGesture[i] = false;
                            }
                        }
                    }
                }

            }
        }

        internal double TestRecognizer(KinectSkeleton latestSkeleton, int testNumber)
        {
            if (testNumber >= 0 && testNumber < recognizers.Count)
            {
                recognizers[testNumber].AddDataPoint(latestSkeleton, masterSettings.gestureCommands[testNumber].monitoredJoint);
                return recognizers[testNumber].TestGesture(masterSettings.gestureCommands[testNumber].sensitivity);
            }
            else
            {
                return double.PositiveInfinity;
            }
        }

        internal void StartRecognizer()
        {
            running = true;
        }

        internal void StopRecognizer()
        {
            running = false;
        }

        internal void AddGesture()
        {
            //GestureRecognizer recog = new GestureRecognizer(HMMModel.LeftToRight);
            GestureRecognizer recog = new GestureRecognizer(HMMModel.LeftToRight2);
            recognizers.Add(recog);
            inGesture.Add(false);
        }

        internal void RemoveGesture(int index)
        {
            recognizers.RemoveAt(index);
            inGesture.RemoveAt(index);
        }

        private void OnGestureRecognized(GestureRecognizedEventArgs e)
        {
            if (GestureRecognizer != null)
            {
                GestureRecognizer(this, e);
            }
        }
    }

    internal class GestureRecognizer
    {
        private DiscreteHMM<int, int> hmm;
        private double shoulderWidth = 0.0;
        private double shoulderCovariance = 10000;
        private double sensorStd = 0.01;
        private double actualStd = 0.01;
        private DateTime utcLastTime;
        private int[] symbols = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        private int[] states = { 0, 1, 2, 4, 5 };
        private List<Point3D> kCentroids = new List<Point3D>();
        private double rawLogThreshold = 0.0;
        private Queue<int> skeletonHistory = new Queue<int>();
        private int sequenceLength = 0;

        internal GestureRecognizer(HMMModel model)
        {
            hmm = new DiscreteHMM<int, int>(model, symbols, states);
        }

        internal void TrainGesture(List<List<KinectSkeleton>> trainingData, JointType joint)
        {
            List<List<Point3D>> normalizedTrainingData = new List<List<Point3D>>();

            //Normalize all the training data.  We average the data per skeleton sequence in case it was trained with multiple people
            for (int i = 0; i < trainingData.Count; i++)
            {
                //Find the average shoulder width
                double tempShoulderWidth = 0.0;
                int n = 0;

                for (int j = 0; j < trainingData[i].Count; j++)
                {
                    if (trainingData[i][j].skeleton[JointType.ShoulderRight].TrackingState == TrackingState.Tracked && trainingData[i][j].skeleton[JointType.ShoulderLeft].TrackingState == TrackingState.Tracked)
                    {
                        //Calculate the distance between the shoulders
                        double temp = (trainingData[i][j].skeleton[JointType.ShoulderRight].Position - trainingData[i][j].skeleton[JointType.ShoulderLeft].Position).Length;

                        tempShoulderWidth += (temp - tempShoulderWidth) / (double)(n + 1);
                        n++;
                    }
                }

                //Normalize this skeleton sequence
                normalizedTrainingData.Add(new List<Point3D>());
                for (int j = 0; j < trainingData[i].Count; j++)
                {
                    normalizedTrainingData[i].Add(GetNormalizedRelativePosition(trainingData[i][j].skeleton, joint, tempShoulderWidth));
                }
            }

            //Find the K-mean centroids
            List<Point3D> combinedData = new List<Point3D>();
            for (int i = 0; i < normalizedTrainingData.Count; i++)
            {
                combinedData.AddRange(normalizedTrainingData[i]);
            }
            List<Point3D> centroids = KMeans.FindKMeanCentroids(combinedData, symbols.Length);

            //Convert the training data to lists of cluster numbers
            List<int[]> clusteredTrainingData = new List<int[]>();
            for (int i = 0; i < normalizedTrainingData.Count; i++)
            {
                clusteredTrainingData.Add(new int[normalizedTrainingData[i].Count]);

                for (int j = 0; j < clusteredTrainingData[i].Length; j++)
                {
                    clusteredTrainingData[i][j] = KMeans.FindNearestCluster(centroids, normalizedTrainingData[i][j]);
                }
            }

            //Train the HMM on the clustered data
            hmm.TrainHMMScaled(clusteredTrainingData);
            kCentroids = centroids;

            //Test the trained HMM against the training data to determine what the threshold should be
            double aveProb = 0.0;
            for (int i = 0; i < clusteredTrainingData.Count; i++)
            {
                double logProb = hmm.LogFastObservationSequenceProbability(clusteredTrainingData[i]);
                aveProb += (logProb - aveProb) / (double)(i + 1);
            }
            rawLogThreshold = aveProb * 2.0; //Set the baseline threshold to twice the average probility
            //Note: the probabilities logs of small numbers, so they are negative values (~-25 normally)
            //That means that a higher multiplier will make it more sensitive, a lower number less sensitive


            //Determine the length of sequence to keep
            double averageSequenceLength = 0;
            double moment2 = 0.0;
            for (int i = 0; i < trainingData.Count; i++)
            {
                double delta = (double)trainingData[i].Count - averageSequenceLength;
                averageSequenceLength += delta / (double)(i + 1);
                moment2 += delta * ((double)trainingData[i].Count - averageSequenceLength);
            }
            double stdDev = Math.Sqrt(moment2 / (double)(trainingData.Count - 1));
            sequenceLength = (int)Math.Floor(averageSequenceLength);
            if (double.IsNaN(stdDev))
            {
                sequenceLength = (int)Math.Ceiling(averageSequenceLength);
            }
            else
            {
                sequenceLength = (int)Math.Ceiling(averageSequenceLength + 2 * stdDev);
            }
        }

        internal Point3D GetNormalizedRelativePosition(SkeletonData skeleton, JointType joint, double localShoulderWidth = double.NaN)
        {
            if (double.IsNaN(localShoulderWidth))
            {
                localShoulderWidth = shoulderWidth;
            }

            Point3D temp = (Point3D)(skeleton[joint].Position - skeleton[JointType.ShoulderCenter].Position);
            temp = Point3D.Multiply(temp, skeleton[joint].Orientation.orientationMatrix);
            return new Point3D(temp.X / localShoulderWidth, temp.Y / localShoulderWidth, temp.Z / localShoulderWidth);
            //return skeleton[joint].Position;
        }

        internal void AddDataPoint(KinectSkeleton data, JointType joint)
        {
            if (data.skeleton[joint].TrackingState == TrackingState.Tracked)
            {
                if (sequenceLength != 0) //Don't bother adding stuff to the sequence if its length should be 0 anyway
                {
                    UpdateShoulderWidth(data.skeleton[JointType.ShoulderRight], data.skeleton[JointType.ShoulderLeft]);
                    if (skeletonHistory.Count >= sequenceLength)
                    {
                        skeletonHistory.Dequeue();
                    }
                    int clusterNumber = KMeans.FindNearestCluster(kCentroids, GetNormalizedRelativePosition(data.skeleton, joint));
                    skeletonHistory.Enqueue(clusterNumber);
                }
            }
        }

        internal double TestGesture(double sensitivity)
        {
            if (skeletonHistory.Count > 0.5 * sequenceLength)
            {
                double logProb = hmm.LogFastObservationSequenceProbability(skeletonHistory.ToArray());
                return logProb / (rawLogThreshold * sensitivity);  //This returns the normalized probability relative to the threshold, smaller numbers mean more probable, < 1.0 means it is above the threshold, >1.0 is below the probability threshold
            }
            else
            {
                return double.PositiveInfinity;
            }
        }

        private void UpdateShoulderWidth(Joint rightShoulder, Joint leftShoulder)
        {
            if (rightShoulder.TrackingState == TrackingState.Tracked && leftShoulder.TrackingState == TrackingState.Tracked)
            {
                DateTime utcNow = DateTime.UtcNow;
                double tempWidth = (rightShoulder.Position - leftShoulder.Position).Length;

                //Update the shoulder width using a 1D constant Kalman filter, with time dependent covariances
                double predCovar = 10000;
                if (skeletonHistory.Count != 0)
                {
                    predCovar = shoulderCovariance + (utcNow - utcLastTime).TotalSeconds * actualStd * actualStd;
                }
                double resid = tempWidth - shoulderWidth;
                double residCovar = predCovar + sensorStd * sensorStd;
                double kalmanGain = predCovar * (1.0 / residCovar);
                shoulderWidth = shoulderWidth + kalmanGain * resid;
                shoulderCovariance = (1 - kalmanGain) * predCovar;
                utcLastTime = utcNow;

                System.Diagnostics.Trace.WriteLine("Width: " + shoulderWidth.ToString());
            }
        }
    }

    internal delegate void GestureRecognizedEventHandler(object sender, GestureRecognizedEventArgs e);
    internal class GestureRecognizedEventArgs
    {
        internal string GestureName { get; set; }
        internal DateTime UtcTime { get; set; }
    }
}
