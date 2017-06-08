using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectWithVRServer
{
    //TODO: FIX THIS!!!
    //This works in principle, but it isn't being agressive enough in getting rid of old skeletons
    //Somehow, when the system gets backed up, we need to clear it
    class ConcurrentSkeletonCollection
    {
        private List<FilteredSkeleton> mainList = new List<FilteredSkeleton>();
        private List<FilteredSkeleton> addCache = new List<FilteredSkeleton>();
        private List<int> removeList = new List<int>();
        object lockObj = new object();
        int refCount = 0;

        public void Add(FilteredSkeleton skeleton)
        {
            lock (lockObj)
            {
                if (refCount == 0)
                {
                    mainList.Add(skeleton);
                }
                else
                {
                    addCache.Add(skeleton);
                }
            }
        }
        public void HoldUpdates()
        {
            lock (lockObj)
            {
                refCount++;
            }
        }
        public void ReleaseForUpdates()
        {
            lock (lockObj)
            {
                refCount--;
                System.Diagnostics.Debug.WriteLine("There are {0} references.", refCount);

                if (refCount == 0)
                {
                    //Remove the requested skeletons
                    removeList.Sort();  //Sort the indices
                    for (int i = 0; i < removeList.Count; i++)   //Remove any duplicates
                    {
                        while (i + 1 < removeList.Count && removeList[i] == removeList[i + 1])
                        {
                            removeList.RemoveAt(i + 1);
                        }
                    }

                    for (int j = removeList.Count - 1; j >= 0; j--)  //Go through and do the removals in reverse order so we don't mess up the indexing
                    {
                        mainList.RemoveAt(removeList[j]);
                    }
                    removeList.Clear();

                    //Copy all the data from the cache to the main list
                    for (int i = 0; i < addCache.Count; i++)
                    {
                        mainList.Add(addCache[i]);
                    }
                    addCache.Clear();
                }
                else if (refCount < 0)
                {
                    throw new Exception("The ref count should never be less than 0!");
                }
            }
        }
        public void RemoveAt(int idx)
        {
            lock (lockObj)
            {
                if (refCount == 0)
                {
                    mainList.RemoveAt(idx);
                }
                else
                {
                    removeList.Add(idx);
                }
            }
        }
        public int Count
        {
            get { return mainList.Count; }
        }
        public FilteredSkeleton this[int index]
        {
            get
            {
                return mainList[index];
            }
        }
    }
}
