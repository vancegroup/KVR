using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Serialization;
using Microsoft.Kinect;

namespace KinectWithVRServer
{
    static class HelperMethods
    {
        internal static Key[] NumberKeys = {Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3, Key.NumPad4, Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,
                                    Key.D0, Key.D1, Key.D2, Key.D3, Key.D4, Key.D5, Key.D6, Key.D7, Key.D8, Key.D9, 
                                    Key.Return, Key.Enter, Key.Delete, Key.Back, Key.Left, Key.Right, Key.Tab, Key.OemMinus, Key.Subtract};

        internal static void WriteToLog(string text, MainWindow parent = null)
        {
            string stringTemp = "\r\n" + DateTime.Now.ToString() + ": " + text;

            if (parent != null) //GUI mode
            {
                if (parent.Dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    parent.LogTextBox.AppendText(stringTemp);

                    //Autoscroll mechanism
                    if (parent.LogScrollViewer.VerticalOffset >= (((TextBox)parent.LogScrollViewer.Content).ActualHeight - parent.LogScrollViewer.ActualHeight))
                    {
                        parent.LogScrollViewer.ScrollToEnd();
                    }
                }
                else
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        parent.LogTextBox.AppendText(stringTemp);

                        //Autoscroll mechanism
                        if (parent.LogScrollViewer.VerticalOffset >= (((TextBox)parent.LogScrollViewer.Content).ActualHeight - parent.LogScrollViewer.ActualHeight))
                        {
                            parent.LogScrollViewer.ScrollToEnd();
                        }
                    }), null
                    );
                }
            }
            else //Console mode
            {
                Console.Write(stringTemp);
            }
        }

        internal static void ShowErrorMessage(string title, string text, MainWindow parent = null)
        {
            if (parent != null)
            {
                if (parent.Dispatcher.Thread.ManagedThreadId == Thread.CurrentThread.ManagedThreadId)
                {
                    MessageBox.Show(parent, text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    parent.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        MessageBox.Show(parent, text, title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }), null
                    );
                }
            }
            else
            {
                WriteToLog(title + ": " + text);
            }
        }

        internal static MasterSettings LoadSettings(string fileName)
        {
            MasterSettings settings = null;
            XmlSerializer serializer = new XmlSerializer(typeof(MasterSettings));

            FileInfo info = new FileInfo(fileName);
            if (info.Exists)
            {
                using (FileStream file = new FileStream(fileName, FileMode.Open))
                {
                    settings = (MasterSettings)serializer.Deserialize(file);
                    file.Close();
                    file.Dispose();
                }
            }
            else
            {
                throw new Exception("File does not exist!");
            }

            return settings;
        }

        internal static void SaveSettings(string fileName, MasterSettings settings)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MasterSettings));

            using (FileStream file = new FileStream(fileName, FileMode.Create))
            {
                serializer.Serialize(file, settings);
                file.Close();
                file.Dispose();
            }
        }
    }

    public class AvailableKinectData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public string ConnectionID { get; set; }
        private int? kinectID;
        public int? KinectID
        {
            get { return kinectID; }
            set
            {
                kinectID = value;
                NotifyPropertyChanged("KinectID");
            }
        }
        private string serverStatus = "Stopped";
        public string ServerStatus
        {
            get { return serverStatus; }
            set
            {
                serverStatus = value;
                NotifyPropertyChanged("ServerStatus");
            }
        }
        public KinectStatus Status { get; set; }
        private bool useKinect;
        public bool UseKinect
        {
            get { return useKinect; }
            set
            {
                useKinect = value;
                NotifyPropertyChanged("UseKinect");
            }
        }
    }

    public class BoolToPressConverter : IValueConverter
    {
        public object Convert(object value, Type tagertType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? PressState.Pressed : PressState.Released;
        }

        public object ConvertBack(object value, Type tagetType, object parameter, CultureInfo culture)
        {
            return ((PressState)value == PressState.Pressed) ? true : false;
        }
    }

    public class ConnectionStateToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidCastException("The output of the converter must be a boolean");
            }

            if (((KinectStatus)value) == KinectStatus.Connected)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(KinectStatus))
            {
                throw new InvalidCastException("The output of the converter must be KinectStatus");
            }

            if ((bool)value)
            {
                return KinectStatus.Connected;
            }
            else
            {
                return KinectStatus.Undefined;
            }
        }
    }

    public class ConnectionStateToInverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(bool))
            {
                throw new InvalidCastException("The output of the converter must be a boolean");
            }

            if (((KinectStatus)value) == KinectStatus.Connected)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(KinectStatus))
            {
                throw new InvalidCastException("The output of the converter must be KinectStatus");
            }

            if ((bool)value)
            {
                return KinectStatus.Undefined;
            }
            else
            {
                return KinectStatus.Connected;
            }
        }
    }

    public class ConnectionStateToTextDecorationsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(TextDecorationCollection))
            {
                throw new InvalidCastException("The output of the converter must be TextDecorationCollection");
            }

            if (((KinectStatus)value) == KinectStatus.Connected)
            {
                return null;
            }
            else
            {
                return TextDecorations.Underline;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(KinectStatus))
            {
                throw new InvalidCastException("The output of the converter must be KinectStatus");
            }

            if (value == null)
            {
                return KinectStatus.Connected;
            }
            else
            {
                return KinectStatus.Undefined;
            }
        }
    }

    public class ConnectionStateToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(System.Windows.Media.Brush))
            {
                throw new InvalidCastException("The output of the converter must be TextDecorationCollection");
            }

            if (((KinectStatus)value) == KinectStatus.Connected)
            {
                return System.Windows.Media.Brushes.Black;
            }
            else
            {
                return new System.Windows.Media.SolidColorBrush(new System.Windows.Media.Color() { A = 0xFF, R = 0x00, G = 0x66, B = 0xCC });
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType != typeof(KinectStatus))
            {
                throw new InvalidCastException("The output of the converter must be KinectStatus");
            }

            if ((System.Windows.Media.Brush)value == System.Windows.Media.Brushes.Black)
            {
                return KinectStatus.Connected;
            }
            else
            {
                return KinectStatus.Undefined;
            }
        }
    }

    public class KinectSkeletonsData : INotifyPropertyChanged
    {
        public KinectSkeletonsData(string ConnectionID)
        {
            connectID = ConnectionID;
            actualSkeletons = new List<KinectSkeleton>(6);
            actualSkeletons.Add(new KinectSkeleton());
            actualSkeletons.Add(new KinectSkeleton());
            actualSkeletons.Add(new KinectSkeleton());
            actualSkeletons.Add(new KinectSkeleton());
            actualSkeletons.Add(new KinectSkeleton());
            actualSkeletons.Add(new KinectSkeleton());
            update = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private string connectID = "";
        private bool update = false;
        public int kinectID { get; set; }
        internal bool useSkeleton { get; set; }
        internal bool needsUpdate
        {
            get { return update; }
            set
            {
                update = value;
                if (value == true)
                {
                    NotifyPropertyChanged("needsUpdate");
                }
            }
        }
        internal string connectionID
        {
            get { return connectID; }
        }
        internal List<KinectSkeleton> actualSkeletons { get; set; }
    }

    public class KinectSkeletonsDataComparer : IComparer<KinectSkeletonsData>
    {
        //This just redirects the compare to a comparison on the KinectID property
        public int Compare(KinectSkeletonsData x, KinectSkeletonsData y)
        {
            return x.kinectID.CompareTo(y.kinectID);
        }
    }

    internal class KinectSkeleton
    {
        internal Skeleton skeleton { get; set; }
        internal volatile bool rightHandClosed;
        internal volatile bool leftHandClosed;
        internal volatile int masterSkeletonIndex = -1;
    }
}
