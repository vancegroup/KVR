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
        internal string ColorStreamConnectionID = "";
        internal string DepthStreamConnectionID = "";
        System.Timers.Timer uptimeUpdateTimer;
        internal ObservableCollection<AvailableKinectData> availableKinects = new ObservableCollection<AvailableKinectData>();
        private List<string> kinectsPageList = new List<string>(new string[] {"Available Kinects"});
        internal List<KinectSettingsControl> kinectOptionGUIPages = new List<KinectSettingsControl>();

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
            //for (int i = 0; i < 6; i++)
            //{
            //    for (int j = 0; j < 2; j++) //We need a command for each hand
            //    {
            //        GestureCommand gripCommand = new GestureCommand();
            //        gripCommand.buttonNumber = j;
            //        string handString = "_left";
            //        if (j == 0)
            //        {
            //            handString = "_right";
            //        }
            //        gripCommand.comments = "Skeleton" + i.ToString() + handString;
            //        gripCommand.gestureType = GestureType.Grip;
            //        gripCommand.serverName = "Tracker0" + i.ToString();
            //        gripCommand.skeletonNumber = i;
            //        tempSettings.gestureCommands.Add(gripCommand);
            //    }
            //}

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
                    server.serverMasterOptions.kinectOptions.Add(new KinectSettings(tempData.ConnectionID, (int)tempData.KinectID));
                    server.kinects.Add(new KinectCore(server, this, (int)tempData.KinectID));
                }
                else
                {
                    tempData.UseKinect = false;
                    tempData.KinectID = null;
                }
                tempData.PropertyChanged += useKinect_PropertyChanged;
                availableKinects.Add(tempData);
            }
            kinectsAvailableDataGrid.ItemsSource = availableKinects;
            UpdatePageListing();
            GenerateImageSourcePickerLists();
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;

            if (startOnLaunch)
            {
                startServerButton_Click(this, new RoutedEventArgs());
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (server.isRunning)
            {
                server.stopServer();
            }

            for (int i = 0; i < server.kinects.Count; i++)
            {
                server.kinects[i].ShutdownSensor();
            }
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
                        availableKinects[i].PropertyChanged -= useKinect_PropertyChanged;
                        availableKinects.RemoveAt(i);

                        renumberKinectIDs();
                    }
                    kinectsAvailableDataGrid.Items.Refresh();
                    return;
                }
            }

            AvailableKinectData tempData = new AvailableKinectData();
            tempData.ConnectionID = e.Sensor.DeviceConnectionId;
            tempData.KinectID = null;
            tempData.UseKinect = false;
            tempData.PropertyChanged += useKinect_PropertyChanged;
            tempData.Status = e.Sensor.Status;
            availableKinects.Add(tempData);
            kinectsAvailableDataGrid.Items.Refresh();
        }
        //Renumbers the Kinect IDs
        private void renumberKinectIDs()
        {
            int sensorNum = 0;
            for (int j = 0; j < availableKinects.Count; j++)
            {
                if (availableKinects[j].UseKinect)
                {
                    availableKinects[j].KinectID = sensorNum;
                    sensorNum++;
                }
                else
                {
                    availableKinects[j].KinectID = null;
                }
            }
        }
        //Updates the items in the Kinect settings tab listbox, this list is used to pick what is shown in the area next to the list box
        void UpdatePageListing()
        {
            kinectsPageList.RemoveRange(1, kinectsPageList.Count - 1); //Clear all but the first page, which we will always show

            for (int i = 0; i < availableKinects.Count; i++)
            {
                if (availableKinects[i].UseKinect)
                {
                    //Check if the GUI page exists, and create it if it doesn't exist
                    bool exists = false;
                    for (int j = 0; j < kinectOptionGUIPages.Count; j++)
                    {
                        if (kinectOptionGUIPages[j].ConnectionID == availableKinects[i].ConnectionID)
                        {
                            exists = true;
                            kinectOptionGUIPages[j].KinectID = availableKinects[i].KinectID;
                            break;
                        }
                    }
                    if (!exists)
                    {
                        kinectOptionGUIPages.Add(new KinectSettingsControl(availableKinects[i].KinectID, availableKinects[i].ConnectionID, verbose, this));
                    }
                    kinectsPageList.Add("Kinect " + availableKinects[i].KinectID.ToString());
                }
                else
                {
                    //Check if the GUI exist for the one we are removing, and set the Kinect ID to null so it will go to the end
                    for (int j = 0; j < kinectOptionGUIPages.Count; j++)
                    {
                        if (kinectOptionGUIPages[j].ConnectionID == availableKinects[i].ConnectionID)
                        {
                            kinectOptionGUIPages[j].KinectID = null;
                            break;
                        }
                    }
                }
            }

            kinectOptionGUIPages.Sort(new KinectSettingsControlComparer());
            kinectTabListBox.ItemsSource = kinectsPageList;
            kinectTabListBox.Items.Refresh();
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
        //Updates the Kinect list when the property is changed
        void useKinect_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UseKinect")
            {
                renumberKinectIDs();
                reorderKinectSettings();
                launchAndKillKinects();
                UpdatePageListing();
                GenerateImageSourcePickerLists();

                //FOR DEBUGGING ONLY!!!!
                WriteOutKinectOrders();
            }
        }
        //Reorder the Kinect Settings
        private void reorderKinectSettings()
        {
            for (int i = 0; i < availableKinects.Count; i++)
            {
                if (availableKinects[i].UseKinect)
                {
                    bool found = false;
                    for (int j = 0; j < server.serverMasterOptions.kinectOptions.Count; j++)
                    {
                        if (availableKinects[i].ConnectionID == server.serverMasterOptions.kinectOptions[j].connectionID)
                        {
                            server.serverMasterOptions.kinectOptions[j].kinectID = (int)availableKinects[i].KinectID;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        server.serverMasterOptions.kinectOptions.Add(new KinectSettings(availableKinects[i].ConnectionID, (int)availableKinects[i].KinectID));
                    }
                }
                else
                {
                    for (int j = 0; j < server.serverMasterOptions.kinectOptions.Count; j++)
                    {
                        if (availableKinects[i].ConnectionID == server.serverMasterOptions.kinectOptions[j].connectionID)
                        {
                            server.serverMasterOptions.kinectOptions.RemoveAt(j);
                        }
                    }
                }
            }
            server.serverMasterOptions.kinectOptions.Sort(new KinectSettingsComparer());
        }
        //Updates which Kinects are running based on the selections in the available Kinects data grid
        private void launchAndKillKinects()
        {
            for (int i = 0; i < availableKinects.Count; i++)
            {
                if (availableKinects[i].UseKinect)
                {
                    //If the Kinect is to be used, check and see if it exists, and launch it if it doesn't
                    bool kinectFound = false;
                    for (int j = 0; j < server.kinects.Count; j++)
                    {
                        if (server.kinects[j].kinect.DeviceConnectionId == availableKinects[i].ConnectionID)
                        {
                            server.kinects[j].kinectID = (int)availableKinects[i].KinectID;
                            kinectFound = true;
                            break;
                        }
                    }
                    if (!kinectFound)
                    {
                        server.kinects.Add(new KinectCore(server, this, availableKinects[i].KinectID));
                    }
                }
                else
                {
                    //If the Jinect is not to be used, check and see if it exists, and destroy it if it does
                    for (int j = 0; j < server.kinects.Count; j++)
                    {
                        if (server.kinects[j].kinect.DeviceConnectionId == availableKinects[i].ConnectionID)
                        {
                            server.kinects[j].ShutdownSensor();
                            server.kinects.RemoveAt(j);
                            break;
                        }
                    }
                }
            }
            server.kinects.Sort(new KinectCoreComparer());
        }
        //TODO: REMOVE (FOR DEBUGGING ONLY)
        private void WriteOutKinectOrders()
        {
            Debug.WriteLine("Available Kinects:");
            for (int i = 0; i < availableKinects.Count; i++)
            {
                Debug.WriteLine(availableKinects[i].KinectID.ToString() + ":   " + availableKinects[i].ConnectionID);
            }

            Debug.WriteLine("Running Kinects:");
            for (int i = 0; i < server.kinects.Count; i++)
            {
                Debug.WriteLine(server.kinects[i].kinectID.ToString() + ":   " + server.kinects[i].kinect.DeviceConnectionId);
            }

            Debug.WriteLine("Kinect Setting:");
            for (int i = 0; i < server.serverMasterOptions.kinectOptions.Count; i++)
            {
                Debug.WriteLine(server.serverMasterOptions.kinectOptions[i].kinectID.ToString() + ":   " + server.serverMasterOptions.kinectOptions[i].connectionID);
            }

            Debug.WriteLine("GUI Pages:");
            for (int i = 0; i < kinectOptionGUIPages.Count; i++)
            {
                Debug.WriteLine(kinectOptionGUIPages[i].KinectID.ToString() + ":   " + kinectOptionGUIPages[i].ConnectionID);
            }
        }
        #endregion

        #region Skeleton Tab GUI Stuff

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
        private void ColorSourcePickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ColorSourcePickerComboBox.SelectedItem != null)
            {
                if (ColorSourcePickerComboBox.SelectedItem.ToString().ToLower() == "none")
                {
                    ColorStreamConnectionID = "";
                }
                else
                {
                    string temp = ColorSourcePickerComboBox.SelectedItem.ToString().ToLower().Replace("kinect ", "");
                    int kinectIndex = -1;
                    if (int.TryParse(temp, out kinectIndex))
                    {
                        ColorStreamConnectionID = server.kinects[kinectIndex].kinect.DeviceConnectionId;
                    }
                }

                //TODO: Update the writeable bitmap, if necessary
            }
        }
        private void DepthSourcePickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepthSourcePickerComboBox.SelectedItem != null)
            {
                if (DepthSourcePickerComboBox.SelectedItem.ToString().ToLower() == "none")
                {
                    DepthStreamConnectionID = "";
                }
                else
                {
                    string temp = DepthSourcePickerComboBox.SelectedItem.ToString().ToLower().Replace("kinect ", "");
                    int kinectIndex = -1;
                    if (int.TryParse(temp, out kinectIndex))
                    {
                        DepthStreamConnectionID = server.kinects[kinectIndex].kinect.DeviceConnectionId;
                    }
                }
            }

            //TODO:  Update the writeable bitmap, if necessary
        }
        private void GenerateImageSourcePickerLists()
        {
            ColorSourcePickerComboBox.Items.Clear();
            DepthSourcePickerComboBox.Items.Clear();
            ColorSourcePickerComboBox.Items.Add("None");
            DepthSourcePickerComboBox.Items.Add("None");

            bool colorFound = false;
            bool depthFound = false;
            for (int i = 0; i < server.kinects.Count; i++)
            {
                if (server.kinects[i].kinect.ColorStream.IsEnabled)
                {
                    ColorSourcePickerComboBox.Items.Add("Kinect " + server.kinects[i].kinectID);
                    if (server.kinects[i].kinect.DeviceConnectionId == ColorStreamConnectionID)
                    {
                        ColorSourcePickerComboBox.SelectedIndex = i;
                        colorFound = true;
                    }
                }
                if (server.kinects[i].kinect.DepthStream.IsEnabled)
                {
                    DepthSourcePickerComboBox.Items.Add("Kinect " + server.kinects[i].kinectID);
                    if (server.kinects[i].kinect.DeviceConnectionId == DepthStreamConnectionID)
                    {
                        DepthSourcePickerComboBox.SelectedIndex = i;
                        depthFound = true;
                    }
                }
            }

            if (!colorFound)
            {
                ColorSourcePickerComboBox.SelectedIndex = 0;
            }
            if (!depthFound)
            {
                DepthSourcePickerComboBox.SelectedIndex = 0;
            }
        }
        #endregion

    }
}
