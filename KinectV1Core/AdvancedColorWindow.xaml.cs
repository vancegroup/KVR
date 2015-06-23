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
using System.Windows.Shapes;

namespace KinectV1Core
{
    /// <summary>
    /// Interaction logic for AdvancedColorWindow.xaml
    /// </summary>
    public partial class AdvancedColorWindow : Window
    {
        private KinectCoreV1 kinectCore;

        //TODO: Implement the functionality of the advanced color window
        public AdvancedColorWindow(KinectCoreV1 kinectV1Core)
        {
            if (kinectV1Core == null)
            {
                throw new ArgumentNullException("kinectV1Core");
            }

            InitializeComponent();

            kinectCore = kinectV1Core;

            ContrastSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Contrast;
            ContrastManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Contrast.ToString();
            SaturationSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Saturation;
            SaturationManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Saturation.ToString();
            GammaSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Gamma;
            GammaManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Gamma.ToString();
            SharpnessSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Sharpness;
            SharpnessManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Sharpness.ToString();
            WhiteBalSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance;
            WhiteBalManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance.ToString();
            BrightSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Brightness;
            BrightManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Brightness.ToString();
            WhiteBalAutoCheckBox.IsChecked = kinectCore.kinect.ColorStream.CameraSettings.AutoWhiteBalance;

            switch (kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode)
            {
                case Microsoft.Kinect.BacklightCompensationMode.AverageBrightness:
                {
                    
                    break;
                }
            }
        }
    }
}
