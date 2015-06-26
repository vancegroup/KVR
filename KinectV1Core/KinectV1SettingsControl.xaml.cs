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

namespace KinectV1Core
{
    /// <summary>
    /// Interaction logic for KinectV1SettingsControl.xaml
    /// </summary>
    public partial class KinectV1SettingsControl : UserControl, KinectBase.IKinectSettingsControl
    {
        public int? kinectID { get; set; }
        public string uniqueKinectID { get; set; }
        public KinectBase.KinectVersion version
        {
            get { return KinectBase.KinectVersion.KinectV1; }
        }
        public UserControl skeletonUserControl;
        internal KinectBase.MasterSettings masterSettings;
        private KinectV1Settings kinectSettings;
        private KinectCoreV1 kinectCore;


        public KinectV1SettingsControl(int kinectNumber, ref KinectBase.MasterSettings settings, KinectBase.IKinectCore kinect)
        {
            if (settings != null)
            {
                if (settings.kinectOptionsList[kinectNumber].version == KinectBase.KinectVersion.KinectV1)
                {
                    masterSettings = settings;
                    kinectSettings = (KinectV1Settings)settings.kinectOptionsList[kinectNumber];
                    kinectID = kinectNumber;
                    kinectCore = (KinectCoreV1)kinect;
                    kinectCore.AccelerationChanged += kinectCore_AccelerationChanged;
                    uniqueKinectID = kinect.uniqueKinectID;

                    InitializeComponent();

                    this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                    Grid.SetColumn(this, 2);
                    this.Visibility = System.Windows.Visibility.Collapsed;

                    if (kinectCore.isXbox360Kinect != null && (bool)kinectCore.isXbox360Kinect)
                    {
                        DisableK4WOnlyOptions();
                    }
                }
                else
                {
                    throw new ArgumentException("The provided KienctID is not for a Kinect v1 sensor.");
                }
            }
            else
            {
                throw new NotSupportedException("Method arguements are invalid!");
            }
        }

        public void UpdateGUI(KinectBase.MasterSettings newSettings)
        {
            if (kinectID.HasValue)
            {
                masterSettings = newSettings;
                kinectSettings = (KinectV1Settings)masterSettings.kinectOptionsList[kinectID.Value];

                //Update the color options
                switch (kinectSettings.colorImageMode)
                {
                    case Microsoft.Kinect.ColorImageFormat.RgbResolution640x480Fps30:
                    {
                        colorResComboBox.SelectedIndex = 0;
                        break;
                    }
                    case Microsoft.Kinect.ColorImageFormat.RgbResolution1280x960Fps12:
                    {
                        colorResComboBox.SelectedIndex = 1;
                        break;
                    }
                    case Microsoft.Kinect.ColorImageFormat.InfraredResolution640x480Fps30:
                    {
                        colorResComboBox.SelectedIndex = 2;
                        break;
                    }
                    default:
                    {
                        //If we don't know how to handle a mode, lets just shut it off
                        colorResComboBox.SelectedIndex = 3;
                        break;
                    }
                }

                //Update the skeleton settings
                UseSkeletonCheckBox.IsChecked = kinectSettings.mergeSkeletons;
                UseRawSkeletonCheckBox.IsChecked = kinectSettings.sendRawSkeletons;
                XFormRawSkeletonCheckBox.IsChecked = kinectSettings.transformRawSkeletons;
                SeatedModeCheckBox.IsChecked = kinectSettings.rawSkeletonSettings.isSeatedMode;

                //Update the depth settings
                irOnCheckBox.IsChecked = kinectSettings.irON;
                nearModeCheckBox.IsChecked = kinectSettings.isNearMode;
                switch (kinectSettings.depthImageMode)
                {
                    case Microsoft.Kinect.DepthImageFormat.Resolution320x240Fps30:
                    {
                        depthResComboBox.SelectedIndex = 1;
                        break;
                    }
                    case Microsoft.Kinect.DepthImageFormat.Resolution80x60Fps30:
                    {
                        depthResComboBox.SelectedIndex = 2;
                        break;
                    }
                    default:
                    {
                        depthResComboBox.SelectedIndex = 0;
                        break;
                    }
                }

                //Update the audio settings
                SendSoundAngleCheckBox.IsChecked = kinectSettings.sendAudioAngle;
                audioServerTextBox.Text = kinectSettings.audioAngleServerName;
                audioChannelTextBox.Text = kinectSettings.audioAngleChannel.ToString();
                audioBeamSkeletonNumberTextBox.Text = kinectSettings.audioBeamTrackSkeletonNumber.ToString();
                switch (kinectSettings.audioTrackMode)
                {
                    case KinectBase.AudioTrackingMode.Feedback:
                    {
                        audioBeamModeComboBox.SelectedIndex = 1;
                        break;
                    }
                    case KinectBase.AudioTrackingMode.MergedSkeletonX:
                    {
                        audioBeamModeComboBox.SelectedIndex = 2;
                        break;
                    }
                    case KinectBase.AudioTrackingMode.LocalSkeletonX:
                    {
                        audioBeamModeComboBox.SelectedIndex = 3;
                        break;
                    }
                    default:
                    {
                        audioBeamModeComboBox.SelectedIndex = 0;
                        break;
                    }
                }

                //Update the acceleration options
                SendAccelCheckBox.IsChecked = kinectSettings.sendAcceleration;
                accelServerTextBox.Text = kinectSettings.accelerationServerName;
                accelXChannelTextBox.Text = kinectSettings.accelXChannel.ToString();
                accelYChannelTextBox.Text = kinectSettings.accelYChannel.ToString();
                accelZChannelTextBox.Text = kinectSettings.accelZChannel.ToString();

                //Update the position options
                xPosTextBox.Text = kinectSettings.kinectPosition.X.ToString();
                yPosTextBox.Text = kinectSettings.kinectPosition.Y.ToString();
                zPosTextBox.Text = kinectSettings.kinectPosition.Z.ToString();
                yawPosTextBox.Text = kinectSettings.kinectYaw.ToString();
            }
        }

