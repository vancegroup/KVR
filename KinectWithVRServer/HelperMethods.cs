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
using KinectBase;
using System.Windows.Media.Media3D;
//using Microsoft.Kinect;

namespace KinectWithVRServer
{
    static class HelperMethods
    {
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

        internal static KinectBase.MasterSettings LoadSettings(string fileName)
        {
            KinectBase.MasterSettings settings = null;
            XmlSerializer serializer = new XmlSerializer(typeof(SerializableSettings));
            SerializableSettings tempSettings = null;

            FileInfo info = new FileInfo(fileName);
            if (info.Exists)
            {
                using (FileStream file = new FileStream(fileName, FileMode.Open))
                {
                    tempSettings = (SerializableSettings)serializer.Deserialize(file);
                    file.Close();
                    file.Dispose();
                }
            }
            else
            {
                throw new Exception("File does not exist!");
            }

            //Copy the settings from the serializable settings object to the real settings object
            settings = tempSettings.masterSettings;
            settings.kinectOptionsList = new List<IKinectSettings>();
            for (int i = 0; i < tempSettings.kinectV1Settings.Length; i++)
            {
                settings.kinectOptionsList.Add(tempSettings.kinectV1Settings[i]);
            }
            for (int i = 0; i < tempSettings.kinectV2Settings.Length; i++)
            {
                settings.kinectOptionsList.Add(tempSettings.kinectV2Settings[i]);
            }
            for (int i = 0; i < tempSettings.networkKinectSettings.Length; i++)
            {
                settings.kinectOptionsList.Add(tempSettings.networkKinectSettings[i]);
            }

            settings.kinectOptionsList.Sort(new KinectBase.KinectSettingsComparer());

            return settings;
        }

        internal static void SaveSettings(string fileName, KinectBase.MasterSettings settings)
        {
            //Create a serializable version of the settings (basically, move the Kinect options from the Master settings to a type specific array)
            List<KinectV1Wrapper.Settings> kinect1Settings = new List<KinectV1Wrapper.Settings>();
            List<KinectV2Wrapper.Settings> kinect2Settings = new List<KinectV2Wrapper.Settings>();
            List<NetworkKinectWrapper.Settings> networkSettings = new List<NetworkKinectWrapper.Settings>();
            for (int i = 0; i < settings.kinectOptionsList.Count; i++)
            {
                if (settings.kinectOptionsList[i].version == KinectVersion.KinectV1)
                {
                    kinect1Settings.Add((KinectV1Wrapper.Settings)settings.kinectOptionsList[i]);
                }
                else if (settings.kinectOptionsList[i].version == KinectVersion.KinectV2)
                {
                    kinect2Settings.Add((KinectV2Wrapper.Settings)settings.kinectOptionsList[i]);
                }
                else if (settings.kinectOptionsList[i].version == KinectVersion.NetworkKinect)
                {
                    networkSettings.Add((NetworkKinectWrapper.Settings)settings.kinectOptionsList[i]);
                }
            }
            SerializableSettings serialSettings = new SerializableSettings();
            serialSettings.masterSettings = settings;
            serialSettings.kinectV1Settings = kinect1Settings.ToArray();
            serialSettings.kinectV2Settings = kinect2Settings.ToArray();
            serialSettings.networkKinectSettings = networkSettings.ToArray();

            //Do the actual serialization
            XmlSerializer serializer = new XmlSerializer(typeof(SerializableSettings));
            using (FileStream file = new FileStream(fileName, FileMode.Create))
            {
                serializer.Serialize(file, serialSettings);
                file.Close();
                file.Dispose();
            }
        }

        internal static Point3D IncAverage(Point3D x, Point3D y, int n)
        {
            Point3D avePoint = new Point3D();
            avePoint.X = (float)((double)x.X + ((double)y.X - (double)x.X) / (double)(n + 1));
            avePoint.Y = (float)((double)x.Y + ((double)y.Y - (double)x.Y) / (double)(n + 1));
            avePoint.Z = (float)((double)x.Z + ((double)y.Z - (double)x.Z) / (double)(n + 1));
            return avePoint;
        }

