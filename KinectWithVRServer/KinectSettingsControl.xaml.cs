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

namespace KinectWithVRServer
{
    /// <summary>
    /// Interaction logic for KinectSettingsControl.xaml
    /// </summary>
    public partial class KinectSettingsControl : UserControl
    {
        internal int KinectNumber = -1;
        MainWindow parent = null;
        bool isVerbose = false;

        public KinectSettingsControl(int kinectNumber, bool verboseOutput, MainWindow thisParent) //Parent is not optional since this GUI has to go somewhere
        {
            if (kinectNumber >= 0 && thisParent != null)
            {
                InitializeComponent();

                KinectNumber = kinectNumber;
                parent = thisParent;
            }
            else
            {
                throw new NotSupportedException("Method arguements are invalid!");
            }
        }
    }
}
