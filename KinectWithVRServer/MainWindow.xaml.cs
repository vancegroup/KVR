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
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using Microsoft.Win32;
using Microsoft.Kinect;

namespace KinectWithVRServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal string startupFile = "";
        internal bool verbose = false;
        internal bool startOnLaunch = false;
        internal MasterSettings settings;
        internal ServerCore server;
        internal KinectCore kinect;

        public MainWindow(bool isVerbose, bool isAutoStart)
        {
            verbose = isVerbose;
            startOnLaunch = isAutoStart;

            InitializeComponent();
        }

        #region Menu Click Event Handlers
        private void OpenSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MasterSettings));

            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "XML File (*.xml)|*.xml|All Files|*.*";
            openDlg.FilterIndex = 0;
            openDlg.Multiselect = false;

            if ((bool)openDlg.ShowDialog())
            {
                using (FileStream file = new FileStream(openDlg.FileName, FileMode.Open))
                {
                    try
                    {
                        settings = (MasterSettings)serializer.Deserialize(file);
                    }
                    catch
                    {
                        MessageBox.Show("Cannot open specified file.");
                    }
                }
            }
        }
        private void SaveSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(MasterSettings));

            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "XML File (*.xml)|*.xml";

            if ((bool)saveDlg.ShowDialog())
            {
                using (FileStream file = new FileStream(saveDlg.FileName, FileMode.Create))
                {
                    try
                    {
                        serializer.Serialize(file, settings);
                    }
                    catch
                    {
                        MessageBox.Show("Uhh, something went wrong.");
                    }
                }
            }
        }
        private void SaveLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "Text File (*.txt)|*.txt";

            if ((bool)saveDlg.ShowDialog())
            {
                using (FileStream file = new FileStream(saveDlg.FileName, FileMode.Create))
                {
                    StreamWriter writer = new StreamWriter(file);

                    try
                    {
                        writer.Write(LogTextBox.Text.ToCharArray());
                    }
                    catch
                    {
                        MessageBox.Show("Uhh, something went wrong.");
                    }

                    writer.Close();
                    writer.Dispose();
                }
            }
        }
        private void GenJCONFMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add jconf generating
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kinect with VR (KiwiVR) Server\r\nCreated at the Virtual Reality Applications Center\r\nIowa State University\r\nBy Patrick Carlson, Tim Morgan, and Diana Jarrell\r\nCopyright 2013", "About KiwiVR", MessageBoxButton.OK);
        }
        private void HelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add help
        }
        #endregion

        private void Window_Initialized(object sender, EventArgs e)
        {
            settings = new MasterSettings();

            //FOR TESTING ONLY!!
            VoiceButtonCommand testCommand = new VoiceButtonCommand();
            testCommand.recognizedWord = "Hello";
            testCommand.buttonNumber = 0;
            testCommand.buttonType = ButtonType.Toggle;
            testCommand.commandName = "Test";
            testCommand.confidence = 0.9;
            testCommand.initialState = false;
            testCommand.sendSourceAngle = false;
            testCommand.serverName = "ButtonServe";
            //testCommand.serverType = ServerType.Button;
            testCommand.setState = true;
            settings.voiceButtonCommands.Add(testCommand);
            VoiceTextCommand testCommand2 = new VoiceTextCommand();
            testCommand2.recognizedWord = "Goodbye";
            testCommand2.commandName = "Test2";
            testCommand2.confidence = 0.9;
            testCommand2.sendSourceAngle = false;
            testCommand2.serverName = "ButtonServe";
            testCommand2.actionText = "Good Riddance";
            //testCommand2.serverType = ServerType.Button;
            settings.voiceTextCommands.Add(testCommand2);

            //Set all the data for the data grids
            VoiceButtonDataGrid.ItemsSource = settings.voiceButtonCommands;
            VoiceTextDataGrid.ItemsSource = settings.voiceTextCommands;

            //Create the server core (this does NOT start the server)
            server = new ServerCore(verbose, kinect, this);

            //Open the Kinect
            kinect = new KinectCore(server, this);

            if (startupFile != null && startupFile != "")
            {
                //TODO: Load the initial data file
            }
            if (startOnLaunch)
            {
                server.launchServer(settings);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (kinect != null)
            {
                kinect.ShutdownSensor();
            }
        }

        //Receives checkbox input and orders seated mode. 
        public void SelectSeatedModeChanged(object sender, RoutedEventArgs e)
        {    
            if (kinect != null)
            {
                if (this.ChooseSeatedModeButton.IsChecked.GetValueOrDefault())
                {
                    KinectCore.seatedmode = true;
                }
                else
                {
                    KinectCore.seatedmode = false;
                }

                kinect.CheckSeated();
            }
        }

        public void SelectNearModeChanged(object sender, RoutedEventArgs e)
        {
            if (kinect != null)
            {
                if (this.ChooseNearModeButton.IsChecked.GetValueOrDefault())
                {
                    KinectCore.nearmode = true;
                }
                else
                {
                    KinectCore.nearmode = false;
                }

                kinect.CheckNear();
            }
        }

        private void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (server != null)
            {
                if (server.isRunning)
                {
                    server.shutdownServer();
                    startServerButton.Content = "Start";
                }
                else
                {
                    server.launchServer(settings);
                    startServerButton.Content = "Stop";
                }
            }
        }

        internal void WriteToLog(string text)
        {
            LogTextBox.Text += "\r\n";
            LogTextBox.Text += DateTime.Now.ToString() + ": ";
            LogTextBox.Text += text;
        }

        private void VoiceButtonDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceButtonDataGrid.SelectedIndex = -1;
        }

        private void VoiceTextDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceTextDataGrid.SelectedIndex = -1;
        }
    }
}
