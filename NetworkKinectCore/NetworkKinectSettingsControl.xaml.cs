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
using System.Timers;

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
        private Timer guiUpdateTimer;
        private bool hasNewData = false;

        //TODO: Subscribe this to the skeletonChanged event and update the GUI with a preview of the skeleton positions
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

                    //Set the binding on the joint mapping data grid
                    jointMappingDataGrid.ItemsSource = kinectSettings.jointMappings;
                    jointMappingDataGrid.Items.Refresh();

                    //Subscribe to the skeletonChanged event
                    kinectCore.SkeletonChanged += kinectCore_SkeletonChanged;

                    guiUpdateTimer = new Timer();
                    guiUpdateTimer.Interval = 1000;
                    guiUpdateTimer.AutoReset = true;
                    guiUpdateTimer.Elapsed += guiUpdateTimer_Elapsed;
                    guiUpdateTimer.Start();
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

        void guiUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (hasNewData)
            {
                this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Normal, (System.Windows.Threading.DispatcherOperationCallback)delegate(object arg)
                {
                    jointMappingDataGrid.Items.Refresh();
                    return null;
                }, null);
            }
            hasNewData = false;
        }

        void kinectCore_SkeletonChanged(object sender, KinectBase.SkeletonEventArgs e)
        {
            for (int i = 0; i < e.skeletons[0].skeleton.Count; i++)
            {
                if (e.skeletons[0].skeleton[i].TrackingState == KinectBase.TrackingState.Tracked)
                {
                    for (int j = 0; j < kinectCore.masterKinectSettings.jointMappings.Count; j++)
                    {
                        if (e.skeletons[0].skeleton[i].JointType == kinectCore.masterKinectSettings.jointMappings[j].joint)
                        {
                            kinectCore.masterKinectSettings.jointMappings[j].lastPosition = e.skeletons[0].skeleton[i].Position;
                            break;
                        }
                    }
                }
            }

            hasNewData = true;
        }

        public void UpdateGUI(KinectBase.MasterSettings settings)
        {
            if (kinectID.HasValue)
            {
                masterSettings = settings;
                dynamic tempSettings = masterSettings.kinectOptionsList[kinectID.Value];
                kinectSettings = (NetworkKinectSettings)tempSettings;


                //Shutdown the server, if it is running
                bool wasRunning = false;
                if (kinectCore.isKinectRunning)
                {
                    kinectCore.ShutdownSensor();
                    wasRunning = true;
                }

                //Update the skeleton server and joint mappings
                serverNameTextBox.Text = kinectSettings.serverName;
                jointMappingDataGrid.ItemsSource = kinectSettings.jointMappings;
                jointMappingDataGrid.Items.Refresh();

                //Update the physical position
                xPosTextBox.Text = kinectSettings.kinectPosition.X.ToString();
                yPosTextBox.Text = kinectSettings.kinectPosition.Y.ToString();
                zPosTextBox.Text = kinectSettings.kinectPosition.Z.ToString();
                yawTextBox.Text = kinectSettings.kinectYaw.ToString();
                pitchTextBox.Text = kinectSettings.kinectPitch.ToString();
                rollTextBox.Text = kinectSettings.kinectRoll.ToString();

                //Update the hand grab data
                lhServerTextBox.Text = kinectSettings.lhServerName;
                lhChannelTextBox.Text = kinectSettings.lhChannel.ToString();
                rhServerTextBox.Text = kinectSettings.rhServerName;
                rhChannelTextBox.Text = kinectSettings.rhChannel.ToString();

                //Restart the server if it was running to begin with
                if (wasRunning)
                {
                    kinectCore.StartNetworkKinect();
                }
            }
        }

        private void serverNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            kinectSettings.serverName = serverNameTextBox.Text;
        }

        #region Kinect Position Methods
        private void xPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(xPosTextBox.Text, out temp))
            {
                if (kinectSettings.kinectPosition == null)
                {
                    kinectSettings.kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = kinectSettings.kinectPosition;
                tempPoint.X = temp;
                kinectSettings.kinectPosition = tempPoint;
            }
        }
        private void yPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(yPosTextBox.Text, out temp))
            {
                if (kinectSettings.kinectPosition == null)
                {
                    kinectSettings.kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = kinectSettings.kinectPosition;
                tempPoint.Y = temp;
                kinectSettings.kinectPosition = tempPoint;
            }
        }
        private void zPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(zPosTextBox.Text, out temp))
            {
                if (kinectSettings.kinectPosition == null)
                {
                    kinectSettings.kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = kinectSettings.kinectPosition;
                tempPoint.Z = temp;
                kinectSettings.kinectPosition = tempPoint;
            }
        }
        private void yawTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(yawTextBox.Text, out temp))
            {
                kinectSettings.kinectYaw = temp;
            }
        }
        private void pitchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(pitchTextBox.Text, out temp))
            {
                kinectSettings.kinectPitch = temp;
            }
        }
        private void rollTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(rollTextBox.Text, out temp))
            {
                kinectSettings.kinectRoll = temp;
            }
        }
        #endregion

        #region Hand Mapping Methods
        private void lhServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            kinectSettings.lhServerName = lhServerTextBox.Text;
        }
        private void lhChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(lhChannelTextBox.Text, out temp))
            {
                kinectSettings.lhChannel = temp;
            }
        }
        private void rhServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            kinectSettings.rhServerName = rhServerTextBox.Text;
        }
        private void rhChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(rhChannelTextBox.Text, out temp))
            {
                kinectSettings.rhChannel = temp;
            }
        }
        #endregion

        #region Misc. Methods
        //Rejects any points that are not numbers or control characters or a period
        private void floatNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key) && e.Key != Key.OemPeriod && e.Key != Key.Decimal)
            {
                e.Handled = true;
            }
        }
        //Rejects any points that are not numbers or control charactes
        private void intNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key))
            {
                e.Handled = true;
            }
        }
        #endregion

        private void connectButton_Click(object sender, RoutedEventArgs e)
        {
            if (kinectCore.isKinectRunning)
            {
                kinectCore.ShutdownSensor();
                connectButton.Content = "Connect";
            }
            else
            {
                kinectCore.StartNetworkKinect();
                connectButton.Content = "Disconnect";
            }
        }
    }
}
