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
using System.Windows.Shapes;

namespace KinectWithVRServer.NetworkKinectWrapper
{
    //This class is a dialog window which prompts for a unique name for the network Kinect when it is added
    //This isn't a wrapper like the rest of the stuff in this sub-namespace, as wrapping in this case would be superfluous
    public partial class NKAddDialog : Window
    {
        MainWindow parent;
        private string id = null;
        internal string UniqueID
        {
            get { return id; }
        }

        public NKAddDialog(MainWindow thisParent)
        {
            InitializeComponent();

            parent = thisParent;

            //This causes the cursor to be in the textbox automatically when the dialog is opened so the user can start typing without having to select the textbox first
            UniqueNameTextBox.Focus();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            bool uniqueName = false;
            string name = UniqueNameTextBox.Text;

            if (name.Length > 0)
            {
                bool found = false;
                for (int i = 0; i < parent.availableKinects.Count; i++)
                {
                    if (name == parent.availableKinects[i].UniqueID)
                    {
                        found = true;
                        break;
                    }
                }
                uniqueName = !found;
            }

            if (uniqueName)
            {
                id = name;
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Error: The provided name for the network Kinect sensor is not unique.  Please enter a different name.", "Error: Invalid Name.", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