        void kinectCore_AccelerationChanged(object sender, KinectBase.AccelerationEventArgs e)
        {
            //Don't bother calling the disptacher (which is kind of expensive) if the control is hidden
            if (IsInitialized && Visibility == System.Windows.Visibility.Visible)  
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.acceleration.HasValue)
                    {
                        AccelXTextBlock.Text = e.acceleration.Value.X.ToString("F2");
                        AccelYTextBlock.Text = e.acceleration.Value.Y.ToString("F2");
                        AccelZTextBlock.Text = e.acceleration.Value.Z.ToString("F2");
                    }
                    else
                    {
                        AccelXTextBlock.Text = "N/A";
                        AccelYTextBlock.Text = "N/A";
                        AccelZTextBlock.Text = "N/A";
                    }

                    if (e.elevationAngle.HasValue)
                    {
                        AngleTextBlock.Text = e.elevationAngle.Value.ToString();
                    }
                    else
                    {
                        AngleTextBlock.Text = "N/A";
                    }
                }), null);
            }
        }

        #region Acceleration Option Methods
        private void SendAccelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectSettings.sendAcceleration = true;

            //Enable the server boxes
            accelXChannelTextBox.IsEnabled = true;
            accelYChannelTextBox.IsEnabled = true;
            accelZChannelTextBox.IsEnabled = true;
            accelServerTextBox.IsEnabled = true;
        }
        private void SendAccelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectSettings.sendAcceleration = false;

            //Disable the server boxes
            accelXChannelTextBox.IsEnabled = false;
            accelYChannelTextBox.IsEnabled = false;
            accelZChannelTextBox.IsEnabled = false;
            accelServerTextBox.IsEnabled = false;
        }
        private void accelServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            kinectSettings.accelerationServerName = accelServerTextBox.Text;
        }
        private void accelXChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelXChannelTextBox.Text, out temp))
            {
                kinectSettings.accelXChannel = temp;
            }
        }
        private void accelYChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelYChannelTextBox.Text, out temp))
            {
                kinectSettings.accelYChannel = temp;
            }
        }
        private void accelZChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelZChannelTextBox.Text, out temp))
            {
                kinectSettings.accelZChannel = temp;
            }
        }
        #endregion

        #region Audio Option Methods
        private void SendSoundAngleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectSettings.sendAudioAngle = true;

            //Enable the server boxes
            audioServerTextBox.IsEnabled = true;
            audioChannelTextBox.IsEnabled = true;
        }
        private void SendSoundAngleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            kinectSettings.sendAudioAngle = false;

            //Disable the server boxes
            audioServerTextBox.IsEnabled = false;
            audioChannelTextBox.IsEnabled = false;
        }
        private void audioServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            kinectSettings.audioAngleServerName = audioServerTextBox.Text;
        }
        private void audioChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(audioChannelTextBox.Text, out temp))
            {
                kinectSettings.audioAngleChannel = temp;
            }
        }
        private void audioBeamModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (kinectID != null)
            {
                if (audioBeamModeComboBox.SelectedIndex == 0) //Loudest source mode
                {
                    if (audioBeamSkeletonNumberTextBox != null)
                    {
                        audioBeamSkeletonNumberTextBox.IsEnabled = false;
                    }

                    kinectSettings.audioTrackMode = KinectBase.AudioTrackingMode.Loudest;
                    if (kinectCore.kinect.AudioSource != null)
                    {
                        kinectCore.kinect.AudioSource.BeamAngleMode = Microsoft.Kinect.BeamAngleMode.Automatic;
                    }
                }
                else if (audioBeamModeComboBox.SelectedIndex == 1) //Feedback position mode
                {
                    if (audioBeamSkeletonNumberTextBox != null)
                    {
                        audioBeamSkeletonNumberTextBox.IsEnabled = false;
                    }

                    kinectSettings.audioTrackMode = KinectBase.AudioTrackingMode.Feedback;
                    if (kinectCore.kinect.AudioSource != null)
                    {
                        kinectCore.kinect.AudioSource.BeamAngleMode = Microsoft.Kinect.BeamAngleMode.Manual;
                    }
                }
                else if (audioBeamModeComboBox.SelectedIndex == 2) //Merged skeleton position mode
                {
                    if (audioBeamSkeletonNumberTextBox != null)
                    {
                        audioBeamSkeletonNumberTextBox.IsEnabled = true;
                    }

                    kinectSettings.audioTrackMode = KinectBase.AudioTrackingMode.MergedSkeletonX;
                    if (kinectCore.kinect.AudioSource != null)
                    {
                        kinectCore.kinect.AudioSource.BeamAngleMode = Microsoft.Kinect.BeamAngleMode.Manual;
                    }
                }
                else //Local skeleton position mode
                {
                    if (audioBeamSkeletonNumberTextBox != null)
                    {
                        audioBeamSkeletonNumberTextBox.IsEnabled = true;
                    }

                    kinectSettings.audioTrackMode = KinectBase.AudioTrackingMode.LocalSkeletonX;
                    if (kinectCore.kinect.AudioSource != null)
                    {
                        kinectCore.kinect.AudioSource.BeamAngleMode = Microsoft.Kinect.BeamAngleMode.Manual;
                    }
                }
            }
        }
        private void audioBeamSkeletonNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int tempSkel = 0;

            if (int.TryParse(audioBeamSkeletonNumberTextBox.Text, out tempSkel))
            {
                if (kinectSettings.audioTrackMode == KinectBase.AudioTrackingMode.LocalSkeletonX)
                {
                    if (tempSkel < 6)
                    {
                        kinectSettings.audioBeamTrackSkeletonNumber = tempSkel;
                    }
                }
                else if (kinectSettings.audioTrackMode == KinectBase.AudioTrackingMode.MergedSkeletonX)
                {
                    if (tempSkel < masterSettings.mergedSkeletonOptions.individualSkeletons.Count)
                    {
                        kinectSettings.audioBeamTrackSkeletonNumber = tempSkel;
                    }
                }
            }
        }
        #endregion

        #region Depth Option Methods
        private void depthResComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Microsoft.Kinect.DepthImageFormat newFormat = Microsoft.Kinect.DepthImageFormat.Undefined;

            switch (depthResComboBox.SelectedIndex)
            {
                case (0):
                    {
                        newFormat = Microsoft.Kinect.DepthImageFormat.Resolution640x480Fps30;
                        break;
                    }
                case (1):
                    {
                        newFormat = Microsoft.Kinect.DepthImageFormat.Resolution320x240Fps30;
                        break;
                    }
                case (2):
                    {
                        newFormat = Microsoft.Kinect.DepthImageFormat.Resolution80x60Fps30;
                        break;
                    }
                case (3):
                    {
                        //Note: This case should never be hit.  In order for skeleton tracking to work, we must have the depth image so turning it off is currently not available from the GUI
                        newFormat = Microsoft.Kinect.DepthImageFormat.Undefined;
                        break;
                    }
            }

            kinectSettings.depthImageMode = newFormat;

            kinectCore.ChangeDepthResolution(newFormat);
        }
        private void nearModeCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            //TODO: Reimplement near mode control somehow
            if ((bool)nearModeCheckBox.IsChecked)
            {
                try
                {
                    kinectCore.kinect.DepthStream.Range = Microsoft.Kinect.DepthRange.Near;
                    kinectSettings.isNearMode = true;
                }
                catch (InvalidOperationException)
                {
                    //Must be a XBox Kinect, so disable the near mode option and the force IR off mode
                    kinectCore.kinect.DepthStream.Range = Microsoft.Kinect.DepthRange.Default;
                    kinectSettings.isNearMode = false;
                    kinectCore.isXbox360Kinect = true;
                    DisableK4WOnlyOptions();
                }
            }
            else
            {
                kinectCore.kinect.DepthStream.Range = Microsoft.Kinect.DepthRange.Default;
                kinectSettings.isNearMode = false;
            }
        }
        private void irOnCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)irOnCheckBox.IsChecked)
            {
                kinectCore.kinect.ForceInfraredEmitterOff = false;
                kinectSettings.irON = true;
            }
            else if (!(bool)irOnCheckBox.IsChecked)
            {
                try
                {
                    kinectCore.kinect.ForceInfraredEmitterOff = true;
                    kinectSettings.irON = false;
                }
                catch (InvalidOperationException)
                {
                    //Must be a XBox Kinect, so disable near mode and IR off
                    kinectCore.kinect.ForceInfraredEmitterOff = false;
                    kinectSettings.irON = true;
                    kinectCore.isXbox360Kinect = true;
                    DisableK4WOnlyOptions();
                }
            }
        }
        #endregion

        #region Color Option Methods
        private void colorResComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Microsoft.Kinect.ColorImageFormat newFormat = Microsoft.Kinect.ColorImageFormat.Undefined;

            switch (colorResComboBox.SelectedIndex)
            {
                case (0):
                {
                    newFormat = Microsoft.Kinect.ColorImageFormat.RgbResolution640x480Fps30;
                    break;
                }
                case (1):
                {
                    newFormat = Microsoft.Kinect.ColorImageFormat.RgbResolution1280x960Fps12;
                    break;
                }
                case (2):
                {
                    newFormat = Microsoft.Kinect.ColorImageFormat.InfraredResolution640x480Fps30;
                    break;
                }
                case (3):
                {
                    newFormat = Microsoft.Kinect.ColorImageFormat.Undefined;
                    break;
                }
            }

            kinectSettings.colorImageMode = newFormat;
            kinectCore.ChangeColorResolution(newFormat);
        }
        private void advancedColorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AdvancedColorWindow colorWindow = new AdvancedColorWindow(kinectCore);
                colorWindow.ShowDialog();
            }
            catch
            {
                kinectCore.isXbox360Kinect = true;
                DisableK4WOnlyOptions();
            }
        }
        #endregion

        #region Kinect Position Option Methods
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
        private void yawPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(yawPosTextBox.Text, out temp))
            {
                kinectSettings.kinectYaw = temp;
            }
        }
        #endregion

        #region Skeleton Option Methods
        private void UseSkeletonCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            kinectSettings.mergeSkeletons = (bool)UseSkeletonCheckBox.IsChecked;
        }
        private void UseRawSkeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (kinectSettings.rawSkeletonSettings.individualSkeletons.Count != 6)
            {
                if (kinectSettings.rawSkeletonSettings.individualSkeletons.Count > 6)
                {
                    for (int i = kinectSettings.rawSkeletonSettings.individualSkeletons.Count - 1; i >= 6; i--)
                    {
                        kinectSettings.rawSkeletonSettings.individualSkeletons.RemoveAt(i);
                    }
                }
                else
                {
                    for (int i = kinectSettings.rawSkeletonSettings.individualSkeletons.Count; i < 6; i++)
                    {
                        KinectBase.PerSkeletonSettings tempSetting = new KinectBase.PerSkeletonSettings();
                        tempSetting.skeletonNumber = i;
                        tempSetting.useSkeleton = true;
                        tempSetting.serverName = "Kinect" + kinectID.ToString() + "Skel" + i.ToString();
                        tempSetting.renderColor = Colors.Transparent;
                        tempSetting.useRightHandGrip = true;
                        tempSetting.rightGripServerName = tempSetting.serverName;
                        tempSetting.rightGripButtonNumber = 0;
                        tempSetting.useLeftHandGrip = true;
                        tempSetting.leftGripServerName = tempSetting.serverName;
                        tempSetting.leftGripButtonNumber = 1;
                        kinectSettings.rawSkeletonSettings.individualSkeletons.Add(tempSetting);
                    }
                }
            }
            skeletonUserControl = new KinectV1SkeletonControl(this);
            XFormRawSkeletonCheckBox.IsEnabled = true;
            kinectSettings.sendRawSkeletons = (bool)UseRawSkeletonCheckBox.IsChecked;
        }
        private void UseRawSkeletonCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            skeletonUserControl = null;
            XFormRawSkeletonCheckBox.IsEnabled = false;
            kinectSettings.sendRawSkeletons = (bool)UseRawSkeletonCheckBox.IsChecked;
        }
        private void XFormRawSkeletonCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            kinectSettings.transformRawSkeletons = (bool)XFormRawSkeletonCheckBox.IsChecked;
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
        private void DisableK4WOnlyOptions()
        {
            //These controls only work with the offical Kinect for Windows Kinects, so we disable them if the system is using a XBox 360 Kinect
            nearModeCheckBox.IsEnabled = false;
            nearModeCheckBox.IsChecked = false;
            irOnCheckBox.IsChecked = true;
            irOnCheckBox.IsEnabled = false;
            advancedColorButton.IsEnabled = false;
        }
        #endregion


        //Update the options in the audio mode combobox
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //This was for updating the audio beam skeleton combo box items, but with the new design, we don't need it
            //TODO: Should we validate settings here?
        }
    }
}
