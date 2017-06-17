using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace KinectWithVRServer
{
    class KMeans
    {
        public static List<Point3D> FindKMeanCentroids(List<Point3D> data, int clusters)
        {
            #region Initialize the centroids semi-randomly
            //Initialize variables
            Point3D mins = new Point3D(double.MaxValue, double.MaxValue, double.MaxValue);
            Point3D maxes = new Point3D(double.MinValue, double.MinValue, double.MinValue);
            Random rand = new Random();

            //Find the extents of the data
            for (int i = 0; i < data.Count; i++)
            {
                //Check the X values
                if (mins.X > data[i].X)
                {
                    mins.X = data[i].X;
                }
                if (maxes.X < data[i].X)
                {
                    maxes.X = data[i].X;
                }

                //Check the Y values
                if (mins.Y > data[i].Y)
                {
                    mins.Y = data[i].Y;
                }
                if (maxes.Y < data[i].Y)
                {
                    maxes.Y = data[i].Y;
                }

                //Check the Z values
                if (mins.Z > data[i].Z)
                {
                    mins.Z = data[i].Z;
                }
                if (maxes.Z < data[i].Z)
                {
                    maxes.Z = data[i].Z;
                }
            }

            List<Point3D> centroids = new List<Point3D>(clusters);
            for (int i = 0; i < clusters; i++)
            {
                Point3D temp = new Point3D();
                temp.X = rand.NextDouble() * (maxes.X - mins.X) + mins.X;
                temp.Y = rand.NextDouble() * (maxes.Y - mins.Y) + mins.Y;
                temp.Z = rand.NextDouble() * (maxes.Z - mins.Z) + mins.Z;

                centroids.Add(temp);

                //Verify that no cluster centers are the same
                for (int k = i - 1; k >= 0; k--)
                {
                    bool same = true;

                    if (centroids[k].X != centroids[i].X)
                    {
                        same = false;
                    }
                    else if (centroids[k].Y != centroids[i].Y)
                    {
                        same = false;
                    }
                    else if (centroids[k].Z != centroids[i].Z)
                    {
                        same = false;
                    }

                    if (same)
                    {
                        centroids.RemoveAt(i);
                        i--;
                        break;
                    }
                }
            }
            #endregion

            List<List<Point3D>> clusterPoints = new List<List<Point3D>>();
            for (int i = 0; i < clusters; i++)
            {
                clusterPoints.Add(new List<Point3D>());
            }


            double posDiff = 1;
            while (posDiff > 0)
            {
                //Clear the old cluster points
                for (int i = 0; i < clusters; i++)
                {
                    clusterPoints[i].Clear();
                }

                //Assign the points to their clusters
                for (int i = 0; i < data.Count; i++)
                {
                    int index = FindNearestCluster(centroids, data[i]);
                    clusterPoints[index].Add(data[i]);
                }

                //Check that all clusters have at least 1 point, if not, take 2 random points and put them in the cluster
                for (int i = 0; i < clusters; i++)
                {
                    if (clusterPoints[i].Count < 1)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int randCluster = rand.Next(0, clusters);
                            if (clusterPoints[randCluster].Count > 1 && randCluster != i)
                            {
                                int randPoint = rand.Next(0, clusterPoints[randCluster].Count);
                                clusterPoints[i].Add(clusterPoints[randCluster][randPoint]);
                                clusterPoints[randCluster].RemoveAt(randPoint);
                            }
                            else
                            {
                                j--;
                            }
                        }
                    }
                }

                //Check that all clusters have at least one point and recalculate the centroids
                posDiff = 0;
                for (int i = 0; i < clusters; i++)
                {
                    Point3D newCentroid = FindCentroid(clusterPoints[i]);
                    posDiff += FindDistance(newCentroid, centroids[i]);
                    centroids[i] = newCentroid;
                }
            }

            return centroids;
        }

        public static int FindNearestCluster(List<Point3D> clusterCentroids, Point3D point)
        {
            int nearest = 0;
            double nearestDistance = double.MaxValue;

            for (int i = 0; i < clusterCentroids.Count; i++)
            {
                double newDistance = FindDistance(clusterCentroids[i], point);

                if (nearestDistance > newDistance)
                {
                    nearestDistance = newDistance;
                    nearest = i;
                }
            }

            return nearest;
        }

        public static double FindDistance(Point3D vecA, Point3D vecB)
        {
            Vector3D vec = vecB - vecA;
            return vec.Length;
        }

        public static Point3D FindCentroid(List<Point3D> clusterPoints)
        {
            Point3D centroid = new Point3D();

            double averageX = 0.0;
            double averageY = 0.0;
            double averageZ = 0.0;
            for (int j = 0; j < clusterPoints.Count; j++)
            {
                averageX += (clusterPoints[j].X - averageX) / (j + 1);
                averageY += (clusterPoints[j].Y - averageY) / (j + 1);
                averageZ += (clusterPoints[j].Z - averageZ) / (j + 1);
            }

            centroid.X = averageX;
            centroid.Y = averageY;
            centroid.Z = averageZ;
            return centroid;
        }
    }
}
