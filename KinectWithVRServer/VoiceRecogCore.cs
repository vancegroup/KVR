using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using Microsoft.Speech;
using Microsoft.Speech.Recognition;
using Vrpn;
using System.Threading;
using System.IO;

namespace KinectWithVRServer
{
    class VoiceRecogCore
    {
        ServerCore server;
        MainWindow parent;
        bool isGUI = false;
        bool verbose = false;
        SpeechRecognitionEngine engine = null;
        Stream audioStream = null;

        public VoiceRecogCore(ServerCore vrpnServer, bool verboseOutput, MainWindow thisParent = null)
        {
            server = vrpnServer;
            verbose = verboseOutput;
            if (server == null)
            {
                throw new Exception("The VRPN server does not exist!");
            }

            parent = thisParent;
            if (parent != null)
            {
                isGUI = true;
            }
        }

        //Need an explicit destructor to cleanup the audio stream and voice recognition engine
        ~VoiceRecogCore()
        {
            if (engine != null)
            {
                engine.Dispose();
            }
            if (audioStream != null)
            {
                audioStream.Close();
                audioStream.Dispose();
            }
        }

        public void launchVoiceRecognizer()
        {
            //Setup the audio source
            KinectAudioSource source = server.kinect.kinect.AudioSource;
            source.EchoCancellationMode = EchoCancellationMode.None; //May need to be an option somewhere
            source.AutomaticGainControlEnabled = false; //Needs to be this way for voice recognition

            RecognizerInfo recognizer = GetKinectRecognizer();
            if (recognizer == null)
            {
                throw new Exception("Couldn't find voice recognizer core.");
            }

            //Wait 4 seconds for the Kinect to be ready, may not be necessary, but the sample does this
            Thread.Sleep(4000);

            engine = new SpeechRecognitionEngine(recognizer.Id);
            Choices vocab = new Choices();
            for (int i = 0; i < server.serverMasterOptions.voiceCommands.Count; i++)
            {
                vocab.Add(server.serverMasterOptions.voiceCommands[i].recognizedWord);
            }

            GrammarBuilder gb = new GrammarBuilder { Culture = recognizer.Culture };
            gb.Append(vocab);
            Grammar grammar = new Grammar(gb);
            engine.LoadGrammar(grammar);

            //Setup events
            engine.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(engine_SpeechRecognized);
            engine.SpeechHypothesized += new EventHandler<SpeechHypothesizedEventArgs>(engine_SpeechHypothesized);
            engine.SpeechRecognitionRejected += new EventHandler<SpeechRecognitionRejectedEventArgs>(engine_SpeechRecognitionRejected);

            if (server.serverMasterOptions.kinectOptions.useKinectAudio)
            {
                audioStream = source.Start();
                engine.SetInputToAudioStream(audioStream, new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(Microsoft.Speech.AudioFormat.EncodingFormat.Pcm, 16000, 16, 1, 32000, 2, null));
            }
            else
            {
                engine.SetInputToDefaultAudioDevice();
            }

            engine.RecognizeAsync(RecognizeMode.Multiple);
        }

        internal void stopVoiceRecognizer()
        {
            engine.RecognizeAsyncStop();
            audioStream.Close();
            audioStream.Dispose();
            //engine.Dispose();
        }

        void engine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (verbose)
            {
                if (isGUI)
                {
                    parent.WriteToLog("Speech Rejected!");
                }
                else
                {
                    Console.WriteLine("Speech Rejected!");
                }
            }
        }

        void engine_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            if (verbose)
            {
                if (isGUI)
                {
                    parent.WriteToLog("Hypothesized the word \"" + e.Result.Text + "\"");
                }
                else
                {
                    Console.WriteLine("Hypothesized the word \"{0}\"", e.Result.Text);
                }
            }
        }

        void engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            for (int i = 0; i < server.serverMasterOptions.voiceCommands.Count; i++)
            {
                if (server.serverMasterOptions.voiceCommands[i].recognizedWord.ToLower()== e.Result.Text.ToLower())
                {
                    if (e.Result.Confidence >= server.serverMasterOptions.voiceCommands[i].confidence)
                    {
                        //Send across VRPN
                        if (server.serverMasterOptions.voiceCommands[i].serverType == ServerType.Button)
                        {
                            for (int j = 0; j < server.serverMasterOptions.buttonServers.Count; j++)
                            {
                                if (server.serverMasterOptions.buttonServers[j].serverName == server.serverMasterOptions.voiceCommands[i].serverName)
                                {
                                    VoiceButtonCommand shortCommand =  (VoiceButtonCommand)server.serverMasterOptions.voiceCommands[i];
                                    if (shortCommand.buttonType == ButtonType.Momentary)
                                    {
                                        server.buttonServers[i].Buttons[shortCommand.buttonNumber] = shortCommand.setState;
                                        Thread.Sleep(500);
                                        server.buttonServers[i].Buttons[shortCommand.buttonNumber] = shortCommand.initialState;
                                    }
                                    else if (shortCommand.buttonType == ButtonType.Setter)
                                    {
                                        server.buttonServers[i].Buttons[shortCommand.buttonNumber] = shortCommand.setState;
                                    }
                                    else //Toggle button
                                    {
                                        server.buttonServers[i].Buttons[shortCommand.buttonNumber] = !server.buttonServers[i].Buttons[shortCommand.buttonNumber];
                                    }
                                }
                            }
                        }
                        else if (server.serverMasterOptions.voiceCommands[i].serverType == ServerType.Text)
                        {
                            for (int j = 0; j < server.serverMasterOptions.textServers.Count; j++)
                            {
                                if (server.serverMasterOptions.textServers[j].serverName == server.serverMasterOptions.voiceCommands[i].serverName)
                                {
                                    server.textServers[j].SendMessage(((VoiceTextCommand)server.serverMasterOptions.voiceCommands[i]).actionText);
                                }
                            }
                        }

                        //TODO: Send the audio source angle here

                        //Write out to the log
                        if (isGUI)
                        {
                            parent.WriteToLog("Recognized the word \"" + e.Result.Text + "\", with the confidence of " + e.Result.Confidence.ToString("F2") + ".");
                        }
                        else
                        {
                            Console.WriteLine("Recognized the word \"{0}\", with the confidence of {1}.", e.Result.Text, e.Result.Confidence.ToString("F2"));
                        }
                    }
                    else
                    {
                        if (verbose)
                        {
                            if (isGUI)
                            {
                                parent.WriteToLog("Recognized the word \"" + e.Result.Text + "\", but the confidence (" + e.Result.Confidence.ToString("F2") + ") was too low.");
                            }
                            else
                            {
                                Console.WriteLine("Recognized the word \"{0}\", but the confidence ({1}) was too low.", e.Result.Text, e.Result.Confidence.ToString("F2"));
                            }
                        }
                    }
                }
            }
        }

        //Note: there may be multiple recognizes installed on a computer, this picks the one for the Kinect
        //However, we do not HAVE to use that one.  This should probably be an option somewhere.
        private RecognizerInfo GetKinectRecognizer()
        {
            Func<RecognizerInfo, bool> matchingFunc = r =>
            {
                string value;
                r.AdditionalInfo.TryGetValue("Kinect", out value);
                return "True".Equals(value, StringComparison.InvariantCultureIgnoreCase) && "en-US".Equals(r.Culture.Name, StringComparison.InvariantCultureIgnoreCase);
            };
            return SpeechRecognitionEngine.InstalledRecognizers().Where(matchingFunc).FirstOrDefault();
        }
    }
}
