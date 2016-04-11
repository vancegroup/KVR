using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectV2Core
{
    /// <summary>
    /// Interaction logic for KinectV2SettingsControl.xaml
    /// </summary>
    public partial class KinectV2SettingsControl : UserControl, KinectBase.IKinectSettingsControl
    {
        //Properties required by the interface
        public int? kinectID { get; set; }
        public string uniqueKinectID { get; set; }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV2; }
        }

        //Other public variables
        public UserControl skeletonUserControl;

        //Internal variables for this class
        internal KinectBase.MasterSettings masterSettings;
        internal KinectV2Settings kinectSettings;
        private KinectCoreV2 kinectCore;

        public KinectV2SettingsControl(int kinectNumber, ref KinectBase.MasterSettings settings, KinectBase.IKinectCore kinect)
        {
            if (settings != null)
            {
                if (settings.kinectOptionsList[kinectNumber].version == KinectBase.KinectVersion.KinectV2)
                {
                    masterSettings = settings;
                    dynamic tempSettings = settings.kinectOptionsList[kinectNumber];
                    kinectSettings = (KinectV2Settings)tempSettings;
                    kinectID = kinectNumber;
                    kinectCore = (KinectCoreV2)kinect;
                    uniqueKinectID = kinect.uniqueKinectID;

                    InitializeComponent();
                    this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                }
                else
                {
                    throw new ArgumentException("The provided KinectID is not for a Kinect v2 sensor.");
                }
            }
            else
            {
                throw new NotSupportedException("Method arguments are invalid!");
            }
        }

        public void UpdateGUI(KinectBase.MasterSettings newSettings)
        {
            //TODO: Add the updating logic here
        }

        //TODO: Create the skeleton user control if the users decides to use raw skeletons and destroy it if they decide not to
    }
}
