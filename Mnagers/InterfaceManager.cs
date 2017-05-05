/////////////////////////////////////////////////////////////////////////
//
// Nicholas Hadjiminas,Final Build , 20/09/2011
//
// Copyright 2011 __Nicholas Hadjiminas__. All rights reserved.
//
//
///////////////////////////////////////////////////////////////////////////
#region Using Statements
using BTC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
#endregion

namespace PoseAndGestureGenerator
{
    internal class InterfaceManager
    { 

#region Fields
        MainWindow MyWin;
#endregion
        
#region Initialization
        /// <summary>
        /// initialize InerfaceManager
        /// </summary>
        /// <param name="MyWin"></param>
        public void SetWindow(MainWindow MyWin)
        {
         this.MyWin=MyWin;
        }
#endregion 

#region Functions
        /// <summary>
        /// Update Gesture List combo box in order to show the latest additions/removals of gestures 
        /// </summary>
        public void UpdateGesturesLists()
        {
            MyWin.GesturesList.Items.Clear();
            MyWin.GesturesList.Items.Add("NEWGESTURE");
            MyWin.GesturesList.SelectedIndex = MyWin.GesturesList.Items.IndexOf("NEWGESTURE");
            List<String> gesturesList = MyWin.btc.GetGestureList();
            foreach (String Gesture in gesturesList)
            {

                MyWin.GesturesList.Items.Add(Gesture);

            }
        }
        /// <summary>
        /// Update Pose List combo box in order to show the latest additions/removals of poses 
        /// </summary>
        public void UpdatePoseLists()
        {
            MyWin.StartPose.Items.Clear();
            MyWin.EndPose.Items.Clear();
            MyWin.PosesList.Items.Clear();
            MyWin.PosesList.Items.Add("NEWPOSE");
            MyWin.PosesList.SelectedIndex = MyWin.PosesList.Items.IndexOf("NEWPOSE");
            List<String> posesList = MyWin.btc.GetPoseList();
            foreach (String Pose in posesList)
            {

                MyWin.PosesList.Items.Add(Pose);
                MyWin.StartPose.Items.Add(Pose);
                MyWin.EndPose.Items.Add(Pose);
            }
        }

        /// <summary>
        /// Shows Comparison results 
        /// </summary>
        /// <param name="PoseAnglesChanges"></param>
        //public void ComparisonResults(Dictionary<PoseAndGestureGenerator.MyHelper.AnglesID, int> PoseAnglesChanges) 
        //{
        //    for (int i = 0; i < (int)MyHelper.AnglesID.Count; i++)
        //    {
        //        CheckBox cbox = (CheckBox)MyWin.FindName("checkBox" + i);
        //        string CboxText = (string)cbox.Content;
        //        //display number of changes
        //        if (CboxText.IndexOf("(") != -1)  cbox.Content = CboxText.Remove(CboxText.IndexOf("("));
        //        // change font color to red
        //        cbox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
        //        if (PoseAnglesChanges[(MyHelper.AnglesID)i] > 0)
        //        {
        //            cbox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
        //            cbox.Content = ((string)cbox.Content) + " (" + PoseAnglesChanges[(MyHelper.AnglesID)i].ToString() + ")";
        //        }
        //    }
        //}
        /// <summary>
        /// Load Ui data to a Pose
        /// </summary>
        /// <param name="MyPose"></param>
        public void LoadDataToPose(BTCPose MyPose)
        {
            for (int i = 0; i < (int)BTC.RelationsID.RelationsCount; i++)
            {
                CheckBox cbox = (CheckBox)MyWin.FindName("checkBox" + i);
                TextBox tbox = (TextBox)MyWin.FindName("textBox" + i);
                MyPose.Accuracy[i] = double.Parse(tbox.Text);
                MyPose.Relations[i] =(bool)cbox.IsChecked;
            }

        }

        /// <summary>
        /// Display Pose information to UI
        /// </summary>
        /// <param name="MyPose"></param>
        public void LoadPoseToUI(BTCPose MyPose)
        {
            MyWin.poseid.Text = MyPose.ID;
            MyWin.posevalue.Text = MyPose.value;
            for (int i = 0; i < (int)BTC.RelationsID.RelationsCount; i++)
            {
                CheckBox cbox = (CheckBox)MyWin.FindName("checkBox" + i);
                TextBox tbox = (TextBox)MyWin.FindName("textBox" + i);
                tbox.Text = MyPose.Accuracy[i].ToString();
                cbox.IsChecked = MyPose.Relations[i];
            }
        }

        /// <summary>
        /// clear all information displayed in UI
        /// </summary>
        public void ResetUI()
        {
            MyWin.poseid.Text = "";
            MyWin.posevalue.Text = "";
            for (int i = 0; i < (int)BTC.RelationsID.RelationsCount; i++)
            {
                CheckBox cbox = (CheckBox)MyWin.FindName("checkBox" + i);
                TextBox tbox = (TextBox)MyWin.FindName("textBox" + i);
                cbox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Black);
                tbox.Text = "15";
                cbox.IsChecked = false;
                string CboxText = (string)cbox.Content;
                if (CboxText.IndexOf("(") != -1)
                    cbox.Content = CboxText.Remove(CboxText.IndexOf("("));
            }

        }
#endregion
    }
}
