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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.IO;
using Microsoft.Win32;
using Microsoft.Kinect;
using System.Diagnostics;
using System.Security.Permissions;

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
        private string voiceRecogSourceConnectionID = "";

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
                    server.serverMasterOptions.kinectOptionsList.Add(new KinectSettings(tempData.ConnectionID, (int)tempData.KinectID));
                    server.kinects.Add(new KinectCore(server, this, (int)tempData.KinectID));
                    tempData.ServerStatus = "Running";
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

            //Populate the skeleton data and set the binding for the data grid
            GenerateSkeletonDataGridData();
            SkeletonSettingsDataGrid.ItemsSource = server.serverMasterOptions.skeletonOptions.individualSkeletons;

            //Populate and setup the voice recognition lists
            GenerateVoiceRecogEngineList();
            GenerateAudioSourceList();
            VoiceKinectComboBox.SelectedIndex = 0;

            //Set defaults where needed
            FeedbackJointTypeComboBox.SelectedIndex = 0;
            SkelSortModeComboBox.SelectedIndex = 5;

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
                //TODO: Handle if a Kinect is unplugged from the system (which doesn't call this method but can break things badly)
                //Note, there is some logic to the order these are called in, do not rearrange without understanding that logic!
                renumberKinectIDs();
                reorderKinectSettings();
                launchAndKillKinects();
                UpdatePageListing();
                GenerateImageSourcePickerLists();
                GenerateAudioSourceList();
                GenerateSkeletonDataGridData();
                //FOR DEBUGGING ONLY!!!!
                //WriteOutKinectOrders();
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
                    for (int j = 0; j < server.serverMasterOptions.kinectOptionsList.Count; j++)
                    {
                        if (availableKinects[i].ConnectionID == server.serverMasterOptions.kinectOptionsList[j].connectionID)
                        {
                            server.serverMasterOptions.kinectOptionsList[j].kinectID = (int)availableKinects[i].KinectID;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        server.serverMasterOptions.kinectOptionsList.Add(new KinectSettings(availableKinects[i].ConnectionID, (int)availableKinects[i].KinectID));
                    }
                }
                else
                {
                    for (int j = 0; j < server.serverMasterOptions.kinectOptionsList.Count; j++)
                    {
                        if (availableKinects[i].ConnectionID == server.serverMasterOptions.kinectOptionsList[j].connectionID)
                        {
                            server.serverMasterOptions.kinectOptionsList.RemoveAt(j);
                        }
                    }
                }
            }

            server.serverMasterOptions.kinectOptionsList.Sort(new KinectSettingsComparer());
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
                        availableKinects[i].ServerStatus = "Starting";
                        kinectsAvailableDataGrid.Items.Refresh();
                        kinectsAvailableDataGrid.InvalidateVisual();
                        System.Threading.Thread.Sleep(10); //Yes, it is a dirty hack, but it is the only way I can find to get the GUI to update reliably
                        ForceGUIUpdate();
                        server.kinects.Add(new KinectCore(server, this, availableKinects[i].KinectID));
                        availableKinects[i].ServerStatus = "Running";
                    }
                }
                else
                {
                    //If the Kinect is not to be used, check and see if it exists, and destroy it if it does
                    for (int j = 0; j < server.kinects.Count; j++)
                    {
                        if (server.kinects[j].kinect.DeviceConnectionId == availableKinects[i].ConnectionID)
                        {
                            availableKinects[i].ServerStatus = "Stopping";
                            kinectsAvailableDataGrid.Items.Refresh();
                            kinectsAvailableDataGrid.UpdateLayout();
                            System.Threading.Thread.Sleep(10);
                            ForceGUIUpdate();
                            server.kinects[j].ShutdownSensor();
                            server.kinects.RemoveAt(j);
                            availableKinects[i].ServerStatus = "Stopped";
                            break;
                        }
                    }
                }
            }
            server.kinects.Sort(new KinectCoreComparer());
        }

        private void ForceGUIUpdate()
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Render, new DispatcherOperationCallback(delegate(object parameter)
            {
                frame.Continue = false;
                return null;
            }), null);

            Dispatcher.PushFrame(frame);
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
            for (int i = 0; i < server.serverMasterOptions.kinectOptionsList.Count; i++)
            {
                Debug.WriteLine(server.serverMasterOptions.kinectOptionsList[i].kinectID.ToString() + ":   " + server.serverMasterOptions.kinectOptionsList[i].connectionID);
            }

            Debug.WriteLine("GUI Pages:");
            for (int i = 0; i < kinectOptionGUIPages.Count; i++)
            {
                Debug.WriteLine(kinectOptionGUIPages[i].KinectID.ToString() + ":   " + kinectOptionGUIPages[i].ConnectionID);
            }
        }
        //Handles the linking of the connection status hyperlinks to the help messages
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Link this to the pages in the help file for each error
            KinectStatus status = availableKinects[kinectsAvailableDataGrid.SelectedIndex].Status;
            switch (status)
            {
                case KinectStatus.Connected:
                {
                    //No help is needed if the Kinect is connected properly
                    break;
                }
                case KinectStatus.DeviceNotGenuine:
                {
                    MessageBox.Show("The connected device is not genuine.  Please attach a genuine Kinect.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.DeviceNotSupported:
                {
                    MessageBox.Show("The connected device is not supported.  Please attach a Kinect sensor.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.Disconnected:
                {
                    MessageBox.Show("The Kinect device is disconnected.  Please reconnect the Kinect sensor.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.Error:
                {
                    MessageBox.Show("An unknown error has occured with the Kinect.  Try restarting the computer?", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.Initializing:
                {
                    MessageBox.Show("The Kinect is initializing.  Please wait, the Kinect should be operational momentarily.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.InsufficientBandwidth:
                {
                    MessageBox.Show("There is not enough bandwidth available on this USB port for the Kinect.  Please move the Kinect to a different USB root node.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.NotPowered:
                {
                    MessageBox.Show("The Kinect is not connected to AC power.  Please plug in the Kinect's AC adapter.", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.NotReady:
                {
                    MessageBox.Show("The Kinect is not ready to run.  Please wait, the Kinect should be operational momentarily.  If not, try restarting the computer?", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
                case KinectStatus.Undefined:
                {
                    MessageBox.Show("An unknown error has occured with the Kinect.  Try restarting the computer?", "Connection Help", MessageBoxButton.OK, MessageBoxImage.Warning);
                    break;
                }
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
        private void VoiceKinectComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VoiceKinectComboBox.SelectedIndex == VoiceKinectComboBox.Items.Count - 1)
            {
                server.serverMasterOptions.audioOptions.sourceID = -1;
                voiceRecogSourceConnectionID = "";
            }
            else
            {
                server.serverMasterOptions.audioOptions.sourceID = VoiceKinectComboBox.SelectedIndex;
                voiceRecogSourceConnectionID = server.serverMasterOptions.kinectOptionsList[VoiceKinectComboBox.SelectedIndex].connectionID;
            }
        }
        private void VoiceRecognitionEngineComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ReadOnlyCollection<Microsoft.Speech.Recognition.RecognizerInfo> allRecognizers = Microsoft.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers();
            server.serverMasterOptions.audioOptions.recognizerEngineID = allRecognizers[VoiceRecognitionEngineComboBox.SelectedIndex].Id;
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
                    ColorImage.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    string temp = ColorSourcePickerComboBox.SelectedItem.ToString().ToLower().Replace("kinect ", "");
                    int kinectIndex = -1;
                    if (int.TryParse(temp, out kinectIndex))
                    {
                        ColorStreamConnectionID = server.kinects[kinectIndex].kinect.DeviceConnectionId;
                        ColorImage.Source = server.kinects[kinectIndex].colorImage;
                        ColorImage.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }
        private void DepthSourcePickerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DepthSourcePickerComboBox.SelectedItem != null)
            {
                if (DepthSourcePickerComboBox.SelectedItem.ToString().ToLower() == "none")
                {
                    DepthStreamConnectionID = "";
                    DepthImage.Visibility = System.Windows.Visibility.Hidden;
                }
                else
                {
                    string temp = DepthSourcePickerComboBox.SelectedItem.ToString().ToLower().Replace("kinect ", "");
                    int kinectIndex = -1;
                    if (int.TryParse(temp, out kinectIndex))
                    {
                        DepthStreamConnectionID = server.kinects[kinectIndex].kinect.DeviceConnectionId;
                        DepthImage.Source = server.kinects[kinectIndex].depthImage;
                        DepthImage.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
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
                        ColorSourcePickerComboBox.SelectedIndex = i + 1;
                        colorFound = true;
                    }
                }
                if (server.kinects[i].kinect.DepthStream.IsEnabled)
                {
                    DepthSourcePickerComboBox.Items.Add("Kinect " + server.kinects[i].kinectID);
                    if (server.kinects[i].kinect.DeviceConnectionId == DepthStreamConnectionID)
                    {
                        DepthSourcePickerComboBox.SelectedIndex = i + 1;
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
        private void GenerateAudioSourceList()
        {
            bool sourceFound = false;

            VoiceKinectComboBox.Items.Clear();

            for (int i = 0; i < server.kinects.Count; i++)
            {
                VoiceKinectComboBox.Items.Add("Kinect " + server.kinects[i].kinectID);
                if (server.kinects[i].kinect.DeviceConnectionId == voiceRecogSourceConnectionID)
                {
                    VoiceKinectComboBox.SelectedIndex = i;
                    sourceFound = true;
                }
            }
            VoiceKinectComboBox.Items.Add("System Default");

            if (!sourceFound)
            {
                VoiceKinectComboBox.SelectedIndex = VoiceKinectComboBox.Items.Count - 1;
            }
        }
        private void GenerateVoiceRecogEngineList()
        {
            ReadOnlyCollection<Microsoft.Speech.Recognition.RecognizerInfo> allRecognizers = Microsoft.Speech.Recognition.SpeechRecognitionEngine.InstalledRecognizers();
            VoiceRecognitionEngineComboBox.Items.Clear();
            for (int i = 0; i < allRecognizers.Count; i++)
            {
                VoiceRecognitionEngineComboBox.Items.Add(allRecognizers[i].Name);
                if (allRecognizers[i].Name.ToLower().Contains("kinect"))
                {
                    VoiceRecognitionEngineComboBox.SelectedIndex = i;
                }
            }
        }
        //Rejects any points that are not numbers or control characters or a period
        private void floatNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!HelperMethods.NumberKeys.Contains(e.Key) && e.Key != Key.OemPeriod && e.Key != Key.Decimal)
            {
                e.Handled = true;
            }
        }
        //Rejects any points that are not numbers or control charactes
        private void intNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!HelperMethods.NumberKeys.Contains(e.Key))
            {
                e.Handled = true;
            }
        }
        #endregion

        #region Skeleton Rendering Methods
        private void DrawBoneOnColor(Joint startJoint, Joint endJoint, Color boneColor, double thickness, Point offset, int kinectID, Matrix3D transform)
        {
            if (startJoint.TrackingState == JointTrackingState.Tracked && endJoint.TrackingState == JointTrackingState.Tracked)
            {
                //Undo the transform from the skeleton merging
                SkeletonPoint skelStartPoint = transformSkeletonPoint(startJoint.Position, transform);
                SkeletonPoint skelEndPoint = transformSkeletonPoint(endJoint.Position, transform);

                //Map the joint from the skeleton to the color image
                ColorImagePoint startPoint = server.kinects[kinectID].mapper.MapSkeletonPointToColorPoint(skelStartPoint, server.kinects[kinectID].kinect.ColorStream.Format);
                ColorImagePoint endPoint = server.kinects[kinectID].mapper.MapSkeletonPointToColorPoint(skelEndPoint, server.kinects[kinectID].kinect.ColorStream.Format);

                //Calculate the coordinates on the image (the offset of the image is added in the next section)
                Point imagePointStart = new Point(0.0, 0.0);
                imagePointStart.X = ((double)startPoint.X / (double)server.kinects[kinectID].kinect.ColorStream.FrameWidth) * ColorImage.ActualWidth;
                imagePointStart.Y = ((double)startPoint.Y / (double)server.kinects[kinectID].kinect.ColorStream.FrameHeight) * ColorImage.ActualHeight;
                Point imagePointEnd = new Point(0.0, 0.0);
                imagePointEnd.X = ((double)endPoint.X / (double)server.kinects[kinectID].kinect.ColorStream.FrameWidth) * ColorImage.ActualWidth;
                imagePointEnd.Y = ((double)endPoint.Y / (double)server.kinects[kinectID].kinect.ColorStream.FrameHeight) * ColorImage.ActualHeight;

                //Generate the line for the bone
                Line line = new Line();
                line.Stroke = new SolidColorBrush(boneColor);
                line.StrokeThickness = thickness;
                line.X1 = imagePointStart.X + offset.X;
                line.X2 = imagePointEnd.X + offset.X;
                line.Y1 = imagePointStart.Y + offset.Y;
                line.Y2 = imagePointEnd.Y + offset.Y;
                ColorImageCanvas.Children.Add(line);
            }
        }
        private void DrawJointPointOnColor(Joint joint, Color jointColor, double radius, Point offset, int kinectID, Matrix3D transform)
        {
            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                //Undo the transform from the skeleton merging
                SkeletonPoint skelPoint = transformSkeletonPoint(joint.Position, transform);

                //Map the joint from the skeleton to the color image
                ColorImagePoint point = server.kinects[kinectID].mapper.MapSkeletonPointToColorPoint(skelPoint, server.kinects[kinectID].kinect.ColorStream.Format);

                //Calculate the coordinates on the image (the offset is also added in this section)
                Point imagePoint = new Point(0.0, 0.0);
                imagePoint.X = ((double)point.X / (double)server.kinects[kinectID].kinect.ColorStream.FrameWidth) * ColorImage.ActualWidth + offset.X;
                imagePoint.Y = ((double)point.Y / (double)server.kinects[kinectID].kinect.ColorStream.FrameHeight) * ColorImage.ActualHeight + offset.Y;

                //Generate the circle for the joint
                Ellipse circle = new Ellipse();
                circle.Fill = new SolidColorBrush(jointColor);
                circle.StrokeThickness = 0.0;
                circle.Margin = new Thickness(imagePoint.X - radius, imagePoint.Y - radius, 0, 0);
                circle.HorizontalAlignment = HorizontalAlignment.Left;
                circle.VerticalAlignment = VerticalAlignment.Top;
                circle.Height = radius * 2;
                circle.Width = radius * 2;
                ColorImageCanvas.Children.Add(circle);
            }
        }
        internal void RenderSkeletonOnColor(Skeleton skeleton, Color renderColor)
        {
            if (ColorStreamConnectionID != null && ColorStreamConnectionID != "")
            {
                //Get the Kinect ID of the currently in view color stream
                int inViewKinectID = -1;
                bool found = false;
                for (int i = 0; i < server.kinects.Count; i++)
                {
                    if (ColorStreamConnectionID == server.kinects[i].kinect.DeviceConnectionId)
                    {
                        found = true;
                        inViewKinectID = i;
                        break;
                    }
                }

                if (found)
                {
                    //Calculate the offset
                    Point offset = new Point(0.0, 0.0);
                    if (ColorImageCanvas.ActualWidth != ColorImage.ActualWidth)
                    {
                        offset.X = (ColorImageCanvas.ActualWidth - ColorImage.ActualWidth) / 2;
                    }

                    if (ColorImageCanvas.ActualHeight != ColorImage.ActualHeight)
                    {
                        offset.Y = (ColorImageCanvas.ActualHeight - ColorImage.ActualHeight) / 2;
                    }

                    //Invert the transform done to put skeletons on a universal coordinate system
                    Matrix3D invertMat = server.kinects[inViewKinectID].skeletonTransformation;
                    invertMat.Invert();

                    //Render all the bones (this can't be looped because the enum isn't ordered in order of bone connections)
                    DrawBoneOnColor(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight], renderColor, 2.0, offset, inViewKinectID, invertMat);
                    DrawBoneOnColor(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight], renderColor, 2.0, offset, inViewKinectID, invertMat);

                    foreach (Joint joint in skeleton.Joints)
                    {
                        DrawJointPointOnColor(joint, renderColor, 2.0, offset, inViewKinectID, invertMat);
                    }
                }
            }
        }

        private SkeletonPoint transformSkeletonPoint(SkeletonPoint position, Matrix3D rotation)
        {
            Point3D adjustedVector = new Point3D(position.X, position.Y, position.Z);
            adjustedVector = Point3D.Multiply(adjustedVector, rotation);
            SkeletonPoint adjustedPoint = new SkeletonPoint();
            adjustedPoint.X = (float)adjustedVector.X;
            adjustedPoint.Y = (float)adjustedVector.Y;
            adjustedPoint.Z = (float)adjustedVector.Z;
            return adjustedPoint;
        }
        #endregion

        #region Skeleton GUI methods
        //Updates the data for the skeletons to reflect that a maximum of 6 times the number of kinects in use skeletons are available (only 1/2 of those skeletons support full skeleton tracking)
        private void GenerateSkeletonDataGridData()
        {
            if (server.kinects.Count * 6 > server.serverMasterOptions.skeletonOptions.individualSkeletons.Count) //Add skeleton settings
            {
                for (int i = server.serverMasterOptions.skeletonOptions.individualSkeletons.Count; i < server.kinects.Count * 6; i++)
                {
                    PerSkeletonSettings temp = new PerSkeletonSettings(); //Fill the skeleton information with the default settings
                    string tempServer = "Tracker" + i.ToString();
                    temp.skeletonNumber = i;
                    temp.serverName = tempServer;
                    temp.renderColor = AutoPickSkeletonRenderColor(i);
                    temp.useSkeleton = true;
                    temp.useRightHandGrip = true;
                    temp.rightGripServerName = tempServer;
                    temp.rightGripButtonNumber = 0;
                    temp.useLeftHandGrip = true;
                    temp.leftGripServerName = tempServer;
                    temp.leftGripButtonNumber = 1;
                    server.serverMasterOptions.skeletonOptions.individualSkeletons.Add(temp);
                }
            }
            else if (server.kinects.Count * 6 < server.serverMasterOptions.skeletonOptions.individualSkeletons.Count) //Remove skeleton settings
            {
                for (int i = server.serverMasterOptions.skeletonOptions.individualSkeletons.Count - 1; i >= server.kinects.Count * 6; i--)
                {
                    server.serverMasterOptions.skeletonOptions.individualSkeletons.RemoveAt(i);
                }
            }
            SkeletonSettingsDataGrid.Items.Refresh();
        }
        //Select a predefined render color based on the skeleton index
        private Color AutoPickSkeletonRenderColor(int index)
        {
            switch (index)
            {
                case 0:
                {
                    return Colors.Red;
                }
                case 1:
                {
                    return Colors.Blue;
                }
                case 2:
                {
                    return Colors.Green;
                }
                case 3:
                {
                    return Colors.Yellow;
                }
                case 4:
                {
                    return Colors.Cyan;
                }
                case 5:
                {
                    return Colors.Fuchsia;
                }
                case 6:
                {
                    return Colors.Orange;
                }
                case 7:
                {
                    return Colors.Brown;
                }
                case 8:
                {
                    return Colors.LightSkyBlue;
                }
                case 9:
                {
                    return Colors.LimeGreen;
                }
                case 10:
                {
                    return Colors.Purple;
                }
                case 11:
                {
                    return Colors.White;
                }
            }
            return Colors.Black;
        }
        //Changes if the skeleton tracking is in seated mode
        private void ChooseSeatedCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            server.serverMasterOptions.skeletonOptions.isSeatedMode = (bool)ChooseSeatedCheckBox.IsChecked;
        }
        //Controls which skeleton sorting mode is used
        private void SkelSortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            server.serverMasterOptions.skeletonOptions.skeletonSortMode = (SkeletonSortMethod)SkelSortModeComboBox.SelectedIndex;
        }
        #endregion

        #region Feedback Tab GUI Methods
        //Changes if the options for feedback are enabled or not
        private void UseFeedbackCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            server.serverMasterOptions.feedbackOptions.useFeedback = (bool)UseFeedbackCheckBox.IsChecked;
            FeedbackOptionsGroupBox.IsEnabled = (bool)UseFeedbackCheckBox.IsChecked;
        }
        //Changes the feedback sensor number in the options
        private void FeedbackSensorNumberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            int temp = -1;
            if (int.TryParse(FeedbackSensorNumberTextBox.Text, out temp))
            {
                server.serverMasterOptions.feedbackOptions.feedbackSensorNumber = temp;
            }
        }
        //Changes the name of the feedback server in the options
        private void FeedbackServerNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            server.serverMasterOptions.feedbackOptions.feedbackServerName = FeedbackServerNameTextBox.Text;
        }

        private void FeedbackJointTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            server.serverMasterOptions.feedbackOptions.sensorJointType = (JointType)FeedbackJointTypeComboBox.SelectedIndex;
        }
        #endregion
    }
}