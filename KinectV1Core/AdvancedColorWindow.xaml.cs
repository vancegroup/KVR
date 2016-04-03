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
using Microsoft.Kinect;

namespace KinectV1Core
{
    /// <summary>
    /// Interaction logic for AdvancedColorWindow.xaml
    /// </summary>
    public partial class AdvancedColorWindow : Window
    {
        private KinectCoreV1 kinectCore;

        public AdvancedColorWindow(KinectCoreV1 kinectV1Core)
        {
            if (kinectV1Core == null)
            {
                throw new ArgumentNullException("kinectV1Core");
            }

            InitializeComponent();

            kinectCore = kinectV1Core;

            WhiteBalSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance;
            WhiteBalManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance.ToString();
            WhiteBalAutoCheckBox.IsChecked = kinectCore.kinect.ColorStream.CameraSettings.AutoWhiteBalance;
            SaturationSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Saturation;
            SaturationManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Saturation.ToString();
            GammaSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Gamma;
            GammaManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Gamma.ToString();
            HueSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Hue;
            HueManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Hue.ToString();
            SharpnessSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Sharpness;
            SharpnessManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Sharpness.ToString();
            ContrastSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Contrast;
            ContrastManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Contrast.ToString();
            ExposureSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.ExposureTime / 10.0;
            ExposureManSet.Text = (kinectCore.kinect.ColorStream.CameraSettings.ExposureTime / 10.0).ToString();
            ExposureAutoCheckBox.IsChecked = kinectCore.kinect.ColorStream.CameraSettings.AutoExposure;
            FrameIntervalSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.FrameInterval / 10.0;
            FrameIntervalManSet.Text = (kinectCore.kinect.ColorStream.CameraSettings.FrameInterval / 10.0).ToString();
            BrightSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Brightness;
            BrightManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Brightness.ToString();
            GainSlider.Value = kinectCore.kinect.ColorStream.CameraSettings.Gain;
            GainManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Gain.ToString();            
            
            switch (kinectCore.kinect.ColorStream.CameraSettings.PowerLineFrequency)
            {
                case PowerLineFrequency.SixtyHertz:
                {
                    powerLineComboBox.SelectedIndex = 0;
                    break;
                }
                case PowerLineFrequency.FiftyHertz:
                {
                    powerLineComboBox.SelectedIndex = 1;
                    break;
                }
                case PowerLineFrequency.Disabled:
                {
                    powerLineComboBox.SelectedIndex = 2;
                    break;
                }
            }
            switch (kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode)
            {
                case BacklightCompensationMode.AverageBrightness:
                {
                    BacklightCompComboBox.SelectedIndex = 0;
                    break;
                }
                case BacklightCompensationMode.CenterOnly:
                {
                    BacklightCompComboBox.SelectedIndex = 1;
                    break;
                }
                case BacklightCompensationMode.CenterPriority:
                {
                    BacklightCompComboBox.SelectedIndex = 2;
                    break;
                }
                case BacklightCompensationMode.LowlightsPriority:
                {
                    BacklightCompComboBox.SelectedIndex = 3;
                    break;
                }
            }
        }

