using System;
using System.Threading;

namespace KinectWithVRServer
{
    static class ConsoleUI
    {
        internal static void RunServerInConsole(bool isVerbose, bool autoStart, string startupFile, AvaliableDLLs dlls)
        {
            Console.Clear();
            Console.WriteLine("Welcome to the Kinect With VR (KVR) Server!");
            Console.WriteLine("Press the \"E\" key at any time to exit.");

            //Notify the user if DLLs are missing
            if (!dlls.HasKinectV1)
            {
                Console.WriteLine("Warning: Kinect v1 support is unavaliable due to missing DLLs");
            }
            if (!dlls.HasKinectV2)
            {
                Console.WriteLine("Warning: Kinect v2 support is unavaliable due to missing DLLs");
            }
            if (!dlls.HasNetworkedKinect)
            {
                Console.WriteLine("Warning: Networked Kinect support is unavaliable due to missing DLLs");
            }

            KinectBase.MasterSettings settings = new KinectBase.MasterSettings();

            try
            {
                settings = HelperMethods.LoadSettings(startupFile);
            }
            catch
            {
                HelperMethods.WriteToLog("Cannot open settings file!");
            }

            ServerCore server = new ServerCore(isVerbose, settings);
            for (int i = 0; i < server.serverMasterOptions.kinectOptionsList.Count; i++) //Launch the Kinects
            {
                if (server.serverMasterOptions.kinectOptionsList[i].version == KinectBase.KinectVersion.KinectV1)
                {
                    if (dlls.HasKinectV1)
                    {
                        //server.kinects.Add(new KinectV1Core.KinectCoreV1(ref server.serverMasterOptions, false, server.serverMasterOptions.kinectOptionsList[i].kinectID));
                        server.kinects.Add(new KinectV1Wrapper.Core(ref server.serverMasterOptions, false, server.serverMasterOptions.kinectOptionsList[i].kinectID));
                    }
                    else
                    {
                        Console.WriteLine("Cannot load Kinect v1 with ID: {0} due to missing DLLs.", server.serverMasterOptions.kinectOptionsList[i].kinectID);
                    }
                }
                else if (server.serverMasterOptions.kinectOptionsList[i].version == KinectBase.KinectVersion.KinectV2)
                {
                    if (dlls.HasKinectV2)
                    {
                        server.kinects.Add(new KinectV2Wrapper.Core(ref server.serverMasterOptions, false, server.serverMasterOptions.kinectOptionsList[i].kinectID));
                    }
                    else
                    {
                        Console.WriteLine("Cannot load Kinect v2 with ID: {0} due to missing DLLs.", server.serverMasterOptions.kinectOptionsList[i].kinectID);
                    }
                }
                else if (server.serverMasterOptions.kinectOptionsList[i].version == KinectBase.KinectVersion.NetworkKinect)
                {
                    if (dlls.HasNetworkedKinect)
                    {
                        server.kinects.Add(new NetworkKinectWrapper.Core(ref server.serverMasterOptions, false, server.serverMasterOptions.kinectOptionsList[i].kinectID, server.serverMasterOptions.kinectOptionsList[i].uniqueKinectID));
                    }
                    else
                    {
                        Console.WriteLine("Cannot load network Kinect with ID: {0} due to missing DLLs.", server.serverMasterOptions.kinectOptionsList[i].kinectID);
                    }
                }
                else
                {
                    Console.WriteLine("Kinect number {0} was of an unknown version and could not be opened.", i);
                }
            }
            server.launchServer(); //This will still try to launch with default settings even if the settings load fails

            bool running = true;
            while (running)
            {
                Thread.Sleep(100);
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(false);
                    if (key.Key == ConsoleKey.E || (key.Key == ConsoleKey.C && key.Modifiers == ConsoleModifiers.Control))
                    {
                        running = false;
                    }
                }
            }

            Console.WriteLine(); //Write a blank so the next statement has its own line
            Console.WriteLine("Shutting down the server.  Please wait...");
            server.stopServer();

            NativeInterop.FreeConsole();
        }
    }
}
