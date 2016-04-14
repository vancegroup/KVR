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
using NetworkKinectCore;

namespace KinectWithVRServer.NetworkKinectWrapper
{
    public partial class SettingsControl : UserControl, IKinectSettingsControl
    {
        //Private variable to manage the wrapped settings control
        private NetworkKinectSettingsControl realControl;

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

        //Public properties specific to the network Kinect

        //Public methods required by the IKinectSettingsControl interface
        public void UpdateGUI(MasterSettings newSettings)
        {
            realControl.UpdateGUI(newSettings);
        }

        //Constructor to setup the real NetworkKinectSettingsControl
        public SettingsControl(int kinectNumber, ref MasterSettings settings, IKinectCore kinect)
        {
            InitializeComponent();
            Grid.SetColumn(this, 2);
            this.Visibility = System.Windows.Visibility.Collapsed;

            Core coreWrapper = (Core)kinect;
            NetworkKinectCore.NetworkKinectCore kinectCore = (NetworkKinectCore.NetworkKinectCore)coreWrapper;
            realControl = new NetworkKinectSettingsControl(kinectNumber, ref settings, kinectCore);
            realControl.Visibility = System.Windows.Visibility.Visible;
            this.MasterGrid.Children.Add((UserControl)realControl);
        }
    }
}
