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
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
using Microsoft.Kinect;
using System.Diagnostics;

namespace KinectWithVRServer
{
    public partial class MainWindow : Window
    {
        internal string startupFile = "";
        internal bool verbose = false;
        internal bool startOnLaunch = false;
        //internal MasterSettings settings;
        internal ServerCore server;
        //internal KinectCore kinect;
        //int totalFrames = 0;
        //int lastFrames = 0;
        //bool isGUI = false;  //Shouldn't be needed, if this class exists, then it must be in GUI mode
        internal DateTime serverStartTime = DateTime.MaxValue;
        System.Timers.Timer uptimeUpdateTimer;

        public MainWindow(bool isVerbose, bool isAutoStart, string startSettings = "")
        {
            verbose = isVerbose;
            startOnLaunch = isAutoStart;
            startupFile = startSettings;

            InitializeComponent();
        }

        #region Menu Click Event Handlers
        private void OpenSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openDlg = new OpenFileDialog();
            openDlg.Filter = "XML File (*.xml)|*.xml|All Files|*.*";
            openDlg.FilterIndex = 0;
            openDlg.Multiselect = false;

            if ((bool)openDlg.ShowDialog())
            {
                try
                {
                    server.serverMasterOptions = HelperMethods.LoadSettings(openDlg.FileName);
                    UpdateGUISettings();
                }
                catch
                {
                    MessageBox.Show("Error: The settings file failed to open!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    HelperMethods.WriteToLog("Settings file (" + openDlg.FileName + ") failed to load.");
                }
            }
        }
        private void SaveSettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveDlg = new SaveFileDialog();
            saveDlg.Filter = "XML File (*.xml)|*.xml";

            if ((bool)saveDlg.ShowDialog())
            {
                try
                {
                    HelperMethods.SaveSettings(saveDlg.FileName, server.serverMasterOptions);
                }
                catch
                {
                    MessageBox.Show("Error: The settings file failed to save!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    HelperMethods.WriteToLog("Settings file (" + saveDlg.FileName + ") failed to save.");
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
                        MessageBox.Show("Error: The log file failed to save!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        HelperMethods.WriteToLog("Log file (" + saveDlg.FileName + ") failed to save.");
                    }

                    writer.Close();
                    writer.Dispose();
                }
            }
        }
        private void GenJCONFMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add jconf generating
            MessageBox.Show("Warning: The JCONF generation feature has not been implemented yet.  Sorry.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Kinect with VR (KiwiVR) Server\r\nCreated at the Virtual Reality Applications Center\r\nIowa State University\r\nBy Patrick Carlson, Diana Jarrell, and Tim Morgan.\r\nCopyright 2013", "About KiwiVR", MessageBoxButton.OK);
        }
        private void HelpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Add help
            MessageBox.Show("Warning: Help feature has not been implemented yet.  Sorry.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        #endregion

        private void Window_Initialized(object sender, EventArgs e)
        {
            //Setup the timer to update the GUI with the server runtime
            uptimeUpdateTimer = new System.Timers.Timer();
            uptimeUpdateTimer.Interval = 500;
            uptimeUpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(uptimeUpdateTimer_Elapsed);

            MasterSettings tempSettings = new MasterSettings();

            //FOR TESTING ONLY!!  Replace with an option on the GUI
            tempSettings.skeletonOptions.skeletonSortMode = SkeletonSortMethod.Closest;

            //Since skeleton tracking is on by default, add the buttons for the hands
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 2; j++) //We need a command for each hand
                {
                    GestureCommand gripCommand = new GestureCommand();
                    gripCommand.buttonNumber = j;
                    string handString = "_left";
                    if (j == 0)
                    {
                        handString = "_right";
                    }
                    gripCommand.comments = "Skeleton" + i.ToString() + handString;
                    gripCommand.gestureType = GestureType.Grip;
                    gripCommand.serverName = "Tracker0" + i.ToString();
                    gripCommand.skeletonNumber = i;
                    tempSettings.gestureCommands.Add(gripCommand);
                }
            }

            //MORE TESTING
            //VoiceButtonCommand testCommand = new VoiceButtonCommand();
            //testCommand.recognizedWord = "Hello";
            //testCommand.buttonNumber = 0;
            //testCommand.buttonType = ButtonType.Toggle;
            //testCommand.comments = "Test";
            //testCommand.confidence = 0.9;
            //testCommand.initialState = false;
            //testCommand.sendSourceAngle = false;
            //testCommand.serverName = "ButtonServe";
            ////testCommand.serverType = ServerType.Button;
            //testCommand.setState = true;
            //settings.voiceButtonCommands.Add(testCommand);
            //VoiceTextCommand testCommand2 = new VoiceTextCommand();
            //testCommand2.recognizedWord = "Goodbye";
            //testCommand2.comments = "Test2";
            //testCommand2.confidence = 0.9;
            //testCommand2.sendSourceAngle = false;
            //testCommand2.serverName = "ButtonServe";
            //testCommand2.actionText = "Good Riddance";
            ////testCommand2.serverType = ServerType.Button;
            //settings.voiceTextCommands.Add(testCommand2);

