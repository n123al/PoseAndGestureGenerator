/////////////////////////////////////////////////////////////////////////
//
// Nicholas Hadjiminas,Final Build , 20/09/2011
//
// Copyright 2011 __Nicholas Hadjiminas__. All rights reserved.
//
//
///////////////////////////////////////////////////////////////////////////

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using System.Windows;
using System.Threading;
using System.Windows.Threading;
#endregion

namespace PoseAndGestureGenerator
{
    internal class KinectManager
    {

#region Fields
        public KinectSensor KSensor;
        int totalFrames = 0;
        int lastFrames = 0;
        DateTime lastTime = DateTime.MaxValue;
        public int fps = 0;
        private bool valid = false;
        public enum ImageType
        {
            Color,
            Depth,
        }
        public ImageType myImageType { get; set; }
 #endregion

#region Constructor
        public KinectManager() 
        {
         try
            {
                KSensor = (from sensorToCheck in KinectSensor.KinectSensors
                           where sensorToCheck.Status == KinectStatus.Connected
                           select sensorToCheck).FirstOrDefault();


            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Failed to open stream. Please make sure to specify a supported image type and resolution.");
                System.Environment.Exit(0);
                return;
            }

         Start();
         valid = true;
        }
#endregion

#region Functions
        public bool IsValid()
        {
            return valid;
        }

        private void Start()
        {
            //nui = new KinectSensor();
           // nui.SkeletonStream.Enable();
           
            // Initialize Kinect NUI 
            if (KSensor == null) {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                System.Environment.Exit(0);
                return;
            }
            try
            {
               
                KSensor.SkeletonStream.Enable();
                KSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                KSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                KSensor.Start();
              //  nui.Initialize( RuntimeOptions.UseDepthAndPlayerIndex | RuntimeOptions.UseSkeletalTracking | RuntimeOptions.UseColor);
            }
            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                System.Environment.Exit(0);
                return;
            }


          
            KSensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(nui_DepthFrameReady);
        }
        /// <summary>
        /// Stop Knect Runtimes
        /// </summary>
        public void Stop()
        {
            if (KSensor != null)
            {
                KSensor.Stop(); 
                KSensor.Dispose();
            }
        }
        
        //public Point ScreenPosition(Joint joint, double ScreenWidth, double ScreenHeight)
        //{
        //    float depthX, depthY;
        //    nui.SkeletonEngine.SkeletonToDepthImage(joint.Position, out depthX, out depthY);
        //    depthX = Math.Max(0, Math.Min(depthX * 320, 320));  //convert to 320, 240 space
        //    depthY = Math.Max(0, Math.Min(depthY * 240, 240));  //convert to 320, 240 space
        //    int colorX, colorY;
        //    ImageViewArea iv = new ImageViewArea();
        //    nui.NuiCamera.GetColorPixelCoordinatesFromDepthPixel(ImageResolution.Resolution640x480, iv, (int)depthX, (int)depthY, (short)0, out colorX, out colorY);
        //    // map back to skeleton.Width & skeleton.Height
        //    return new Point((int)(ScreenWidth * colorX / 640.0), (int)(ScreenHeight * colorY / 480));
        //}

        public Point GetPosition2DLocation(DepthImageFrame depthFrame, SkeletonPoint skeletonPoint, double ScreenWidth, double ScreenHeight)
        {
            DepthImagePoint depthPoint = depthFrame.MapFromSkeletonPoint(skeletonPoint);

            switch (this.myImageType)
            {
                case ImageType.Color:
                    ColorImagePoint colorPoint = depthFrame.MapToColorImagePoint(depthPoint.X, depthPoint.Y, this.KSensor.ColorStream.Format);

                    // map back to skeleton.Width & skeleton.Height
                    return new Point(
                        (int)(ScreenWidth * colorPoint.X / this.KSensor.ColorStream.FrameWidth),
                        (int)(ScreenHeight * colorPoint.Y / this.KSensor.ColorStream.FrameHeight));
                case ImageType.Depth:
                    return new Point(
                        (int)(ScreenWidth * depthPoint.X / depthFrame.Width),
                        (int)(ScreenHeight * depthPoint.Y / depthFrame.Height));
                default:
                    throw new ArgumentOutOfRangeException("ImageType was a not expected value: " + myImageType.ToString());
            }
        }
#endregion

#region Event handling
        void nui_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            //PlanarImage Image = e.ImageFrame.Image;
            //byte[] convertedDepthFrame = convertDepthFrame(Image.Bits);

            //depth.Source = BitmapSource.Create(
            //    Image.Width, Image.Height, 96, 96, PixelFormats.Bgr32, null, convertedDepthFrame, Image.Width * 4);

            ++totalFrames;

            DateTime cur = DateTime.Now;
            if (cur.Subtract(lastTime) > TimeSpan.FromSeconds(1))
            {
                int frameDiff = totalFrames - lastFrames;
                lastFrames = totalFrames;
                lastTime = cur;
                fps = frameDiff;
            }
        }

#endregion
          
    }
}
