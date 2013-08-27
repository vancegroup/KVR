using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml.Serialization;
using Microsoft.Kinect;

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
}
