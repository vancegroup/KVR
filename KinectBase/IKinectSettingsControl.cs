using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectBase
{
    public interface IKinectSettingsControl
    {
        int? kinectID { get; set; }
        KinectVersion version { get; }
        string uniqueKinectID { get; set; }

        //A function to update all the GUI settings based on the server settings
        void UpdateGUI(MasterSettings newSettings);
    }

    public class KinectSettingsControlComparer : IComparer<IKinectSettingsControl>
    {
        //Sorts the Kinect settings GUI controls by kinectID with null IDs having the HIGHEST value
        //This is so the index in the array of the pages will match the Kinect number, with the unused ones at the end
        public int Compare(IKinectSettingsControl x, IKinectSettingsControl y)
        {
            if (x.kinectID.HasValue)
            {
                if (y.kinectID.HasValue)
                {
                    int tempX = x.kinectID.Value;
                    int tempY = y.kinectID.Value;
                    return tempX.CompareTo(tempY);
                }
                else
                {
                    return -1; //If x has a value but y is null, then y is greater
                }
            }
            else
            {
                if (y.kinectID.HasValue)
                {
                    return 1; //If x is null amd y isn't, then x is greater 
                }
                else
                {
                    return 0; //If both are null, then they are equal
                }
            }
        }
    }
}
