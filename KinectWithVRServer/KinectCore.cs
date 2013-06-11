using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Shapes;

namespace KinectWithVRServer
{
    class KinectCore
    {
        internal KinectSensor kinect;
        MainWindow parent;
        WriteableBitmap depthImage;
        short[] depthImagePixels;
        byte[] colorImagePixels;
        WriteableBitmap colorImage;
        CoordinateMapper mapper;
        bool isGUI = false;
        ServerCore server;

        //The parent has to be optional to allow for console operation
        public KinectCore(ServerCore mainServer, MainWindow thisParent = null, int KinectNumber = 0)
        {
            if (KinectSensor.KinectSensors.Count > KinectNumber)
            {
                kinect = KinectSensor.KinectSensors[KinectNumber];
            }
            else
            {
                throw new Exception("Specified Kinect sensor does not exist");
            }

            server = mainServer;
            if (server == null)
            {
                throw new Exception("Server does not exist.");
            }

            parent = thisParent;
            if (parent != null)
            {
                isGUI = true;
            }

            //Setup default properties
            kinect.ColorStream.Enable();
            kinect.DepthStream.Enable();
            kinect.SkeletonStream.Enable();
            kinect.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
            kinect.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
            kinect.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

            if (isGUI)
            {
                //Setup the images for the display
                depthImage = new WriteableBitmap(kinect.DepthStream.FrameWidth, kinect.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Gray16, null);
                parent.DepthImage.Source = depthImage;
                colorImage = new WriteableBitmap(kinect.ColorStream.FrameWidth, kinect.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                parent.ColorImage.Source = colorImage;
            }

            //Create the coordinate mapper
            mapper = new CoordinateMapper(kinect);

            kinect.Start();
            //Note: Audio stream must be started AFTER the skeleton stream
        }

        public void ShutdownSensor()
        {
            if (kinect != null)
            {
                kinect.ColorFrameReady -= new EventHandler<ColorImageFrameReadyEventArgs>(kinect_ColorFrameReady);
                kinect.DepthFrameReady -= new EventHandler<DepthImageFrameReadyEventArgs>(kinect_DepthFrameReady);
                kinect.SkeletonFrameReady -= new EventHandler<SkeletonFrameReadyEventArgs>(kinect_SkeletonFrameReady);

                kinect.Stop();
            }
        }

        private void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skelFrame = e.OpenSkeletonFrame())
            {
                if (skelFrame != null)
                {
                    if (isGUI)
                    {
                        parent.ColorImageCanvas.Children.Clear();
                    }

                    Skeleton[] skeletons = new Skeleton[skelFrame.SkeletonArrayLength];
                    skelFrame.CopySkeletonDataTo(skeletons);
                    int index = 0;
                    foreach (Skeleton skel in skeletons)
                    {
                        //Pick a color for the bones and joints based off the player ID
                        Color renderColor = Colors.White;
                        if (index == 0)
                        {
                            renderColor = Colors.Red;
                        }
                        else if (index == 1)
                        {
                            renderColor = Colors.Blue;
                        }
                        else if (index == 2)
                        {
                            renderColor = Colors.Green;
                        }
                        else if (index == 3)
                        {
                            renderColor = Colors.Yellow;
                        }
                        else if (index == 4)
                        {
                            renderColor = Colors.Cyan;
                        }
                        else if (index == 5)
                        {
                            renderColor = Colors.Fuchsia;
                        }

                        //Send the points across if the skeleton is either tracked or has a position
                        if (skel.TrackingState != SkeletonTrackingState.NotTracked)
                        {
                            if (parent.settings.kinectOptions.trackSkeletons)
                            {
                                if (server.isRunning)
                                {
                                    SendSkeletonVRPN(skel, index);
                                }
                                if (isGUI)
                                {
                                    RenderSkeletonOnColor(skel, renderColor);
                                }
                            }
                        }

                        index++;
                    }
                }
            }
        }
        private void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    depthImagePixels = new short[frame.PixelDataLength];
                    frame.CopyPixelDataTo(depthImagePixels);
                    depthImage.WritePixels(new System.Windows.Int32Rect(0, 0, frame.Width, frame.Height), depthImagePixels, frame.Width * frame.BytesPerPixel, 0);
                }
            }
        }
        private void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame frame = e.OpenColorImageFrame())
            {
                if (frame != null)
                {
                    colorImagePixels = new byte[frame.PixelDataLength];
                    frame.CopyPixelDataTo(colorImagePixels);
                    colorImage.WritePixels(new System.Windows.Int32Rect(0, 0, frame.Width, frame.Height), colorImagePixels, frame.Width * frame.BytesPerPixel, 0);
                }
            }
        }

        private void SendSkeletonVRPN(Skeleton skeleton, int id)
        {
            foreach (Joint joint in skeleton.Joints)
            {
                //I could include inferred joints as well, should I?
                if (joint.TrackingState == JointTrackingState.Tracked)
                {
                    Vector4 boneQuat = skeleton.BoneOrientations[joint.JointType].AbsoluteRotation.Quaternion;
                    server.trackerServers[id].ReportPose(GetSkeletonSensorNumber(joint.JointType), DateTime.Now,
                                                         new Vrpn.Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z),
                                                         new Vrpn.Quaternion(boneQuat.W, boneQuat.X, boneQuat.Y, boneQuat.Z));
                }
            }
        }
        private void RenderSkeletonOnColor(Skeleton skeleton, Color renderColor)
        {
            //Calculate the offset
            Point offset = new Point(0.0, 0.0);
            if (parent.ColorImageCanvas.ActualWidth != parent.ColorImage.ActualWidth)
            {
                offset.X = (parent.ColorImageCanvas.ActualWidth - parent.ColorImage.ActualWidth) / 2;
            }

            if (parent.ColorImageCanvas.ActualHeight != parent.ColorImage.ActualHeight)
            {
                offset.Y = (parent.ColorImageCanvas.ActualHeight - parent.ColorImage.ActualHeight) / 2;
            }

            if (KinectSettings.skeletonMode == SkeletonTrackingMode.Seated)
            {
                DrawBoneOnColor(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight], renderColor, 2.0, offset);
            }
            else
            {
                //Render all the bones
                DrawBoneOnColor(skeleton.Joints[JointType.Head], skeleton.Joints[JointType.ShoulderCenter], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderLeft], skeleton.Joints[JointType.ElbowLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ElbowLeft], skeleton.Joints[JointType.WristLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.WristLeft], skeleton.Joints[JointType.HandLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.ShoulderRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderRight], skeleton.Joints[JointType.ElbowRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ElbowRight], skeleton.Joints[JointType.WristRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.WristRight], skeleton.Joints[JointType.HandRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.ShoulderCenter], skeleton.Joints[JointType.Spine], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.Spine], skeleton.Joints[JointType.HipCenter], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.HipLeft], skeleton.Joints[JointType.KneeLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.KneeLeft], skeleton.Joints[JointType.AnkleLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.AnkleLeft], skeleton.Joints[JointType.FootLeft], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.HipCenter], skeleton.Joints[JointType.HipRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.HipRight], skeleton.Joints[JointType.KneeRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.KneeRight], skeleton.Joints[JointType.AnkleRight], renderColor, 2.0, offset);
                DrawBoneOnColor(skeleton.Joints[JointType.AnkleRight], skeleton.Joints[JointType.FootRight], renderColor, 2.0, offset);
            }
            foreach (Joint joint in skeleton.Joints)
            {
                DrawJointPointOnColor(joint, renderColor, 2.0, offset);
            }
        }
        private int GetSkeletonSensorNumber(JointType joint)
        {
            int sensorNumber = -1;

            switch (joint)
            {
                case JointType.Head:
                {
                    sensorNumber = 0;
                    break;
                }
                case JointType.ShoulderCenter:
                {
                    sensorNumber = 1;
                    break;
                }
                case JointType.Spine:
                {
                    sensorNumber = 2;
                    break;
                }
                case JointType.HipCenter:
                {
                    sensorNumber = 3;
                    break;
                }
                //There is no 4, in order to match with FAAST
                case JointType.ShoulderLeft:
                {
                    sensorNumber = 5;
                    break;
                }
                case JointType.ElbowLeft:
                {
                    sensorNumber = 6;
                    break;
                }
                case JointType.WristLeft:
                {
                    sensorNumber = 7;
                    break;
                }
                case JointType.HandLeft:
                {
                    sensorNumber = 8;
                    break;
                }
                //There is no 9 or 10, in order to match with FAAST
                case JointType.ShoulderRight:
                {
                    sensorNumber = 11;
                    break;
                }
                case JointType.ElbowRight:
                {
                    sensorNumber = 12;
                    break;
                }
                case JointType.WristRight:
                {
                    sensorNumber = 13;
                    break;
                }
                case JointType.HandRight:
                {
                    sensorNumber = 14;
                    break;
                }
                //There is no 15, in order to match with FAAST
                case JointType.HipLeft:
                {
                    sensorNumber = 16;
                    break;
                }
                case JointType.KneeLeft:
                {
                    sensorNumber = 17;
                    break;
                }
                case JointType.AnkleLeft:
                {
                    sensorNumber = 18;
                    break;
                }
                case JointType.FootLeft:
                {
                    sensorNumber = 19;
                    break;
                }
                case JointType.HipRight:
                {
                    sensorNumber =20;
                    break;
                }
                case JointType.KneeRight:
                {
                    sensorNumber = 21;
                    break;
                }
                case JointType.AnkleRight:
                {
                    sensorNumber = 22;
                    break;
                }
                case JointType.FootRight:
                {
                    sensorNumber = 23;
                    break;
                }
            }

            return sensorNumber;
        }
        private void DrawBoneOnColor(Joint startJoint, Joint endJoint, Color boneColor, double thickness, Point offset)
        {
            if (startJoint.TrackingState == JointTrackingState.Tracked && endJoint.TrackingState == JointTrackingState.Tracked)
            {
                //Map the joint from the skeleton to the color image
                ColorImagePoint startPoint = mapper.MapSkeletonPointToColorPoint(startJoint.Position, kinect.ColorStream.Format);
                ColorImagePoint endPoint = mapper.MapSkeletonPointToColorPoint(endJoint.Position, kinect.ColorStream.Format);

                //Calculate the coordinates on the image (the offset of the image is added in the next section)
                Point imagePointStart = new Point(0.0, 0.0);
                imagePointStart.X = ((double)startPoint.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth;
                imagePointStart.Y = ((double)startPoint.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight;
                Point imagePointEnd = new Point(0.0, 0.0);
                imagePointEnd.X = ((double)endPoint.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth;
                imagePointEnd.Y = ((double)endPoint.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight;

                //Generate the line for the bone
                Line line = new Line();
                line.Stroke = new SolidColorBrush(boneColor);
                line.StrokeThickness = thickness;
                line.X1 = imagePointStart.X + offset.X;
                line.X2 = imagePointEnd.X + offset.X;
                line.Y1 = imagePointStart.Y + offset.Y;
                line.Y2 = imagePointEnd.Y + offset.Y;
                parent.ColorImageCanvas.Children.Add(line);
            }
        }
        private void DrawJointPointOnColor(Joint joint, Color jointColor, double radius, Point offset)
        {
            if (joint.TrackingState == JointTrackingState.Tracked)
            {
                //Map the joint from the skeleton to the color image
                ColorImagePoint point = mapper.MapSkeletonPointToColorPoint(joint.Position, kinect.ColorStream.Format);

                //Calculat the coordinates on the image (the offset is also added in this section)
                Point imagePoint = new Point(0.0, 0.0);
                imagePoint.X = ((double)point.X / (double)kinect.ColorStream.FrameWidth) * parent.ColorImage.ActualWidth + offset.X;
                imagePoint.Y = ((double)point.Y / (double)kinect.ColorStream.FrameHeight) * parent.ColorImage.ActualHeight + offset.Y;

                //Generate the circle for the joint
                Ellipse circle = new Ellipse();
                circle.Fill = new SolidColorBrush(jointColor);
                circle.StrokeThickness = 0.0;
                circle.Margin = new Thickness(imagePoint.X - radius, imagePoint.Y - radius, 0, 0);
                circle.HorizontalAlignment = HorizontalAlignment.Left;
                circle.VerticalAlignment = VerticalAlignment.Top;
                circle.Height = radius * 2;
                circle.Width = radius * 2;
                parent.ColorImageCanvas.Children.Add(circle);
            }
        }

        
    }
}