        #region White Balance 
        private void WhiteBalManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized && WhiteBalManSet.IsEnabled)
            {
                int balanceTemp = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance;

                if (int.TryParse(WhiteBalManSet.Text, out balanceTemp))
                {
                    if (balanceTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxWhiteBalance)
                    {
                        balanceTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxWhiteBalance;
                    }
                    else if (balanceTemp < kinectCore.kinect.ColorStream.CameraSettings.MinWhiteBalance)
                    {
                        balanceTemp = kinectCore.kinect.ColorStream.CameraSettings.MinWhiteBalance;
                    }
                }

                WhiteBalManSet.Text = balanceTemp.ToString();
                WhiteBalSlider.Value = balanceTemp;
            }
        }
        private void WhiteBalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null && WhiteBalSlider.IsEnabled)
            {
                kinectCore.masterKinectSettings.WhiteBalance = (int)WhiteBalSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance = (int)WhiteBalSlider.Value;
                WhiteBalManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.WhiteBalance.ToString();
            }
        }
        private void WhiteBalAutoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isAuto = (bool)WhiteBalAutoCheckBox.IsChecked;

            WhiteBalManSet.IsEnabled = !isAuto;
            WhiteBalSlider.IsEnabled = !isAuto;
            kinectCore.masterKinectSettings.autoWhiteBalance = isAuto;
            kinectCore.kinect.ColorStream.CameraSettings.AutoWhiteBalance = isAuto;
        }
        #endregion

        #region Saturation
        private void SaturationManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                double satTemp = kinectCore.kinect.ColorStream.CameraSettings.Saturation;

                if (double.TryParse(SaturationManSet.Text, out satTemp))
                {
                    if (satTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxSaturation)
                    {
                        satTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxSaturation;
                    }
                    else if (satTemp < kinectCore.kinect.ColorStream.CameraSettings.MinSaturation)
                    {
                        satTemp = kinectCore.kinect.ColorStream.CameraSettings.MinSaturation;
                    }
                }

                SaturationManSet.Text = satTemp.ToString();
                SaturationSlider.Value = satTemp;
            }
        }
        private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null)
            {
                kinectCore.masterKinectSettings.Saturation = SaturationSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Saturation = SaturationSlider.Value;
                SaturationManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Saturation.ToString();
            }
        }
        #endregion

        #region Gamma
        private void GammaManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                double gammaTemp = kinectCore.kinect.ColorStream.CameraSettings.Gamma;

                if (double.TryParse(GammaManSet.Text, out gammaTemp))
                {
                    if (gammaTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxGamma)
                    {
                        gammaTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxGamma;
                    }
                    else if (gammaTemp < kinectCore.kinect.ColorStream.CameraSettings.MinGamma)
                    {
                        gammaTemp = kinectCore.kinect.ColorStream.CameraSettings.MinGamma;
                    }
                }

                GammaManSet.Text = gammaTemp.ToString();
                GammaSlider.Value = gammaTemp;
            }
        }
        private void GammaSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null)
            {
                kinectCore.masterKinectSettings.Gamma = GammaSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Gamma = GammaSlider.Value;
                GammaManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Gamma.ToString();
            }
        }
        #endregion

        #region Hue
        private void HueManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                double hueTemp = kinectCore.kinect.ColorStream.CameraSettings.Hue;

                if (double.TryParse(HueManSet.Text, out hueTemp))
                {
                    if (hueTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxHue)
                    {
                        hueTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxHue;
                    }
                    else if (hueTemp < kinectCore.kinect.ColorStream.CameraSettings.MinHue)
                    {
                        hueTemp = kinectCore.kinect.ColorStream.CameraSettings.MinHue;
                    }
                }

                HueManSet.Text = hueTemp.ToString();
                HueSlider.Value = hueTemp;
            }
        }
        private void HueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null)
            {
                kinectCore.masterKinectSettings.Hue = HueSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Hue = HueSlider.Value;
                HueManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Hue.ToString();
            }
        }
        #endregion

        #region Sharpness
        private void SharpnessManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                double sharpTemp = kinectCore.kinect.ColorStream.CameraSettings.Sharpness;

                if (double.TryParse(SharpnessManSet.Text, out sharpTemp))
                {
                    if (sharpTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxSharpness)
                    {
                        sharpTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxSharpness;
                    }
                    else if (sharpTemp < kinectCore.kinect.ColorStream.CameraSettings.MinSharpness)
                    {
                        sharpTemp = kinectCore.kinect.ColorStream.CameraSettings.MinSharpness;
                    }
                }

                SharpnessManSet.Text = sharpTemp.ToString();
                SharpnessSlider.Value = sharpTemp;
            }
        }
        private void SharpnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null)
            {
                kinectCore.masterKinectSettings.Sharpness = SharpnessSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Sharpness = SharpnessSlider.Value;
                SharpnessManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Sharpness.ToString();
            }
        }
        #endregion

        #region Contrast
        private void ContrastManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                double contrastTemp = kinectCore.kinect.ColorStream.CameraSettings.Contrast;

                if (double.TryParse(ContrastManSet.Text, out contrastTemp))
                {
                    if (contrastTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxContrast)
                    {
                        contrastTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxContrast;
                    }
                    else if (contrastTemp < kinectCore.kinect.ColorStream.CameraSettings.MinContrast)
                    {
                        contrastTemp = kinectCore.kinect.ColorStream.CameraSettings.MinContrast;
                    }
                }

                ContrastManSet.Text = contrastTemp.ToString();
                ContrastSlider.Value = contrastTemp;
            }
        }
        private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null)
            {
                kinectCore.masterKinectSettings.Contrast = ContrastSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Contrast = ContrastSlider.Value;
                ContrastManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Contrast.ToString();
            }
        }
        #endregion

        #region Powerline Frequency
        private void powerLineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized && kinectCore != null)
            {
                if (powerLineComboBox.SelectedIndex == 0)
                {
                    kinectCore.kinect.ColorStream.CameraSettings.PowerLineFrequency = PowerLineFrequency.SixtyHertz;
                    kinectCore.masterKinectSettings.lineFrequency = KinectBase.PowerLineFrequency.SixtyHertz;
                }
                else if (powerLineComboBox.SelectedIndex == 1)
                {
                    kinectCore.kinect.ColorStream.CameraSettings.PowerLineFrequency = PowerLineFrequency.FiftyHertz;
                    kinectCore.masterKinectSettings.lineFrequency = KinectBase.PowerLineFrequency.FiftyHertz;
                }
                else
                {
                    kinectCore.kinect.ColorStream.CameraSettings.PowerLineFrequency = PowerLineFrequency.Disabled;
                    kinectCore.masterKinectSettings.lineFrequency = KinectBase.PowerLineFrequency.Disabled;
                }
            }
        }
        #endregion

        #region Backlight Compensation
        private void BacklightCompComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized && kinectCore != null)
            {
                if (BacklightCompComboBox.SelectedIndex == 1)
                {
                    kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode = BacklightCompensationMode.CenterOnly;
                    kinectCore.masterKinectSettings.backlightMode = KinectBase.BacklightCompensationMode.CenterOnly;
                }
                else if (BacklightCompComboBox.SelectedIndex == 2)
                {
                    kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode = BacklightCompensationMode.CenterPriority;
                    kinectCore.masterKinectSettings.backlightMode = KinectBase.BacklightCompensationMode.CenterPriority;
                }
                else if (BacklightCompComboBox.SelectedIndex == 3)
                {
                    kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode = BacklightCompensationMode.LowlightsPriority;
                    kinectCore.masterKinectSettings.backlightMode = KinectBase.BacklightCompensationMode.LowlightsPriority;
                }
                else
                {
                    kinectCore.kinect.ColorStream.CameraSettings.BacklightCompensationMode = BacklightCompensationMode.AverageBrightness;
                    kinectCore.masterKinectSettings.backlightMode = KinectBase.BacklightCompensationMode.AverageBrightness;
                }
            }
        }
        #endregion

        #region Exposure
        private void ExposureManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized && ExposureManSet.IsEnabled)
            {
                double exposureTemp = kinectCore.kinect.ColorStream.CameraSettings.ExposureTime;

                if (double.TryParse(ExposureManSet.Text, out exposureTemp))
                {
                    exposureTemp *= 10; //Multiply by 10 to get it in units of 1/10,000 of a second instead of ms

                    if (exposureTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxExposureTime)
                    {
                        exposureTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxExposureTime;
                    }
                    else if (exposureTemp < kinectCore.kinect.ColorStream.CameraSettings.MinExposureTime)
                    {
                        exposureTemp = kinectCore.kinect.ColorStream.CameraSettings.MinExposureTime;
                    }
                }

                double msExposure = exposureTemp / 10.0;
                ExposureManSet.Text = msExposure.ToString();
                ExposureSlider.Value = msExposure;
            }
        }
        private void ExposureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null && ExposureSlider.IsEnabled)
            {
                kinectCore.masterKinectSettings.ExposureTime = ExposureSlider.Value * 10;
                kinectCore.kinect.ColorStream.CameraSettings.ExposureTime = ExposureSlider.Value * 10;
                double msExposure = kinectCore.kinect.ColorStream.CameraSettings.ExposureTime / 10.0;
                ExposureManSet.Text = msExposure.ToString();
            }
        }
        private void ExposureAutoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            bool isAuto = (bool)ExposureAutoCheckBox.IsChecked;

            ExposureManSet.IsEnabled = !isAuto;
            ExposureSlider.IsEnabled = !isAuto;
            GainManSet.IsEnabled = !isAuto;
            GainSlider.IsEnabled = !isAuto;
            FrameIntervalManSet.IsEnabled = !isAuto;
            FrameIntervalSlider.IsEnabled = !isAuto;
            BrightManSet.IsEnabled = isAuto;
            BrightSlider.IsEnabled = isAuto;
            kinectCore.masterKinectSettings.autoExposure = isAuto;
            kinectCore.kinect.ColorStream.CameraSettings.AutoExposure = isAuto;
        }
        #endregion

        #region Interframe Delay
        private void FrameIntervalManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized && FrameIntervalManSet.IsEnabled)
            {
                double intervalTemp = kinectCore.kinect.ColorStream.CameraSettings.FrameInterval;

                if (double.TryParse(FrameIntervalManSet.Text, out intervalTemp))
                {
                    intervalTemp *= 10; //Multiply by 10 to get it in units of 1/10,000 of a second instead of ms

                    if (intervalTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxFrameInterval)
                    {
                        intervalTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxFrameInterval;
                    }
                    else if (intervalTemp < kinectCore.kinect.ColorStream.CameraSettings.MinFrameInterval)
                    {
                        intervalTemp = kinectCore.kinect.ColorStream.CameraSettings.MinFrameInterval;
                    }
                }

                double msInterval = intervalTemp / 10.0;
                FrameIntervalManSet.Text = msInterval.ToString();
                FrameIntervalSlider.Value = msInterval;
            }
        }
        private void FrameIntervalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null && FrameIntervalSlider.IsEnabled)
            {
                kinectCore.masterKinectSettings.FrameInterval = FrameIntervalSlider.Value * 10;
                kinectCore.kinect.ColorStream.CameraSettings.FrameInterval = FrameIntervalSlider.Value * 10;
                double msInterval = kinectCore.kinect.ColorStream.CameraSettings.FrameInterval / 10.0;
                FrameIntervalManSet.Text = msInterval.ToString();
            }
        }
        #endregion

        #region Brightness
        private void BrightManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized && BrightManSet.IsEnabled)
            {
                double brightnessTemp = kinectCore.kinect.ColorStream.CameraSettings.Brightness;

                if (double.TryParse(BrightManSet.Text, out brightnessTemp))
                {
                    if (brightnessTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxBrightness)
                    {
                        brightnessTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxBrightness;
                    }
                    else if (brightnessTemp < kinectCore.kinect.ColorStream.CameraSettings.MinBrightness)
                    {
                        brightnessTemp = kinectCore.kinect.ColorStream.CameraSettings.MinBrightness;
                    }
                }

                BrightManSet.Text = brightnessTemp.ToString();
                BrightSlider.Value = brightnessTemp;
            }
        }
        private void BrightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null && BrightSlider.IsEnabled)
            {
                kinectCore.masterKinectSettings.Brightness = BrightSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Brightness = BrightSlider.Value;
                BrightManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Brightness.ToString();
            }
        }
        #endregion

        #region Gain
        private void GainManSet_LostFocus(object sender, RoutedEventArgs e)
        {
            if (IsInitialized && GainManSet.IsEnabled)
            {
                double gainTemp = kinectCore.kinect.ColorStream.CameraSettings.Gain;

                if (double.TryParse(GainManSet.Text, out gainTemp))
                {
                    if (gainTemp > kinectCore.kinect.ColorStream.CameraSettings.MaxGain)
                    {
                        gainTemp = kinectCore.kinect.ColorStream.CameraSettings.MaxGain;
                    }
                    else if (gainTemp < kinectCore.kinect.ColorStream.CameraSettings.MinGain)
                    {
                        gainTemp = kinectCore.kinect.ColorStream.CameraSettings.MinGain;
                    }
                }

                GainManSet.Text = gainTemp.ToString();
                GainSlider.Value = gainTemp;
            }
        }
        private void GainSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized && kinectCore != null && GainSlider.IsEnabled)
            {
                kinectCore.masterKinectSettings.Gain = GainSlider.Value;
                kinectCore.kinect.ColorStream.CameraSettings.Gain = GainSlider.Value;
                GainManSet.Text = kinectCore.kinect.ColorStream.CameraSettings.Gain.ToString();
            }
        }
        #endregion

        #region Constrain number inputs to text boxes
        //Rejects any points that are not numbers or control characters or a period
        private void floatNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key) && e.Key != Key.OemPeriod && e.Key != Key.Decimal)
            {
                e.Handled = true;
            }

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
        //Rejects any points that are not numbers or control charactes
        private void intNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key))
            {
                e.Handled = true;
            }

            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                ((TextBox)sender).MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }
        #endregion
    }
}
