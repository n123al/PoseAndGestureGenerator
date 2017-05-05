/////////////////////////////////////////////////////////////////////////
// Nicholas Hadjiminas,Final Build , 20/09/2011
//
// Pose and Gesture Generator is a windows application, which allows a software
// developer to create a list of poses and gestures that he want to recognize 
// in an application, from an easy to use user interface. This application is a 
// part of a software developer’s tool kit the “Be The Controller” tool kit, 
// which aims to help programmers easily recognize user’s pose and gestures, 
// using the Microsoft kinect controller and perform the appropriate actions
//
// Copyright 2011 __Nicholas Hadjiminas__. All rights reserved.
//
// The Microsoft Kinect for Windows SDK (Beta)  
// is licensed under the terms of KinectSDK from Microsoft Research 
// License Agreement: http://research.microsoft.com/KinectSDK-ToU
///////////////////////////////////////////////////////////////////////////

#region Using Statements
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
using Microsoft.Kinect;
using System.Text.RegularExpressions;
using BTC;
using Microsoft.Speech.Recognition;
#endregion

namespace PoseAndGestureGenerator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
public partial class MainWindow : Window
    {
#region Constructor 
        public MainWindow()
        {
            InitializeComponent();

        }
#endregion

#region Fields
   
        DateTime lastTime = DateTime.MaxValue;
        public BTCWraper btc;
        Vector3f[] CurentSkeleton;
        int CurentSkeletonId;
        Vector3f[] LastSkeleton;
        
        int LastSkeletonId;
        SpeechRecognizer speechRecognizer;
        KinectManager KManager;
        InterfaceManager interfaceManager = new InterfaceManager();
        bool pause = false;
        const int NormalMode = 0;
        const int FindChangesMode = 1;
        int Mode = NormalMode;
        const int RED_IDX = 2;
        const int GREEN_IDX = 1;
        const int BLUE_IDX = 0;
        //skeleton colors joins
        Dictionary<JointType,Brush> jointColors = new Dictionary<JointType,Brush>() { 
            {JointType.HipCenter, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.Spine, new SolidColorBrush(Color.FromRgb(169, 176, 155))},
            {JointType.ShoulderCenter, new SolidColorBrush(Color.FromRgb(168, 230, 29))},
            {JointType.Head, new SolidColorBrush(Color.FromRgb(200, 0,   0))},
            {JointType.ShoulderLeft, new SolidColorBrush(Color.FromRgb(79,  84,  33))},
            {JointType.ElbowLeft, new SolidColorBrush(Color.FromRgb(84,  33,  42))},
            {JointType.WristLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HandLeft, new SolidColorBrush(Color.FromRgb(215,  86, 0))},
            {JointType.ShoulderRight, new SolidColorBrush(Color.FromRgb(33,  79,  84))},
            {JointType.ElbowRight, new SolidColorBrush(Color.FromRgb(33,  33,  84))},
            {JointType.WristRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointType.HandRight, new SolidColorBrush(Color.FromRgb(37,   69, 243))},
            {JointType.HipLeft, new SolidColorBrush(Color.FromRgb(77,  109, 243))},
            {JointType.KneeLeft, new SolidColorBrush(Color.FromRgb(69,  33,  84))},
            {JointType.AnkleLeft, new SolidColorBrush(Color.FromRgb(229, 170, 122))},
            {JointType.FootLeft, new SolidColorBrush(Color.FromRgb(255, 126, 0))},
            {JointType.HipRight, new SolidColorBrush(Color.FromRgb(181, 165, 213))},
            {JointType.KneeRight, new SolidColorBrush(Color.FromRgb(71, 222,  76))},
            {JointType.AnkleRight, new SolidColorBrush(Color.FromRgb(245, 228, 156))},
            {JointType.FootRight, new SolidColorBrush(Color.FromRgb(77,  109, 243))}
        };
#endregion

#region Initialization
        private void Window_Loaded(object sender, EventArgs e)
        {
            lastTime = DateTime.Now;
            KManager = new KinectManager();
            btc = new BTCWraper();
            speechRecognizer = new SpeechRecognizer(sre_SpeechRecognized,KManager.KSensor.AudioSource) ;
            interfaceManager.SetWindow(this);
            KManager.KSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(nui_DepthFrameReady);
            KManager.KSensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(KinectAllFramesReady);
            KManager.KSensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(nui_ColorFrameReady);
      //      KinectSensor.StatusChanged += new EventHandler<StatusChangedEventArgs>(Kinects_StatusChanged);
            //nuim.nui.NuiCamera.ElevationAngle = 5;
        }
#endregion

#region Functions
        Polyline getSkeletonParts(JointCollection joints, DepthImageFrame depthFrame, Brush brush, params JointType[] jointsIds)
        {
            PointCollection points = new PointCollection(jointsIds.Length);
            for (int i = 0; i < jointsIds.Length; ++i )
            {
                points.Add(KManager.GetPosition2DLocation(depthFrame, joints[jointsIds[i]].Position, skeletonStream.Width, skeletonStream.Height));
            }
            //Draw a line to connect the joins 
            Polyline Line = new Polyline();
            Line.Points = points;
            Line.Stroke = brush;
            Line.StrokeThickness = 5;
            return Line;
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            speechRecognizer.Stop();
            KManager.Stop();
            Environment.Exit(0);     
        }

#endregion

#region Event handling
        private void Kinects_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Connected:
                    if (KManager.KSensor == null)
                    {
                        KManager.KSensor = e.Sensor; //if Runtime.Init() fails due to an AppDeviceConflict, this property will be null after return.
                    }
                    break;
                case KinectStatus.Disconnected:
                    if (KManager.KSensor == e.Sensor)
                    {
                        KManager.KSensor = null;
                    }
                    break;
                case KinectStatus.NotReady:
                    if (KManager.KSensor == null)
                    {
                     
                    }
                    break;
                case KinectStatus.NotPowered:
                    if (KManager.KSensor == e.Sensor)
                    {
                        KManager.KSensor = null;
                    }
                    break;
                default:
                    throw new Exception("Unhandled Status: " + e.Status);
            }

        }
        void nui_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {

        }
        void KinectAllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
           if (!this.IsEnabled) return;
  
           if (pause) return;

           if ((KManager.KSensor== null) || !((KinectSensor)sender).SkeletonStream.IsEnabled) return;

            SkeletonFrame skeletonFrame = e.OpenSkeletonFrame(); 
            if (skeletonFrame == null) return;
            int counterSkeleton = 0;
            //Create brushs for 6 skeleton (max skelton number NUI supports)
            Brush[] brushes = new Brush[6];
            //Set 6 skeleton colors
            brushes[0] = new SolidColorBrush(Color.FromRgb(255, 0, 0));
            brushes[1] = new SolidColorBrush(Color.FromRgb(0, 255, 0));
            brushes[2] = new SolidColorBrush(Color.FromRgb(64, 255, 255));
            brushes[3] = new SolidColorBrush(Color.FromRgb(255, 255, 64));
            brushes[4] = new SolidColorBrush(Color.FromRgb(255, 64, 255));
            brushes[5] = new SolidColorBrush(Color.FromRgb(128, 128, 255));
            skeletonStream.Children.Clear();
            //Processe all the Skeltons 
           
            Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength]; 
            skeletonFrame.CopySkeletonDataTo(skeletons);
            DepthImageFrame depthImageFrame = e.OpenDepthImageFrame();

            if ((skeletons == null) || (depthImageFrame==null)) return;

            foreach (Skeleton data in skeletons)
            {
                if(data.TrackingState == SkeletonTrackingState.Tracked)
                {
                    // Draw skelton on the skelton stream fream
                    Brush brush = brushes[counterSkeleton % brushes.Length];
                    skeletonStream.Children.Add(getSkeletonParts(data.Joints, depthImageFrame, brush, JointType.HipCenter, JointType.Spine, JointType.ShoulderCenter, JointType.Head));
                    skeletonStream.Children.Add(getSkeletonParts(data.Joints, depthImageFrame, brush, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft));
                    skeletonStream.Children.Add(getSkeletonParts(data.Joints, depthImageFrame, brush, JointType.ShoulderCenter, JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight));
                    skeletonStream.Children.Add(getSkeletonParts(data.Joints, depthImageFrame, brush, JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft));
                    skeletonStream.Children.Add(getSkeletonParts(data.Joints, depthImageFrame, brush, JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight));

                    // Draw joints points
                    foreach (Joint joint in data.Joints)
                    {
                        Point jointPos = KManager.GetPosition2DLocation(depthImageFrame, joint.Position, skeletonStream.Width, skeletonStream.Height);
                        Line jointLine = new Line();
                        jointLine.X1 = jointPos.X - 3;
                        jointLine.X2 = jointLine.X1 + 6;
                        jointLine.Y1 = jointLine.Y2 = jointPos.Y;
                        jointLine.Stroke = jointColors[joint.JointType];
                        jointLine.StrokeThickness = 6;
                        skeletonStream.Children.Add(jointLine);
                    }  
                // Load skeleton joins to pose
                    CurentSkeleton = new Vector3f[(int)BTC.JointID.JointCount];
                    CurentSkeletonId = data.TrackingId;
                    for (int i = (int)BTC.JointID.HipCenter; i < (int)BTC.JointID.JointCount; i++)
                    {
                        CurentSkeleton[i] = new Vector3f();
                        CurentSkeleton[i].x = data.Joints[(JointType)i].Position.X;
                        CurentSkeleton[i].y = data.Joints[(JointType)i].Position.Y;
                        CurentSkeleton[i].z = data.Joints[(JointType)i].Position.Z;
                    }
                    BTCResults result = btc.NewSkeleton(CurentSkeleton, CurentSkeletonId);
                  if (result.EndEvents.Count > 0 && result.NewEvents.Count > 0) frameRate.Text = "Not Recognize";
                  else {
                      foreach (BTC.KinectEvent kevent in result.NewEvents) {
                        if(kevent.Type == KEvents.Pose)
                          frameRate.Text = frameRate.Text + "-" + kevent.Value;
                      }
                  }
             }
                 
              counterSkeleton++;
            }
              
        }
        void nui_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            if (pause) return;
            if (!this.IsEnabled) return;
            // 32-bit per pixel, RGBA image 
            ColorImageFrame imageFrame = e.OpenColorImageFrame();
            if (imageFrame != null)
            {
               
                byte[] Image = new byte[imageFrame.PixelDataLength];
                imageFrame.CopyPixelDataTo(Image);
                //draw the video frame (video stream)
                video.Source = BitmapSource.Create(
                    imageFrame.Width, imageFrame.Height, 96, 96, PixelFormats.Bgr32, null, Image, imageFrame.Width * imageFrame.BytesPerPixel);
            }
        }
        void sre_SpeechRecognized(object sender,SpeechRecognizedEventArgs e)
        {
            Console.Write("\rSpeech Recognized: \t{0}", e.Result.Text);
            if (!this.IsEnabled) return;

            switch (Mode)
            {
                case NormalMode:
                    // Handel Capture Now voice command  
                   
                    if ((e.Result.Text == "Capture Now") && !pause && (e.Result.Confidence > .3))
                    {
                       // TempPose.CopyFrom(mypose);
                        LastSkeleton = CurentSkeleton;
                        LastSkeletonId = CurentSkeletonId;
                        pause = true;
                        MessageBox.Show("Pose Captured");
                    }

                    break;
                case FindChangesMode:
                    //Handel Capture Now voice command  
                    if ((e.Result.Text == "Capture Now") && !pause && (e.Result.Confidence > .3))
                    {
                        //Compare poses and store their differences 
                       // poseSuggester.CompareNewPose(mypose);
                        MyMessageBox msg = new MyMessageBox();
                        msg.Show(5, "Pose Captured. Next Pose In :", this);
                    }
                    //Handel Stop voice command  
                    if ((e.Result.Text == "Stop") && !pause && (e.Result.Confidence > .4))
                    {
                        //Presents the outcome of the comparison   
                       // interfaceManager.ComparisonResults(poseSuggester.PoseAnglesChanges);
                        MessageBox.Show("Sequence Completed");
                        Mode = NormalMode;
                    }
                    break;
            }
            //Handel Play voice command  
            if ((e.Result.Text.Contains("play") && pause && (e.Result.Confidence > .4)))
            {
                pause = false;
            }



        }
