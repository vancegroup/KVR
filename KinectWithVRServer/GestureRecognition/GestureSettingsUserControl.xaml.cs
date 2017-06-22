using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using KinectBase;

namespace KinectWithVRServer
{
    /// <summary>
    /// Interaction logic for GestureSettingsUserControl.xaml
    /// </summary>
    public partial class GestureSettingsUserControl : UserControl
    {
        public string GestureName = "";
        private GestureCommand settings;
        private MainWindow parent;
        private bool recording = false;
        private bool testing = false;
        private List<KinectSkeleton> trainingData = new List<KinectSkeleton>();
        private List<string> trainingDataNames = new List<string>();
        private int index = -1;

        public GestureSettingsUserControl(int gestureIndex, MainWindow guiParent)
        {
            InitializeComponent();

            index = gestureIndex;
            parent = guiParent;
            settings = parent.server.serverMasterOptions.gestureCommands[index];
            Grid.SetColumn(this, 2);
            GestureName = settings.gestureName;
            trainingSetsListBox.ItemsSource = trainingDataNames;

            //Update the GUI
            nameTextBox.Text = GestureName;
            sensitivityTextBox.Text = settings.sensitivity.ToString();
            skeletonTextBox.Text = settings.trackedSkeleton.ToString();
            jointComboBox.SelectedIndex = JointToComboBoxIndex(settings.monitoredJoint);
            serverNameTextBox.Text = settings.serverName;
            buttonNumberTextBox.Text = settings.buttonNumber.ToString();
            buttonTypeComboBox.SelectedIndex = ButtonTypeToComboBoxIndex(settings.buttonType);
            buttonStateComboBox.SelectedIndex = ButtonStateToComboBoxIndex(settings.setState);
        }

        private void nameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            //Check for a unique gesture name
            bool found = false;

            for (int i = 0; i < parent.server.serverMasterOptions.gestureCommands.Count; i++)
            {
                if (string.Compare(parent.server.serverMasterOptions.gestureCommands[i].gestureName, nameTextBox.Text, true) == 0)
                {
                    found = true;
                    break;
                }
            }

            if (found) //Change the name back if it is already in use
            {
                nameTextBox.Text = GestureName;
            }
            else
            {
                GestureName = nameTextBox.Text;
                settings.gestureName = GestureName;
                parent.UpdateGesturePageListing();
                //parent.currentGesturesDataGrid.Items.Refresh();
            }
        }
        private void testButton_Click(object sender, RoutedEventArgs e)
        {
            if (!testing)
            {
                if (index >= 0)
                {
                    testing = true;
                    startButton.IsEnabled = false;
                    stopButton.IsEnabled = false;
                    testButton.Content = "Stop Test";
                    parent.MergedSkeletonChanged += parent_MergedSkeletonChanged;
                }

            }
            else
            {
                testing = false;
                parent.MergedSkeletonChanged -= parent_MergedSkeletonChanged;
                startButton.IsEnabled = true;
                stopButton.IsEnabled = false;
                testButton.Content = "Test";
            }

        }
        private void trainButton_Click(object sender, RoutedEventArgs e)
        {
            if (index >= 0)
            {
                parent.server.gestRecog.TrainGesture(settings.trainingData, index);
                ChangeTrainedState(true);
            }
        }
        private void parent_MergedSkeletonChanged(object sender, SkeletonEventArgs e)
        {
            if (e.skeletons.Length > 0)
            {
                if (recording)
                {
                    KinectSkeleton skelCopy = HelperMethods.DeepCopySkeleton(e.skeletons[0]);
                    trainingData.Add(skelCopy);
                }
                else if (testing)
                {
                    KinectSkeleton skelCopy = HelperMethods.DeepCopySkeleton(e.skeletons[0]);
                    double val = parent.server.gestRecog.TestRecognizer(skelCopy, index);
                    System.Diagnostics.Trace.WriteLine("Relative Probability: " + val.ToString());

                    if (val < 1.0)
                    {
                        this.Dispatcher.BeginInvoke((Action)(() =>
                        {
                            //probTextBlock.Text = val.ToString("F5");
                            probTextBlock.Text = "Found";
                            probTextBlock.Background = Brushes.OrangeRed;
                            ToggleBackDelegate deli = toggleBackFound;
                            deli.BeginInvoke(null, null);
                        }), null);
                    }
                }
            }
        }

