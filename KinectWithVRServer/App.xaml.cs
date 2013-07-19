using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

namespace KinectWithVRServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            //Argument booleans
            bool parentCommandLine = false;
            bool newCommandLine = false;
            bool help = false;
            bool connected = false;
            bool verbose = false;
            bool autoStart = false;
            string startupFile = "";

            string[] args = e.Args;

            //Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if((args[i].ToLower() == "-c" || args[i].ToLower() == "/c") && !newCommandLine)
                {
                    parentCommandLine = true;
                }
                else if (args[i].ToLower() == "-nc" || args[i].ToLower() == "/nc")
                {
                    parentCommandLine = false;
                    newCommandLine = true;
                }
                else if (args[i].ToLower() == "-v" || args[i].ToLower() == "/v")
                {
                    verbose = true;
                }
                else if (args[i].ToLower() == "-s" || args[i].ToLower() == "/s")
                {
                    autoStart = true;
                }
                else if(args[i].ToLower() == "-h" || args[i].ToLower() == "/h" || args[i].ToLower() == "-?" || args[i].ToLower() == "/?")
                {
                    help = true;
                    connected = NativeInterop.AttachConsole(-1);
                    if (connected)
                    {
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("Usage: KinectWithVRServer [filename] [/c] [/nc] [/s] [/v]");
                        Console.WriteLine();
                        Console.WriteLine("Options:");
                        Console.WriteLine("\t/c\tLaunches the program in the pre-existing command line.");
                        Console.WriteLine("\t/nc\tLaunches the program in a new command line window.");
                        Console.WriteLine("\t/?\tShows this help message.");
                        Console.WriteLine("\t/h\tShows this help message.");
                        Console.WriteLine("\t/s\tStarts the server immediately upon program launch.\r\n\t\tThis is implied when launched in console mode.");
                        Console.WriteLine("\t/v\tVerbose output mode.");
                        NativeInterop.FreeConsole();
                    }
                }
                else if (i == 0)
                {
                    startupFile = args[i];
                }
            }

            if (!help)
            {
                if (newCommandLine || parentCommandLine)
                {
                    if (newCommandLine)
                    {
                        connected = NativeInterop.AllocConsole();

                    }
                    else if (parentCommandLine)
                    {
                        connected = NativeInterop.AttachConsole(-1);

                        IntPtr consoleHandle = NativeInterop.GetStdHandle(-10);
                        uint consoleMode = 0;
                        NativeInterop.GetConsoleMode(consoleHandle, out consoleMode);
                        NativeInterop.SetConsoleMode(consoleHandle, (uint)(consoleMode & (~0x0002)));
                    }

                    if (connected)
                    {
                        ConsoleUI.RunServerInConsole(verbose, autoStart, startupFile);
                    }
                }
                else
                {
                    MainWindow gui = new MainWindow(verbose, autoStart, startupFile);

                    gui.ShowDialog();
                }
            }
            
            this.Shutdown();
        }
    }
}