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
        internal int? KinectNumber;
        internal string ConnectionID = "";
        MainWindow parent = null;
        bool isVerbose = false;

        public KinectSettingsControl(int? kinectNumber, string connectionID, bool verboseOutput, MainWindow thisParent) //Parent is not optional since this GUI has to go somewhere
        {
            if (thisParent != null)
            {
                InitializeComponent();

                KinectNumber = kinectNumber;
                ConnectionID = connectionID;
                parent = thisParent;

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
            //TODO: Set the appropriate server setting here
            accelXChannelTextBox.IsEnabled = true;
            accelYChannelTextBox.IsEnabled = true;
            accelZChannelTextBox.IsEnabled = true;
            accelServerTextBox.IsEnabled = true;
        }
        private void SendAccelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //TODO: Set the appropriate server setting here
            accelXChannelTextBox.IsEnabled = false;
            accelYChannelTextBox.IsEnabled = false;
            accelZChannelTextBox.IsEnabled = false;
            accelServerTextBox.IsEnabled = false;
        }
        #endregion

        #region Audio Option Methods
        private void SendSoundAngleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //TODO: Set the appropriate server settings here
            audioServerTextBox.IsEnabled = true;
            audioChannelTextBox.IsEnabled = true;
        }
        private void SendSoundAngleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //TODO: Set the appropriate server settings here
            audioServerTextBox.IsEnabled = false;
            audioChannelTextBox.IsEnabled = false;
        }
        #endregion

        private void UseSkeletonCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            //TODO: Update the appropriate server variable here
        }
    }

    public class KinectSettingsControlComparer : IComparer<KinectSettingsControl>
    {
        //Sorts the KinectSettingsControl GUIs by Kinect number with null having the HIGHEST value
        //This is so the index in the array of pages will match the Kinect number, with unused ones at the end
        public int Compare(KinectSettingsControl x, KinectSettingsControl y)
        {
            if (x.KinectNumber == null)
            {
                if (y.KinectNumber == null)
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
                if (y.KinectNumber == null)
                {
                    return -1; //If y is null and x isn't, then y is greater
                }
                else
                {
                    int tempX = (int)x.KinectNumber;
                    int tempY = (int)y.KinectNumber;
                    return tempX.CompareTo(tempY);
                }
            }
        }
    }
}
