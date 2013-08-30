using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KinectWithVRServer
{
    /// <summary>
    /// Interaction logic for KinectSettingsControl.xaml
    /// </summary>
    public partial class KinectSettingsControl : UserControl
    {
        internal int? KinectID;
        internal string ConnectionID = "";
        MainWindow parent = null;
        bool isVerbose = false;  //TODO: This variable may not be needed...
        private Key[] numberKeys = {Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
                                    Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, 
                                    Key.Return, Key.Enter, Key.Delete, Key.Back, Key.Left, Key.Right, Key.Tab};

        public KinectSettingsControl(int? kinectNumber, string connectionID, bool verboseOutput, MainWindow thisParent) //Parent is not optional since this GUI has to go somewhere
        {
            if (thisParent != null)
            {
                ConnectionID = connectionID;
                parent = thisParent;
                KinectID = kinectNumber;
                isVerbose = verboseOutput;

                InitializeComponent();

                this.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                this.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                Grid.SetColumn(this, 2);
                this.Visibility = System.Windows.Visibility.Collapsed;
                parent.KinectTabMasterGrid.Children.Add(this);
            }
            else
            {
                throw new NotSupportedException("Method arguements are invalid!");
            }
        }

        #region Acceleration Option Methods
        private void SendAccelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].sendAcceleration = true;

            //Enable the server boxes
            accelXChannelTextBox.IsEnabled = true;
            accelYChannelTextBox.IsEnabled = true;
            accelZChannelTextBox.IsEnabled = true;
            accelServerTextBox.IsEnabled = true;
        }
        private void SendAccelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].sendAcceleration = false;

            //Disable the server boxes
            accelXChannelTextBox.IsEnabled = false;
            accelYChannelTextBox.IsEnabled = false;
            accelZChannelTextBox.IsEnabled = false;
            accelServerTextBox.IsEnabled = false;
        }
        private void accelServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].accelerationServerName = accelServerTextBox.Text;
        }
        private void accelXChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelXChannelTextBox.Text, out temp))
            {
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].accelXChannel = temp;
            }
        }
        private void accelYChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelYChannelTextBox.Text, out temp))
            {
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].accelYChannel = temp;
            }
        }
        private void accelZChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(accelZChannelTextBox.Text, out temp))
            {
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].accelZChannel = temp;
            }
        }
        #endregion

        #region Audio Option Methods
        private void SendSoundAngleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].sendAudioAngle = true;

            //Enable the server boxes
            audioServerTextBox.IsEnabled = true;
            audioChannelTextBox.IsEnabled = true;
        }
        private void SendSoundAngleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].sendAudioAngle = false;

            //Disable the server boxes
            audioServerTextBox.IsEnabled = false;
            audioChannelTextBox.IsEnabled = false;
        }
        private void audioServerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].audioAngleServerName = audioServerTextBox.Text;
        }
        private void audioChannelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = 0;
            if (int.TryParse(audioChannelTextBox.Text, out temp))
            {
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].audioAngleChannel = temp;
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

            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].depthImageMode = newFormat;
            parent.server.kinects[(int)KinectID].ChangeDepthResolution(newFormat);
        }
        private void nearModeCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].isNearMode = (bool)nearModeCheckBox.IsChecked;
            if ((bool)nearModeCheckBox.IsChecked)
            {
                parent.server.kinects[(int)KinectID].kinect.DepthStream.Range = Microsoft.Kinect.DepthRange.Near;
            }
            else
            {
                parent.server.kinects[(int)KinectID].kinect.DepthStream.Range = Microsoft.Kinect.DepthRange.Default;
            }
        }
        private void irOnCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].irON = (bool)irOnCheckBox.IsChecked;
            parent.server.kinects[(int)KinectID].kinect.ForceInfraredEmitterOff = !(bool)irOnCheckBox.IsChecked;
        }
        private void UseSkeletonCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].trackSkeletons = (bool)UseSkeletonCheckBox.IsChecked;
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

            parent.server.serverMasterOptions.kinectOptions[(int)KinectID].colorImageMode = newFormat;
            parent.server.kinects[(int)KinectID].ChangeColorResolution(newFormat);
        }
        #endregion

        #region Kinect Position Option Methods
        private void xPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(xPosTextBox.Text, out temp))
            {
                if (parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition == null)
                {
                    parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition;
                tempPoint.X = temp;
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = tempPoint;
            }
        }
        private void yPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(yPosTextBox.Text, out temp))
            {
                if (parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition == null)
                {
                    parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition;
                tempPoint.Y = temp;
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = tempPoint;
            }
        }
        private void zPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(zPosTextBox.Text, out temp))
            {
                if (parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition == null)
                {
                    parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = new System.Windows.Media.Media3D.Point3D(0, 0, 0);
                }
                System.Windows.Media.Media3D.Point3D tempPoint = parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition;
                tempPoint.Z = temp;
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectPosition = tempPoint;
            }
        }
        private void yawPosTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            double temp = 0.0;
            if (double.TryParse(yawPosTextBox.Text, out temp))
            {
                parent.server.serverMasterOptions.kinectOptions[(int)KinectID].kinectYaw = temp;
            }
        }
        #endregion

        #region Misc. Methods
        //Rejects any points that are not numbers or control characters or a period
        private void floatNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!numberKeys.Contains(e.Key) && e.Key != Key.OemPeriod)
            {
                e.Handled = true;
            }
        }
        //Rejects any points that are not numbers or control charactes
        private void intNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!numberKeys.Contains(e.Key))
            {
                e.Handled = true;
            }
        }
        #endregion
    }

    public class KinectSettingsControlComparer : IComparer<KinectSettingsControl>
    {
        //Sorts the KinectSettingsControl GUIs by Kinect number with null having the HIGHEST value
        //This is so the index in the array of pages will match the Kinect number, with unused ones at the end
        public int Compare(KinectSettingsControl x, KinectSettingsControl y)
        {
            if (x.KinectID == null)
            {
                if (y.KinectID == null)
                {
                    return 0; //If both are the null, they are equal
                }
                else
                {
                    return 1; //If x is null and y isn't, x is greater than y
                }
            }
            else
            {
                if (y.KinectID == null)
                {
                    return -1; //If y is null and x isn't, then y is greater
                }
                else
                {
                    int tempX = (int)x.KinectID;
                    int tempY = (int)y.KinectID;
                    return tempX.CompareTo(tempY);
                }
            }
        }
    }
}