            //Create the server core (this does NOT start the server)
            server = new ServerCore(verbose, tempSettings, this);

            //Set all the data for the data grids
            VoiceButtonDataGrid.ItemsSource = server.serverMasterOptions.voiceButtonCommands;
            VoiceTextDataGrid.ItemsSource = server.serverMasterOptions.voiceTextCommands;

            //Start the Kinect to pass to the ServerCore; NOTE: This must be started BEFORE running the server core
            //kinect = new KinectCore(server, this);

            //BUG!!!  -> Because the kinect is also initialized inside the ServerCore code, the kinect is actually getting started TWICE
            //Open the Kinect

            KinectStatusBlock.Text = "1";

            if (startupFile != null && startupFile != "")
            {
                try
                {
                    server.serverMasterOptions = HelperMethods.LoadSettings(startupFile);
                    UpdateGUISettings();
                }
                catch
                {
                    MessageBox.Show("Error: The startup settings file failed to load!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    HelperMethods.WriteToLog("Startup settings (" + startupFile + ") failed to load.");
                }
            }
            if (startOnLaunch)
            {
                //server.launchServer(settings);
                startServerButton_Click(this, new RoutedEventArgs());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            server.shutdownServer();
            //if (kinect != null)
            //{
            //    kinect.ShutdownSensor();
            //}
        }

        //Receives checkbox input and orders seated mode. 
        public void SelectSeatedModeChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)ChooseSeatedModeButton.IsChecked)
            {
                server.serverMasterOptions.skeletonOptions.isSeatedMode = true;
                if (server.kinectCore != null)
                {
                    server.kinectCore.kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                }
            }
            else
            {
                server.serverMasterOptions.skeletonOptions.isSeatedMode = false;
                if (server.kinectCore != null)
                {
                    server.kinectCore.kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
                }
            }
        }

        //Receives checkbox input and orders near mode.
        public void SelectNearModeChanged(object sender, RoutedEventArgs e)
        {
            if ((bool)ChooseNearModeButton.IsChecked)
            {
                server.serverMasterOptions.kinectOptions.isNearMode = true;
                server.kinectCore.kinect.DepthStream.Range = DepthRange.Near;
            }
            else
            {
                server.serverMasterOptions.kinectOptions.isNearMode = false;
                server.kinectCore.kinect.DepthStream.Range = DepthRange.Default;
            }
        }

        private void startServerButton_Click(object sender, RoutedEventArgs e)
        {
            if (server != null)
            {
                if (server.isRunning)
                {
                    uptimeUpdateTimer.Stop();

                    server.stopServer();
                    startServerButton.Content = "Start";
                    ServerStatusItem.Content = "Server Stopped";
                    ServerStatusTextBlock.Text = "Stopped";
                    RunTimeTextBlock.Text = "0";
                }
                else
                {
                    //The server doesn't actually start until the callback from the voice core
                    startServerButton.IsEnabled = false;
                    startServerButton.Content = "...Starting"; //For some screwy reason, it reverses where the periods are on the button, which is why they are first here
                    ServerStatusItem.Content = "Server Starting...";
                    ServerStatusTextBlock.Text = "Starting...";

                    server.launchServer();

                    serverStartTime = DateTime.Now;
                    uptimeUpdateTimer.Start();
                }
            }
        }

        private void VoiceButtonDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceButtonDataGrid.SelectedIndex = -1;
        }

        private void VoiceTextDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceTextDataGrid.SelectedIndex = -1;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        //Refreshes all the data on the GUI after a new settings file is loaded
        private void UpdateGUISettings()
        {
            VoiceTextDataGrid.ItemsSource = server.serverMasterOptions.voiceTextCommands;
            VoiceButtonDataGrid.ItemsSource = server.serverMasterOptions.voiceButtonCommands;

            //TODO: Add the rest of the GUI updates here.
        }

        void uptimeUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //This is on another thread...
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.RunTimeTextBlock.Text = ((TimeSpan)(DateTime.Now - serverStartTime)).ToString(@"dd\:hh\:mm\:ss");
            }), null
            );
        }
    }
}
