/////////////////////////////////////////////////////////////////////////
//
// Nicholas Hadjiminas,Final Build , 20/09/2011
//
// Copyright 2011 __Nicholas Hadjiminas__. All rights reserved.
//
///////////////////////////////////////////////////////////////////////////
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
using System.Windows.Threading;

namespace PoseAndGestureGenerator
{
    /// <summary>
    /// Interaction logic for MyMessageBox.xaml Auto closing messageBox
    /// </summary>
    public partial class MyMessageBox : Page
    {
        DispatcherTimer timer = new DispatcherTimer();
        Window myparent;
        int LifeTime;
        Window MyWin;
        public MyMessageBox()
        {
            InitializeComponent();
            myparent = new Window();
            myparent.Width = 304;
            myparent.Height = 200;
            myparent.Content = this;
            myparent.ResizeMode = ResizeMode.NoResize;
            myparent.Topmost = true;
            myparent.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            timer.Interval= new TimeSpan(0, 0, 1);
            timer.Tick +=new EventHandler(timer_Tick);
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (LifeTime > 0)
            {
                LifeTime--;
                this.label2.Content = LifeTime.ToString(); 
                this.UpdateLayout();

            }
            else
            {
                MyWin.IsEnabled = true;
                timer.Stop();
                myparent.Close();
            }
        }



        public void Show(int time,string msg, Window sender) 
        {
            MyWin = sender;
            MyWin.IsEnabled = false;
            LifeTime = time;
            this.label1.Content = msg;
            this.label2.Content = LifeTime.ToString(); 
            timer.Start();
            myparent.Show();
            myparent.UpdateLayout();

        }
    }
}
