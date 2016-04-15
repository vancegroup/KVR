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
            if (kinectID.HasValue)
            {
                masterSettings = newSettings;
                dynamic tempSettings = newSettings.kinectOptionsList[kinectID.Value];
                kinectSettings = (KinectV2Settings)tempSettings;

                //Update the color options
                useColorRadioButton.IsChecked = kinectSettings.useColorPreview;
                useIRRadionButton.IsChecked = kinectSettings.useIRPreview;

                //Update the depth options
                ScaleDepthCheckBox.IsChecked = kinectSettings.scaleDepthToReliableRange;
                ColorizeDepthCheckBox.IsChecked = kinectSettings.colorizeDepth;

                //Update skeleton options
                UseSkeletonCheckBox.IsChecked = kinectSettings.mergeSkeletons;
                UseRawSkeletonCheckBox.IsChecked = kinectSettings.sendRawSkeletons;
                XFormRawSkeletonCheckBox.IsChecked = kinectSettings.transformRawSkeletons;

                //Update audio settings
                SendSoundAngleCheckBox.IsChecked = kinectSettings.sendAudioAngle;
                audioServerTextBox.Text = kinectSettings.audioAngleServerName;
                audioChannelTextBox.Text = kinectSettings.audioAngleChannel.ToString();
                audioBeamSkeletonNumberTextBox.Text = kinectSettings.audioBeamTrackSkeletonNumber.ToString();
                switch (kinectSettings.audioTrackMode)
                {
                    //Note: the enabled/disabled controls don't need to be set manually here because the selected index changed event will still be hit
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

                //Update the position options
                xPosTextBox.Text = kinectSettings.kinectPosition.X.ToString();
                yPosTextBox.Text = kinectSettings.kinectPosition.Y.ToString();
                zPosTextBox.Text = kinectSettings.kinectPosition.Z.ToString();
                yawPosTextBox.Text = kinectSettings.kinectYaw.ToString();
                pitchPosTextBox.Text = kinectSettings.kinectPitch.ToString();
                rollPosTextBox.Text = kinectSettings.kinectRoll.ToString();
            }
        }


        #region Color Preview Option Methods
        private void colorPreviewRadioButton_CheckChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)useColorRadioButton.IsChecked)
            {
                //Note: only useColorPreview OR useIRPreview needs to be set, they are linked to the same field so they are mutually exclusive options
                kinectSettings.useColorPreview = true;
            }
            else
            {
                kinectSettings.useIRPreview = true;
            }
        }
        #endregion

        #region Depth Preview Option Methods
        private void ScaleDepthCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            kinectSettings.scaleDepthToReliableRange = (bool)ScaleDepthCheckBox.IsChecked;
        }
        private void ColorizeDepthCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            kinectSettings.colorizeDepth = (bool)ColorizeDepthCheckBox.IsChecked;
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
            skeletonUserControl = new KinectV2SkeletonControl(this);
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
                        for (int i = 0; i < kinectCore.kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinectCore.kinect.AudioSource.AudioBeams[i].AudioBeamMode = Microsoft.Kinect.AudioBeamMode.Automatic;
                        }
                    }
                    EnableSourceAngleSending();
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
                        for (int i = 0; i < kinectCore.kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinectCore.kinect.AudioSource.AudioBeams[i].AudioBeamMode = Microsoft.Kinect.AudioBeamMode.Manual;
                        }
                    }
                    DisableSourceAngleSending();
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
                        for (int i = 0; i < kinectCore.kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinectCore.kinect.AudioSource.AudioBeams[i].AudioBeamMode = Microsoft.Kinect.AudioBeamMode.Manual;
                        }
                    }
                    DisableSourceAngleSending();
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
                        for (int i = 0; i < kinectCore.kinect.AudioSource.AudioBeams.Count; i++)
                        {
                            kinectCore.kinect.AudioSource.AudioBeams[i].AudioBeamMode = Microsoft.Kinect.AudioBeamMode.Manual;
                        }
                    }
                    DisableSourceAngleSending();
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
        private void EnableSourceAngleSending()
        {
            if (SendSoundAngleCheckBox != null)
            {
                SendSoundAngleCheckBox.IsEnabled = true;

                if ((bool)SendSoundAngleCheckBox.IsChecked)
                {
                    if (audioServerTextBox != null)
                    {
                        audioServerTextBox.IsEnabled = true;
                    }
                    if (audioChannelTextBox != null)
                    {
                        audioChannelTextBox.IsEnabled = true;
                    }
                }
            }
        }
        private void DisableSourceAngleSending()
        {
            if (SendSoundAngleCheckBox != null)
            {
                SendSoundAngleCheckBox.IsEnabled = false;
            }
            if (audioServerTextBox != null)
            {
                audioServerTextBox.IsEnabled = false;
            }
            if (audioChannelTextBox != null)
            {
                audioChannelTextBox.IsEnabled = false;
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
        private void pitchPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(pitchPosTextBox.Text, out temp))
            {
                kinectSettings.kinectPitch = temp;
            }
        }
        private void rollPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(rollPosTextBox.Text, out temp))
            {
                kinectSettings.kinectRoll = temp;
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
    }
}
