using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        internal ServerCore server;
        internal DateTime serverStartTime = DateTime.MaxValue;
        System.Timers.Timer uptimeUpdateTimer;
        internal ObservableCollection<AvailableKinectData> availableKinects = new ObservableCollection<AvailableKinectData>();
        private List<string> kinectsPageList = new List<string>(new string[] {"Available Kinects"});
        private List<KinectSettingsControl> kinectOptionGUIPages = new List<KinectSettingsControl>();

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

        #region Window Events
        private void Window_Initialized(object sender, EventArgs e)
        {
            //Setup the timer to update the GUI with the server runtime
            uptimeUpdateTimer = new System.Timers.Timer();
            uptimeUpdateTimer.Interval = 500;
            uptimeUpdateTimer.Elapsed += new System.Timers.ElapsedEventHandler(uptimeUpdateTimer_Elapsed);

            MasterSettings tempSettings = new MasterSettings();

            //TODO: FOR TESTING ONLY!!  Replace with an option on the GUI
            tempSettings.skeletonOptions.skeletonSortMode = SkeletonSortMethod.Closest;

            //TODO: Replace with an option on the skeleton tracking tab
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

            //Create the server core (this does NOT start the server)
            server = new ServerCore(verbose, tempSettings, this);

            //Set all the data for the data grids
            VoiceButtonDataGrid.ItemsSource = server.serverMasterOptions.voiceButtonCommands;
            VoiceTextDataGrid.ItemsSource = server.serverMasterOptions.voiceTextCommands;

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

            //TODO: Handle starting Kinects based on the loaded settings file
            //Initialize the data for the available Kinects
            for (int i = 0; i < KinectSensor.KinectSensors.Count; i++)
            {
                AvailableKinectData tempData = new AvailableKinectData();
                tempData.ConnectionID = KinectSensor.KinectSensors[i].DeviceConnectionId;
                tempData.Status = KinectSensor.KinectSensors[i].Status;
                if (i == 0 && tempData.Status == KinectStatus.Connected)
                {
                    tempData.UseKinect = true;
                    tempData.KinectID = 0;
                    server.kinects.Add(new KinectCore(server, this, (int)tempData.KinectID));
                }
                else
                {
                    tempData.UseKinect = false;
                    tempData.KinectID = null;
                }
                availableKinects.Add(tempData);
            }
            kinectsAvailableDataGrid.ItemsSource = availableKinects;
            availableKinects.CollectionChanged += availableKinects_CollectionChanged;
            UpdatePageListing();
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;

            if (startOnLaunch)
            {
                startServerButton_Click(this, new RoutedEventArgs());
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //TODO: Handle closing down all the kinects
            //server.shutdownServer();
            //if (kinect != null)
            //{
            //    kinect.ShutdownSensor();
            //}
        }
        #endregion

        #region Status Tab GUI Stuff
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
        void uptimeUpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            //This is on another thread...
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.RunTimeTextBlock.Text = ((TimeSpan)(DateTime.Now - serverStartTime)).ToString(@"dd\:hh\:mm\:ss");
            }), null
            );
        }
        #endregion

        #region Kinect Tab GUI Stuff
        //Event fires whenever a Kinect on the computer changes, this is used to keep the list of available Kinects up to date
        void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            for (int i = 0; i < availableKinects.Count; i++)
            {
                if (availableKinects[i].ConnectionID == e.Sensor.DeviceConnectionId)
                {
                    if (e.Sensor.Status != KinectStatus.Disconnected)
                    {
                        availableKinects[i].Status = e.Sensor.Status;
                        if (e.Sensor.Status != KinectStatus.Connected)
                        {
                            availableKinects[i].UseKinect = false;
                        }
                    }
                    else
                    {
                        availableKinects.RemoveAt(i);
                        
                        //Renumber all the Kinect IDs
                        int sensorNum = 0;
                        for (int j = 0; j < availableKinects.Count; j++)
                        {
                            if (availableKinects[j].UseKinect)
                            {
                                availableKinects[j].KinectID = sensorNum;
                                sensorNum++;
                            }
                        }
                    }
                    kinectsAvailableDataGrid.Items.Refresh();
                    return;
                }
            }

            AvailableKinectData tempData = new AvailableKinectData();
            tempData.ConnectionID = e.Sensor.DeviceConnectionId;
            tempData.KinectID = null;
            tempData.UseKinect = false;
            tempData.Status = e.Sensor.Status;
            availableKinects.Add(tempData);
            kinectsAvailableDataGrid.Items.Refresh();
        }
        //Updates the items in the Kinect settings tab listbox, this list is used to pick what is shown in the area next to the list box
        void UpdatePageListing()
        {
            kinectsPageList.RemoveRange(1, kinectsPageList.Count - 1); //Clear all but the first page, which we will always show

            for (int i = 0; i < availableKinects.Count; i++)
            {
                if (availableKinects[i].UseKinect)
                {
                    kinectsPageList.Add("Kinect " + availableKinects[i].KinectID.ToString());
                }
            }

            kinectTabListBox.ItemsSource = kinectsPageList;
        }
        //Changes which Kinect settings page is in view based on the selection in the list box
        private void kinectTabListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (kinectTabListBox.SelectedIndex >= 0 && kinectTabListBox.SelectedIndex <= kinectOptionGUIPages.Count)
            {
                if (kinectTabListBox.SelectedIndex == 0)
                {
                    for (int i = 0; i < kinectOptionGUIPages.Count; i++)
                    {
                        kinectOptionGUIPages[i].Visibility = System.Windows.Visibility.Collapsed;
                    }
                    kinectsAvailableDataGrid.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    kinectsAvailableDataGrid.Visibility = System.Windows.Visibility.Collapsed;
                    for (int i = 0; i < kinectOptionGUIPages.Count; i++)
                    {
                        if (kinectTabListBox.SelectedIndex - 1 == i)
                        {
                            kinectOptionGUIPages[i].Visibility = System.Windows.Visibility.Visible;
                        }
                        else
                        {
                            kinectOptionGUIPages[i].Visibility = System.Windows.Visibility.Collapsed;
                        }
                    }
                }
            }
        }
        #endregion

        #region Voice Recognition GUI Stuff
        private void VoiceButtonDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceButtonDataGrid.SelectedIndex = -1;
        }
        private void VoiceTextDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            VoiceTextDataGrid.SelectedIndex = -1;
        }
        #endregion

        #region Other Methods
        //Refreshes all the data on the GUI after a new settings file is loaded
        private void UpdateGUISettings()
        {
            VoiceTextDataGrid.ItemsSource = server.serverMasterOptions.voiceTextCommands;
            VoiceButtonDataGrid.ItemsSource = server.serverMasterOptions.voiceButtonCommands;

            //TODO: Add the rest of the GUI updates here.
        }
        #endregion

        private void kinectsAvailableDataGrid_CurrentCellChanged(object sender, EventArgs e)
        {
            Debug.WriteLine(e.ToString());
        }

        private void kinectsAvailableDataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            Debug.WriteLine(e.ToString());
        }
        void availableKinects_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Debug.WriteLine(e.Action.ToString());
        }

        private void kinectsAvailableDataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            kinectsAvailableDataGrid.SelectedIndex = -1;
        }

    }
}
