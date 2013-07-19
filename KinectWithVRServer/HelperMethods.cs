using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Controls;

namespace KinectWithVRServer
{
    static class HelperMethods
    {
        internal static void WriteToLog(string text, MainWindow parent = null)
        {
            string stringTemp = "\r\n" + DateTime.Now.ToString() + ": " + text;

            if (parent != null) //GUI mode
            {
                parent.LogTextBox.AppendText(stringTemp);

                //Autoscroll mechanism
                if (parent.LogScrollViewer.VerticalOffset >= (((TextBox)parent.LogScrollViewer.Content).ActualHeight - parent.LogScrollViewer.ActualHeight))
                {
                    parent.LogScrollViewer.ScrollToEnd();
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
}
