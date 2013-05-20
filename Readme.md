Kinect With VR Server - KiwiVR

https://github.com/vancegroup/KiwiVR

This software manages and runs VRPN servers for Kinect voice recognition
and skeletal tracking.



Requirements:

* Microsoft .NET Framework
* [Microsoft Kinect SDK (Toolkit optional but recommended)](http://www.microsoft.com/en-us/kinectforwindows/)
* VRPN .NET Bindings
	1. [VRPN](http://www.cs.unc.edu/Research/vrpn/)
	Our research group using [this version](https://github.com/rpavlik/vrpn) of VRPN.
	2. [VRPN .NET](http://wwwx.cs.unc.edu/~chrisv/vrpnnet)



Compiling:

* Make sure the references in the Visual Studio project point to the correct DLL location.
	1. Microsoft.Kinect (Microsoft.Kinect.dll)
	2. Microsoft.Speech (Microsoft.Speech.dll)
	3. VrpnNet (VrpnNet.dll)