        #region Record Training Data Methods
        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            recording = true;
            startButton.IsEnabled = false;
            trainButton.IsEnabled = false;
            testButton.IsEnabled = false;
            stopButton.IsEnabled = true;
            trainingData = new List<KinectSkeleton>();
            parent.MergedSkeletonChanged += parent_MergedSkeletonChanged;
        }
        private void stopButton_Click(object sender, RoutedEventArgs e)
        {
            recording = false;
            parent.MergedSkeletonChanged -= parent_MergedSkeletonChanged;
            if (isTrainingDataValid(trainingData))
            {
                string temp = getTrainingDataName();
                trainingDataNames.Add(temp);
                KinectSkeleton[] newList = new KinectSkeleton[trainingData.Count];
                trainingData.CopyTo(newList);
                settings.trainingData.Add(new List<KinectSkeleton>(newList));
            }
            else
            {
                HelperMethods.ShowErrorMessage("No Training Data", "Warning: No training data was recorded!", parent);
            }
            trainingData.Clear();
            trainingSetsListBox.Items.Refresh();
            stopButton.IsEnabled = false;
            testButton.IsEnabled = false;
            trainButton.IsEnabled = true;
            startButton.IsEnabled = true;

        }
        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            int index = trainingSetsListBox.SelectedIndex;
            if (index >= 0 && index < trainingDataNames.Count)
            {
                trainingDataNames.RemoveAt(index);
                settings.trainingData.RemoveAt(index);
            }
            trainingSetsListBox.Items.Refresh();
        }
        private string getTrainingDataName()
        {
            string tryName = trainingDataNameTextBox.Text;
            
            bool loop = true;
            int number = 0;
            while (loop)
            {
                bool found = false;
                string tempName = string.Copy(tryName);
                if (number != 0)
                {
                    tempName = tempName + " " + number.ToString();
                }

                for (int i = 0; i < trainingDataNames.Count; i++)
                {
                    if (string.Compare(tempName, trainingDataNames[i], true) == 0)
                    {
                        number++;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    loop = false;
                    tryName = tempName;
                }
            }

            return tryName;
        }
        private bool isTrainingDataValid(List<KinectSkeleton> data)
        {
            int validCount = 0;

            for (int i = 0; i < data.Count; i++)
            {
                if (data[i].skeleton[JointType.ShoulderLeft].TrackingState == TrackingState.Tracked &&
                    data[i].skeleton[JointType.ShoulderRight].TrackingState == TrackingState.Tracked &&
                    data[i].skeleton[settings.monitoredJoint].TrackingState == TrackingState.Tracked)
                {
                    validCount++;
                }
            }

            if (validCount > 2)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private void toggleBackFound()
        {
            System.Threading.Thread.Sleep(1500);
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                //probTextBlock.Text = val.ToString("F5");
                probTextBlock.Text = "Not Found";
                probTextBlock.Background = Brushes.Transparent;

            }), null);
        }
        private delegate void ToggleBackDelegate();
        #endregion

        #region Variable Setting GUI Stuff
        private void sensitivityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                double temp = 1.0;
                if (double.TryParse(sensitivityTextBox.Text, out temp))
                {
                    settings.sensitivity = temp;
                }
                else
                {
                    sensitivityTextBox.Text = settings.sensitivity.ToString();
                }
            }
        }
        private void skeletonTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                int temp = 0;
                if (int.TryParse(skeletonTextBox.Text, out temp))
                {
                    settings.trackedSkeleton = temp;
                }
                else
                {
                    skeletonTextBox.Text = settings.trackedSkeleton.ToString();
                }
            }
        }        
        private void ChangeTrainedState(bool isTrained)
        {
            settings.isTrained = isTrained;
            trainButton.IsEnabled = !isTrained;
            testButton.IsEnabled = isTrained;
        }
        private void jointComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                JointType tempJoint = ComboBoxIndexToJoint(jointComboBox.SelectedIndex);

                if (tempJoint != settings.monitoredJoint)
                {
                    settings.monitoredJoint = tempJoint;
                    ChangeTrainedState(false);
                }
            }
            
        }
        private void serverNameTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                settings.serverName = serverNameTextBox.Text;
            }
        }
        private void buttonNumberTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (this.IsInitialized)
            {
                int temp = 0;
                if (int.TryParse(buttonNumberTextBox.Text, out temp))
                {
                    settings.buttonNumber = temp;
                }
                else
                {
                    buttonNumberTextBox.Text = settings.buttonNumber.ToString();
                }
            }
        }
        private void buttonTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                settings.buttonType = ComboBoxIndexToButtonType(buttonTypeComboBox.SelectedIndex);
            }
        }
        private void buttonStateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.IsInitialized)
            {
                settings.setState = ComboBoxIndexToButtonState(buttonStateComboBox.SelectedIndex);
            }
        }
        #endregion

        #region Helper Methods
        private int JointToComboBoxIndex(JointType joint)
        {
            return (int)joint;
        }
        private JointType ComboBoxIndexToJoint(int index)
        {
            return (JointType)index;
        }
        private int ButtonTypeToComboBoxIndex(ButtonType type)
        {
            switch (type)
            {
                case ButtonType.Momentary:
                    {
                        return 0;
                    }
                case ButtonType.Setter:
                    {
                        return 1;
                    }
                case ButtonType.Toggle:
                    {
                        return 2;
                    }
                default:
                    {
                        return 0;
                    }
            }
        }
        private ButtonType ComboBoxIndexToButtonType(int index)
        {
            switch (index)
            {
                case 0:
                    {
                        return ButtonType.Momentary;
                    }
                case 1:
                    {
                        return ButtonType.Setter;
                    }
                case 2:
                    {
                        return ButtonType.Toggle;
                    }
                default:
                    {
                        return ButtonType.Momentary;
                    }
            }
        }
        private int ButtonStateToComboBoxIndex(bool isPressed)
        {
            if (isPressed)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        private bool ComboBoxIndexToButtonState(int index)
        {
            if (index == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        //Rejects any points that are not numbers or control characters or a period
        private void floatNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key) && e.Key != Key.OemPeriod && e.Key != Key.Decimal)
            {
                e.Handled = true;
            }
        }
        //Rejects any points that are not numbers or control charactes
        private void intNumberTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!KinectBase.HelperMethods.NumberKeys.Contains(e.Key))
            {
                e.Handled = true;
            }
        }
        #endregion

        private void trainingSetsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized)
            {
                if (trainingSetsListBox.SelectedIndex >= 0 && trainingSetsListBox.SelectedIndex < trainingSetsListBox.Items.Count)
                {
                    removeButton.IsEnabled = true;
                }
                else
                {
                    removeButton.IsEnabled = false;
                }
            }
        }
    }
}
