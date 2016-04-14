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

namespace NetworkKinectCore
{
    /// <summary>
    /// Interaction logic for NetworkKinectSettingsControl.xaml
    /// </summary>
    public partial class NetworkKinectSettingsControl : UserControl, KinectBase.IKinectSettingsControl
    {
        //Public properties required by the interface
        public int? kinectID { get; set; }
        public string uniqueKinectID { get; set; }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.NetworkKinect; }
        }

        //Variables for managing other stuff
        private KinectBase.MasterSettings masterSettings;
        private NetworkKinectSettings kinectSettings;
        private NetworkKinectCore kinectCore;

        public NetworkKinectSettingsControl(int kinectNumber, ref KinectBase.MasterSettings settings, KinectBase.IKinectCore kinect)
        {
            if (settings != null)
            {
                if (settings.kinectOptionsList[kinectNumber].version == KinectBase.KinectVersion.NetworkKinect)
                {
                    masterSettings = settings;
                    dynamic tempSettings = settings.kinectOptionsList[kinectNumber];
                    kinectSettings = (NetworkKinectSettings)tempSettings;
                    kinectID = kinectNumber;
                    kinectCore = (NetworkKinectCore)kinect;
                    uniqueKinectID = kinect.uniqueKinectID;

                    InitializeComponent();

                    this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                }
                else
                {
                    throw new ArgumentException("The provided KinectID is not for a network Kinect sensor.");
                }
            }
            else
            {
                throw new ArgumentNullException("settings");
            }
        }

        public void UpdateGUI(KinectBase.MasterSettings settings)
        {

        }
    }
}