        internal static KinectSkeleton DeepCopySkeleton(KinectSkeleton inSkel)
        {
            KinectSkeleton outSkel = new KinectSkeleton();

            outSkel.leftHandClosed = inSkel.leftHandClosed;
            outSkel.Position = inSkel.Position;
            outSkel.rightHandClosed = inSkel.rightHandClosed;
            outSkel.SkeletonTrackingState = inSkel.SkeletonTrackingState;
            outSkel.sourceKinectID = inSkel.sourceKinectID;
            outSkel.TrackingId = inSkel.TrackingId;

            for (int i = 0; i < inSkel.skeleton.Count; i++)
            {
                outSkel.skeleton[i] = DeepCopyJoint(inSkel.skeleton[i]);
            }

            return outSkel;
        }

        internal static Joint DeepCopyJoint(Joint inJoint)
        {
            Joint outJoint = new Joint();
            outJoint.Confidence = inJoint.Confidence;
            outJoint.JointType = inJoint.JointType;
            outJoint.Orientation = inJoint.Orientation;  //Orientation is a struct, so it will set by value
            outJoint.Position = inJoint.Position;  //Point3D is a struct, so it will set by value
            outJoint.spatialErrorStdDev = inJoint.spatialErrorStdDev;
            outJoint.TrackingState = inJoint.TrackingState;
            outJoint.utcTime = inJoint.utcTime;  //DateTime is a struct, so it will set by value

            return outJoint;
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

        public string UniqueID { get; set; }
        //public string ConnectionID { get; set; }
        public string KinectTypeString { get; set; }
        public KinectVersion kinectType { get; set; }
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

    public class ConfiguredServerData : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private string serverName = "";
        public string ServerName
        {
            get { return serverName; }
            set
            {
                serverName = value;
                NotifyPropertyChanged("ServerName");
            }
        }

        private bool analogServer = false;
        public bool AnalogServer
        {
            get { return analogServer; }
            set
            {
                analogServer = value;
                NotifyPropertyChanged("AnalogServer");
            }
        }
        private int analogChannels = 0;
        public int AnalogChannels
        {
            get { return analogChannels; }
            set
            {
                analogChannels = value;
                NotifyPropertyChanged("AnalogChannels");
            }
        }
        public string AnalogChannelsString
        {
            get
            {
                if (analogServer)
                {
                    return analogChannels.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        private bool buttonServer = false;
        public bool ButtonServer
        {
            get { return buttonServer; }
            set
            {
                buttonServer = value;
                NotifyPropertyChanged("ButtonServer");
            }
        }
        private int buttonChannels = 0;
        public int ButtonChannels
        {
            get { return buttonChannels; }
            set
            {
                buttonChannels = value;
                NotifyPropertyChanged("ButtonChannels");
            }
        }
        public string ButtonChannelsString
        {
            get
            {
                if (buttonServer)
                {
                    return buttonChannels.ToString();
                }
                else
                {
                    return "";
                }
            }
        }

        private bool imageServer = false;
        public bool ImageServer
        {
            get { return imageServer; }
            set
            {
                imageServer = value;
                NotifyPropertyChanged("ImageServer");
            }
        }

        private bool textServer = false;
        public bool TextServer
        {
            get { return textServer; }
            set
            {
                textServer = value;
                NotifyPropertyChanged("TextServer");
            }
        }

        private bool trackerServer = false;
        public bool TrackerServer
        {
            get { return trackerServer; }
            set
            {
                trackerServer = value;
                NotifyPropertyChanged("TrackerServer");
            }
        }
        private int trackerChannels = 0;
        public int TrackerChannels
        {
            get { return trackerChannels; }
            set
            {
                trackerChannels = value;
                NotifyPropertyChanged("TrackerChannels");
            }
        }
        public string TrackerChannelsString
        {
            get
            {
                if (trackerServer)
                {
                    return trackerChannels.ToString();
                }
                else
                {
                    return "";
                }
            }
        }
    }

    public class BoolToPressConverter : IValueConverter
    {
        public object Convert(object value, Type tagertType, object parameter, CultureInfo culture)
        {
            return ((bool)value == true) ? KinectBase.PressState.Pressed : KinectBase.PressState.Released;
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

    public class SerializableSettings
    {
        public KinectBase.MasterSettings masterSettings { get; set; }
        public KinectV1Wrapper.Settings[] kinectV1Settings { get; set; }
        public KinectV2Wrapper.Settings[] kinectV2Settings { get; set; }
        public NetworkKinectWrapper.Settings[] networkKinectSettings { get; set; }
    }
}
