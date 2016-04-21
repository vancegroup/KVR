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

namespace KinectV2Core
{
    /// <summary>
    /// Interaction logic for KinectV2SkeletonControl.xaml
    /// </summary>
    public partial class KinectV2SkeletonControl : UserControl
    {
        private KinectV2SettingsControl parent;

        public KinectV2SkeletonControl(KinectV2SettingsControl thisParent)
        {
            parent = thisParent;

            InitializeComponent();

            SkelSortModeComboBox.SelectedIndex = 0;
            SkeletonSettingsDataGrid.ItemsSource = parent.kinectSettings.rawSkeletonSettings.individualSkeletons;
            SkeletonSettingsDataGrid.Items.Refresh();
        }

        //Controls which skeleton sorting mode is used
        private void SkelSortModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            parent.kinectSettings.rawSkeletonSettings.skeletonSortMode = (SkeletonSortMethod)SkelSortModeComboBox.SelectedIndex;
        }
    }
}
