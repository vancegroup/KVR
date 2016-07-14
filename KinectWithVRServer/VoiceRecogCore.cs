using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
//using Microsoft.Kinect;
using Microsoft.Speech.Recognition;
using KinectBase;

namespace KinectWithVRServer
{
    class VoiceRecogCore
    {
        ServerCore server;
        MainWindow parent;
        //bool isGUI = false;
        internal bool verbose = false;
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
            //if (parent != null)
            //{
            //    isGUI = true;
            //}
        }

        //Need an explicit destructor to ensure cleanup of the audio stream and voice recognition engine
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
            //Get the info of the voice recognizer engine the user wants to use
            RecognizerInfo recognizer = null;
            ReadOnlyCollection<RecognizerInfo> allRecognizers = SpeechRecognitionEngine.InstalledRecognizers();
            for (int i = 0; i < allRecognizers.Count; i++)
            {
                if (allRecognizers[i].Id == server.serverMasterOptions.audioOptions.recognizerEngineID)
                {
                    recognizer = allRecognizers[i];
                    break;
                }
            }
            if (recognizer == null)
            {
                throw new Exception("Couldn't find voice recognizer core.");
            }

            //Wait 4 seconds for the Kinect to be ready, may not be necessary, but the sample does this
            //Thread.Sleep(4000);

            engine = new SpeechRecognitionEngine(server.serverMasterOptions.audioOptions.recognizerEngineID);
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

            //According to the speech recognition sample, this turns off adaptation of the acoustical mode, which can degrade recognizer accuracy over time
            engine.UpdateRecognizerSetting("AdaptationOn", 0);

            if (server.serverMasterOptions.audioOptions.sourceID >= 0 && server.serverMasterOptions.audioOptions.sourceID < server.kinects.Count)
            {
                Stream audioStream = null;

                if (server.kinects[server.serverMasterOptions.audioOptions.sourceID].version == KinectVersion.KinectV1)
                {
                    ((KinectV1Wrapper.Core)server.kinects[server.serverMasterOptions.audioOptions.sourceID]).StartKinectAudio();
                    audioStream = ((KinectV1Wrapper.Core)server.kinects[server.serverMasterOptions.audioOptions.sourceID]).GetKinectAudioStream();
                }
                else if (server.kinects[server.serverMasterOptions.audioOptions.sourceID].version == KinectVersion.KinectV2)
                {
                    ((KinectV2Wrapper.Core)server.kinects[server.serverMasterOptions.audioOptions.sourceID]).StartKinectAudio();
                    audioStream = ((KinectV2Wrapper.Core)server.kinects[server.serverMasterOptions.audioOptions.sourceID]).GetKinectAudioStream();
                }
                
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
            engine.RecognizeAsyncCancel();
            engine.RecognizeAsyncStop();
            engine.Dispose();  //This was commented out.  Was it causing problems?
        }

        void engine_SpeechRecognitionRejected(object sender, SpeechRecognitionRejectedEventArgs e)
        {
            if (verbose)
            {
                HelperMethods.WriteToLog("Speech Rejected!", parent);
            }
        }

        void engine_SpeechHypothesized(object sender, SpeechHypothesizedEventArgs e)
        {
            Debug.WriteLine("Hypothesized word at time: " + e.Result.Audio.StartTime.ToString());

            if (verbose)
            {
                HelperMethods.WriteToLog("Hypothesized the word \"" + e.Result.Text + "\"", parent);
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
                                        server.UpdateButtonData(j, shortCommand.buttonNumber, shortCommand.setState);

                                        //Run a delegate to change the state back, that way, even though it uses a blocking call, it will be blocking a thread we don't care about
                                        ToggleBackMomentaryButtonDelegate buttonDelegate = ToggleBackMomentaryButton;
                                        buttonDelegate.BeginInvoke(j, shortCommand.buttonNumber, shortCommand.initialState, null, null);
                                    }
                                    else if (shortCommand.buttonType == ButtonType.Setter)
                                    {
                                        server.UpdateButtonData(j, shortCommand.buttonNumber, shortCommand.setState);
                                    }
                                    else //Toggle button
                                    {
                                        server.InvertButton(j, shortCommand.buttonNumber);
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
                                    server.UpdateTextData(j, ((VoiceTextCommand)server.serverMasterOptions.voiceCommands[i]).actionText);
                                }
                            }
                        }

                        //Write out to the log
                        HelperMethods.WriteToLog("Recognized the word \"" + e.Result.Text + "\", with the confidence of " + e.Result.Confidence.ToString("F2") + ".", parent);
                    }
                    else
                    {
                        if (verbose)
                        {
                            HelperMethods.WriteToLog("Recognized the word \"" + e.Result.Text + "\", but the confidence (" + e.Result.Confidence.ToString("F2") + ") was too low.", parent);
                        }
                    }
                }
            }
        }

        private void ToggleBackMomentaryButton(int buttonServerIndex, int buttonNumber, bool state)
        {
            Thread.Sleep(500);
            server.UpdateButtonData(buttonServerIndex, buttonNumber, state);
        }

        private delegate void ToggleBackMomentaryButtonDelegate(int buttonServerIndex, int buttonNumber, bool state);
    }
}
