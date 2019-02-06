# Kinect With VR Server - KVR

https://github.com/vancegroup/KVR

This software manages and runs VRPN servers for one or more Microsoft Kinect sensors and enables voice recognition, gesture recognition, and skeleton merging.


## Requirements:

* [Microsoft .NET Framework](http://www.microsoft.com/en-us/download/details.aspx?id=30653) (currently tested against 4.5)
* [Microsoft Kinect v1 SDK along with the Toolkit](http://www.microsoft.com/en-us/kinectforwindows/) (currently tested against 1.8)
* [Microsoft Kinect v2 SDK](http://www.microsoft.com/en-us/download/details.aspx?id=44561) (currently tested against 2.0.1410.19000)
* [Microsoft Speech Platform SDK](http://www.microsoft.com/en-us/download/details.aspx?id=27226) (currently tested against version 11)
* VRPN .NET Bindings
	1. [VRPN](http://www.cs.unc.edu/Research/vrpn/)
	Our research group using [this fork](https://github.com/rpavlik/vrpn) of VRPN.
	2. [VrpnNet](http://wwwx.cs.unc.edu/~chrisv/vrpnnet)
	Our research group using [this fork](https://github.com/vancegroup/VrpnNet) of VrpnNet.
* [Eigen .NET Bindings](https://github.com/vancegroup/EigenNet) (precompiled binaries included)


## Compiling Hints:

* Make sure the references in the Visual Studio project point to the correct DLL location.
	1. Microsoft.Kinect (Microsoft.Kinect.dll)
	2. Microsoft.Speech (Microsoft.Speech.dll)
	3. VrpnNet (VrpnNet.dll)
* Both the Kinect v1 and Kinect v2 use the Microsoft.Kinect namespace and a library called Microsof.Kinect.dll, but the two are not compatible.  Make sure you reference the correct version in the correct project.
* The project can be compiled and run on a Windows 7 computer without the Kinect v2 SDK installed, provided a precompiled version of the KinectV2Core is available and the KinectV2Core project is set to not build.  KVR will run without Kinect v2 support in this case.
* The debug versions of VrpnNet require Visual Studio 2010 to be installed. If VS 2010 is not installed, link KVR to the release version of VrpnNet for the appropriate CPU architecture.
* Matrix calculations in the Eigen .NET bindings will be extremely slow if the project is using a debug version of the Eigen .NET bindings.
