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
                        server.kinects.Add(new KinectV1Core.KinectCoreV1(ref server.serverMasterOptions, false, server.serverMasterOptions.kinectOptionsList[i].kinectID));
                    }
                    else
                    {
                        Console.WriteLine("Cannot load Kinect v1 with ID: server.serverMasterOptions.kinectOptionsList[i].kinectID due to missing DLLs.");
                    }
                }
                else if (server.serverMasterOptions.kinectOptionsList[i].version == KinectBase.KinectVersion.KinectV2)
                {
                    //TODO: Implement opening kinect V2s from the console
                    Console.WriteLine("Kinect number {0} is a Kinect V2, which is not yet supported.", i);
                }
                else if (server.serverMasterOptions.kinectOptionsList[i].version == KinectBase.KinectVersion.NetworkKinect)
                {
                    //TODO: Implement opening networked kinects from the console
                    Console.WriteLine("Kinect number {0} is a networked Kinect, which is not yet supported.", i);
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

            Console.WriteLine("Shutting down the server.  Please wait...");
            server.stopServer();

            NativeInterop.FreeConsole();
        }
    }
}
