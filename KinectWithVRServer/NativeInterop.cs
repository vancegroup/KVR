using System;
using System.Runtime.InteropServices;

namespace KinectWithVRServer
{
    //WARNING: These may break the ability to compile to 64-bit
    static internal class NativeInterop
    {
        [DllImport("kernel32.dll", SetLastError=true)]
        static internal extern bool AttachConsole(int processID);

        [DllImport("kernel32.dll", SetLastError=true)]
        static internal extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError=true)]
        static internal extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError=true)]
        static internal extern int GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern bool SetConsoleMode(IntPtr consoleHandle, uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern bool GetConsoleMode(IntPtr consoleHandle, out uint mode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static internal extern IntPtr GetStdHandle(int handleType); //-10, -11, or -12 (input, output, and error respectively)
    }
}
