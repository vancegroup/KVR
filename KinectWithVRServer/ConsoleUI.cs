using System;
using System.Threading;

namespace KinectWithVRServer
{
    static class ConsoleUI
    {
        internal static void RunServerInConsole(bool isVerbose, bool autoStart, string startupFile)
        {
            Console.Clear();
            Console.WriteLine("Welcome to the Kinect With VR (KiwiVR) Server!");
            Console.WriteLine("Press the \"E\" key at any time to exit.");

            MasterSettings settings = new MasterSettings();

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
                server.kinects.Add(new KinectCore(server, null, server.serverMasterOptions.kinectOptionsList[i].kinectID));
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