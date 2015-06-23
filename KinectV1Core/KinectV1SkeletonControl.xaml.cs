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

namespace KinectV1Core
{
    /// <summary>
    /// Interaction logic for KinectV1SkeletonControl.xaml
    /// </summary>
    public partial class KinectV1SkeletonControl : UserControl
    {
        private KinectV1SettingsControl parent;

        public KinectV1SkeletonControl(KinectV1SettingsControl thisParent)
        {
            parent = thisParent;

            InitializeComponent();

            SkelSortModeComboBox.SelectedIndex = 0;
            SkeletonSettingsDataGrid.ItemsSource = ((KinectV1Settings)parent.masterSettings.kinectOptionsList[parent.kinectID.Value]).rawSkeletonSettings.individualSkeletons;
            SkeletonSettingsDataGrid.Items.Refresh();
        }

        //Changes if the skeleton tracking is in seated mode
        private void ChooseSeatedCheckBox_CheckChanged(object sender, RoutedEventArgs e)
        {
            ((KinectV1Settings)parent.masterSettings.kinectOptionsList[parent.kinectID.Value]).rawSkeletonSettings.isSeatedMode = (bool)ChooseSeatedCheckBox.IsChecked;
        }
        //Controls which skeleton sorting mode is used
        private void SkelSortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((KinectV1Settings)parent.masterSettings.kinectOptionsList[parent.kinectID.Value]).rawSkeletonSettings.skeletonSortMode = (SkeletonSortMethod)SkelSortModeComboBox.SelectedIndex;
        }
    }
}
