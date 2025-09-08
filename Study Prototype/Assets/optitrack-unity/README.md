# Unity-Plugin-OptiTrack
OptiTrack plugin for the game engine Unity. 


## How to Setup a Project 

In order to start developing on the Unity plugin you must first create a regular 3D example Unity project, then sync the GitHub projects into the "Assets" folder. Unity will then load all the resources for the plugin into the project automatically and you can start developing. 


## How to Start Developing
Here are the official [Unity Docs](https://docs.unity3d.com/Manual/VisualStudioIntegration.html#:~:text=Follow%20these%20steps%20to%20configure,as%20your%20preferred%20external%20editor.&text=Next%2C%20doubleclick%20a%20C%23%20file,open%20that%20file%20for%20you.) on setting up Visual Studio as the debug environment. 

Roughly this involves the setting a version of Visual Studio as the script editor: 

1. Go to Edit > Preferences > External Tools
2. Set "External Script Editor" to "Visual Studio ####"

And also attaching the editor to Unity to hit debug breakpoints:

1. Setup a Unity Project with the OptiTrack plugin repository.
2. Double click one of the C# files.
3. In Visual Studio press the "Attach to Unity" button where the build button usually is located. 
4. Play your game in Unity.
5. Anytime debug breakpoints in the C# files are hit, then you can debug in Visual Studio. 

 
## Packaging Plugins for Release
Packaging plugins for Unity requires the following steps:

1. Select the plugin root directory.
2. Right click and select "Export Package..."
3. This will generate a (.unitypackage) file, which is all you need. 

 
## Gotchas

- The "Native.cs" and "Client.cs" files in the Unity client are very important. They link with NatNetCAPI.cpp/h which is how a C# application can link to a streaming client primarily written in C++. There is a project in the NatNetSDK that also interfaces with these two files called the PInvoke sample. If you make updates to these files, then also make the same changes to the files in the PInvoke sample in the Perforce repository for Motive as well. 
