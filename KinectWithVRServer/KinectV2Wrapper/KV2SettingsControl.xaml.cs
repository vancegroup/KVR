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
using KinectBase;
using KinectV2Core;

namespace KinectWithVRServer.KinectV2Wrapper
{
    /// <summary>
    /// Interaction logic for KV2SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl, IKinectSettingsControl
    {
        //Private variable to manage the wrapping
        private KinectV2SettingsControl realControl;

        //Public properties required by the IKinectSettingsControl interface
        public int? kinectID
        {
            get { return realControl.kinectID; }
            set { realControl.kinectID = value; }
        }
        public KinectVersion version
        {
            get { return realControl.version; }
        }
        public string uniqueKinectID
        {
            get { return realControl.uniqueKinectID; }
            set { realControl.uniqueKinectID = value; }
        }

        //Public properties specific to the Kinect v2
        public UserControl skeletonUserControl
        {
            get { return realControl.skeletonUserControl; }
            set { realControl.skeletonUserControl = value; }
        }

        //Public methods required by the IKinectSettingsControl interface
        public void UpdateGUI(MasterSettings newSettings)
        {
            realControl.UpdateGUI(newSettings);
        }

        //Constructor to setup the real KinectV2SettingsControl
        public SettingsControl(int kinectNumber, ref MasterSettings settings, IKinectCore kinect)
        {
            InitializeComponent();
            Grid.SetColumn(this, 2);
            this.Visibility = System.Windows.Visibility.Collapsed;

            Core coreWrapper = (Core)kinect;
            KinectCoreV2 kinectCore = (KinectCoreV2)coreWrapper;
            realControl = new KinectV2SettingsControl(kinectNumber, ref settings, kinectCore);
            realControl.Visibility = System.Windows.Visibility.Visible;
            this.MasterGrid.Children.Add((UserControl)realControl);
        }
    }
}