#endregion 

#region UI Event
        //Save Pose
        private void Save_Pose_Button(object sender, RoutedEventArgs e)
        {           
            
            if((string.IsNullOrWhiteSpace(poseid.Text))||(string.IsNullOrWhiteSpace(posevalue.Text)))
            {
                  MessageBox.Show("ID and Return Value can not be Empty");
                return;
            }
            System.Type CboxType = typeof(CheckBox);
            System.Type TBoxType = typeof(TextBox);
            BTCPose temp = btc.GetPose(poseid.Text);
            if ( temp == null)
            {
                 // Save Pose Value and ID
                temp = new BTCPose();
                temp.ID = poseid.Text;
                temp.value = posevalue.Text;
                //load UI Data To pose
                interfaceManager.LoadDataToPose(temp);
                btc.AddPose(LastSkeleton,temp.Relations,temp.Accuracy,temp.ID,temp.value,true);
                MessageBox.Show("Pose Saved");
            }
            else
            {
                if (MessageBox.Show("Update Pose ?", "Pose Exist", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    temp.value = posevalue.Text;
                    interfaceManager.LoadDataToPose(temp);
                    if (this.pause) btc.AddPose(LastSkeleton, temp.Relations, temp.Accuracy, temp.ID, temp.value, true);
                    else btc.UpdatePose(temp.Relations, temp.Accuracy, temp.ID, temp.value);
                }
            }
            interfaceManager.UpdatePoseLists();
            
        }
        //Pose selection changed
        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            System.Type CboxType = typeof(CheckBox);
            System.Type TBoxType = typeof(TextBox);
            if (PosesList.SelectedItem == null) return;
            if (((ComboBox)sender).SelectedItem.ToString() != "NEWPOSE")
            {
                //Display Pose information 
                BTCPose temp = btc.GetPose(PosesList.SelectedItem.ToString());
                interfaceManager.LoadPoseToUI(temp);
            }
            else interfaceManager.ResetUI();
        }
        //// Save Gesture
        private void Save_Gesture_Button(object sender, RoutedEventArgs e)
        {
            if ((string.IsNullOrWhiteSpace(GestureValue.Text)) || (string.IsNullOrWhiteSpace(GestureID.Text)))
            {
                MessageBox.Show("ID and Return Value can not be Empty");
                return;
            }
            if (string.IsNullOrEmpty(StartPose.Text) || string.IsNullOrEmpty(EndPose.Text))
            {
                MessageBox.Show("Select Start and End Pose");
                return;
            }
            //Check if gesture exists 
            BTCGesture temp = btc.GetGesture(GestureID.Text);
            // If is a new gesture
            if (temp == null)
            {
                //create gesture
                temp = new BTCGesture();
                temp.StartPose = StartPose.Text;
                temp.EndPose = EndPose.Text;
                temp.MaxDuration = float.Parse(GestureTime.Text);
                temp.ID = GestureID.Text;
                temp.value = GestureValue.Text;
                temp.EndPoseHold = double.Parse(EndHoldTime.Text);
                btc.AddGesture(temp,true);
                interfaceManager.UpdateGesturesLists();
                MessageBox.Show("Gesture Saved");
            }
            else
            {
                // if gesture exists 
                if (MessageBox.Show("Update Gesture ?", "Gesture Exist", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    //Update gesture
                    temp.StartPose = StartPose.Text;
                    temp.EndPose = EndPose.Text;
                    temp.MaxDuration = float.Parse(GestureTime.Text);
                    temp.ID = GestureID.Text;
                    temp.value = GestureValue.Text;
                    temp.EndPoseHold = double.Parse(EndHoldTime.Text);
                }
            }
        }
        //Save Poses List
        private void Save_PosesList_Button(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Pose List File (*.pbtc)|*.pbtc";


            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                btc.SavePosesList(sfd.FileName.ToString(), FileKey.ToString());
            
            } 

        }
        // Load Poses List
        private void Load_PosesList_Button(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;

            ofd.Filter = "Pose List File (*.pbtc)|*.pbtc";


            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                btc.LoadPosesList(ofd.FileName.ToString(), true, FileKey.ToString());
            }
            interfaceManager.UpdatePoseLists();
           
        }
        //Set Find Changes Mode
        private void Find_Changes_Button(object sender, RoutedEventArgs e)
        {
           //if (PosesList.SelectedItem == null)
           // {
           //     MessageBox.Show("Select Base Pose");
           //     return;
           // }
           // if (PosesList.SelectedItem.ToString() != "NEWPOSE")
           // {
           //     //Set Base Pose
           //     Pose temp = posesList.GetPoseById(PosesList.SelectedItem.ToString());
           //     poseSuggester.Reset();
           //     poseSuggester.BasePose = temp;
           //     Mode = FindChangesMode;
           // }
        }
        //Delete Pose
        private void Delete_Pose_Button(object sender, RoutedEventArgs e)
        {
            if (btc.DeletePose(PosesList.SelectedValue.ToString())) MessageBox.Show("Pose Deleted");
            interfaceManager.UpdatePoseLists();
        }
        //Load Gesture List
        private void Load_GesturesList_Button(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.Multiselect = false;

            ofd.Filter = "Gestures List File (*.gbtc)|*.gbtc";


            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                //Load gesture file to the gesturesList
                btc.LoadGesturesList(ofd.FileName.ToString(), true, "sdfs");

            }
            interfaceManager.UpdateGesturesLists();

        }
        // Save Gesture List
        private void Save_GesturesList_Button(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "Gestures List File (*.gbtc)|*.gbtc";


            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                btc.SaveGesturesList(sfd.FileName.ToString(), "sdfs");
            } 

        }
        //Load Selected Gesture
        private void GesturesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((ComboBox)sender).SelectedItem == null) return;
            if (((ComboBox)sender).SelectedItem.ToString() != "NEWGESTURE")
            {
                BTCGesture temp = btc.GetGesture(((ComboBox)sender).SelectedItem.ToString());
                if ((StartPose.Items.IndexOf(temp.StartPose) == -1) || (EndPose.Items.IndexOf(temp.EndPose) == -1))
                {
                    MessageBox.Show("Please Load The Correct List For Pose :" + temp.StartPose + " & " + temp.EndPose);
                    GesturesList.SelectedIndex = GesturesList.Items.IndexOf("NEWGESTURE");
                    return;
                }
                GestureID.Text = temp.ID;
                GestureValue.Text = temp.value;
                GestureTime.Text = temp.MaxDuration.ToString();
                EndHoldTime.Text = temp.EndPoseHold.ToString();
                StartPose.SelectedIndex = StartPose.Items.IndexOf(temp.StartPose);
                EndPose.SelectedIndex = EndPose.Items.IndexOf(temp.EndPose);
            }
            else
            {
                GestureID.Text = "";
                GestureValue.Text = "";
                GestureTime.Text = "0.0";
                EndHoldTime.Text = "0.0";
                StartPose.SelectedItem = null;
                EndPose.SelectedItem = null;
            }
        }
        //Delete Gesture
        private void Delete_Gesture_Button(object sender, RoutedEventArgs e)
        {
            if (btc.DeleteGesture(GesturesList.SelectedValue.ToString())) MessageBox.Show("Gesture Deleted");
            interfaceManager.UpdateGesturesLists();
        }

        private void tabControl1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (GestureTab.IsSelected) Mode = 3;
            else if (PoseTab.IsSelected) Mode = 0;
        }
        //Validate Variance
        private void ValidateVariance(object sender, KeyboardFocusChangedEventArgs e)
        {
            Regex objOneDotPattern = new Regex("^\\d{0,3}.\\.?\\d{0,2}$");
            string inputvalue = ((TextBox)sender).Text;
            if (!objOneDotPattern.IsMatch(inputvalue))
            {
                MessageBox.Show("Only Values Between 0 and 100 are allowed ");
                e.Handled = true;

            }
            else
            {
                if (double.Parse(inputvalue) > 100)
                {
                    MessageBox.Show("Only Values Between 0 and 100 are allowed ");
                    e.Handled = true;

                }
            }
        }
        //Validate Time
        private void ValidateTime(object sender, KeyboardFocusChangedEventArgs e)
        {
            Regex objOneDotPattern = new Regex("^\\d{0,3}.\\.?\\d{0,3}$");
            string inputvalue = ((TextBox)sender).Text;
            if (!objOneDotPattern.IsMatch(inputvalue))
            {
                MessageBox.Show("Only Double Values are allowed");
                e.Handled = true;

            }
     
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBoxResult.Cancel == MessageBox.Show("Are you sure you want to Exit?", "Exit", MessageBoxButton.OKCancel))
                e.Cancel = true;
        }

#endregion
    }
}
