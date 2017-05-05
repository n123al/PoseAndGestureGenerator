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
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Speech.AudioFormat;
using Microsoft.Speech.Recognition;
#endregion

namespace PoseAndGestureGenerator
{
    internal class SpeechRecognizer : IDisposable
    {
        #region Fields
        private KinectAudioSource kinectSource;
        private SpeechRecognitionEngine SREngine;
        private const string RecognizerId = "SR_MS_en-US_Kinect_10.0";
        private bool valid = false;
        // private Thread thread;
        private bool isDisposed;
        #endregion

        #region Constructor
        public SpeechRecognizer(EventHandler<SpeechRecognizedEventArgs> sre_SpeechRecognized, KinectAudioSource source)
        {
            RecognizerInfo recognizer = GetKinectRecognizer();
            if (recognizer == null)
                return;
            //Create voice recognition words/phrases list 
            var words = new Choices();
            words.Add("Stop");
            words.Add("play");
            words.Add("Capture Now");
            words.Add("Capture");
            words.Add("Now");


            var gb = new GrammarBuilder();
            //Specify the culture to match the recognizer .                                 
            gb.Culture = recognizer.Culture;
            gb.AppendWildcard();
            gb.Append(words);

            // Create the Grammar instance, and the voice recognizer.
            var g = new Grammar(gb);
            SREngine = new SpeechRecognitionEngine(recognizer);
            SREngine.LoadGrammar(g);
            SREngine.SpeechRecognized += sre_SpeechRecognized;
            SREngine.SpeechHypothesized += sre_SpeechHypothesized;
            SREngine.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(sre_SpeechRecognitionRejected);
            Start(source);
            //start voice recognizer in a new thread
            //thread = new Thread(Start(source));
            //thread.SetApartmentState(ApartmentState.MTA);
            // thread.Start();
            valid = true;
        }
        #endregion

        #region Functions
        public bool IsValid()
        {
            return valid;
        }

        private static RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }

        /// <summary>
        /// Initialize voice recognizer
        /// </summary>
        private void Start(KinectAudioSource source)
        {
            try
            {
                this.CheckDisposed();

                kinectSource = source;

                kinectSource.EchoCancellationMode = EchoCancellationMode.CancellationOnly;
                kinectSource.AutomaticGainControlEnabled = false;
                kinectSource.BeamAngleMode = BeamAngleMode.Adaptive;
                var kinectStream = kinectSource.Start();
                SREngine.SetInputToAudioStream(kinectStream, new SpeechAudioFormatInfo(
                                                      EncodingFormat.Pcm, 16000, 16, 1,
                                                      32000, 2, null));
                SREngine.RecognizeAsync(RecognizeMode.Multiple);
            }

            catch (InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Runtime initialization failed. Please make sure Kinect device is plugged in.");
                System.Environment.Exit(0);
                return;
            }
        }

        
        protected  void SRStop()
        {
                try
                {
                    SREngine.RecognizeAsyncCancel();
                    SREngine.RecognizeAsyncStop();
                    kinectSource.Stop();
                }
                catch { }
        }

        /// <summary>
        /// Uninitialize voice recognizer
        /// </summary>
        public void Stop()
        {
            if (SREngine != null)
            {
                try
                {
                    //SREngine.RecognizeAsyncCancel();
                    //SREngine.RecognizeAsyncStop();
                    //kinectSource.Stop();
                    //SREngine.Dispose();
                    this.Dispose();
                }
                catch { }
            }
        }
        #endregion

        #region Event handling
        void sre_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {

            Console.WriteLine("\nSpeech Rejected");
        }

        void sre_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Console.Write("\rSpeech Hypothesized: \t{0}", e.Result.Text);

        }
        #endregion

        private void CheckDisposed()
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException("SpeechRecognizer");
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed", MessageId = "sre",
            Justification = "This is suppressed because FXCop does not see our threaded dispose.")]
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.SRStop();

                if (this.SREngine != null)
                {
                    // NOTE: The SpeechRecognitionEngine can take a long time to dispose
                    // so we will dispose it on a background thread
                    ThreadPool.QueueUserWorkItem(
                        delegate(object state)
                        {
                            IDisposable toDispose = state as IDisposable;
                            if (toDispose != null)
                            {
                                toDispose.Dispose();
                            }
                        },
                            this.SREngine);
                    this.SREngine = null;
                }

                this.isDisposed = true;
            }
        }

    }
}
