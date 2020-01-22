# IL2SimFeedback


ABOUT
=====
IL2 Sturmovik Plugin for motion telemetry through SimFeedback.

https://opensfx.com/

https://github.com/SimFeedback/SimFeedback-AC-Servo


RELEASE NOTES
=============
v1.0 - First release.

INSTALLATION INSTRUCTIONS 
=========================

1. Ensure you have the .NET Framework v4.8 runtime installed.

Download: https://dotnet.microsoft.com/download/dotnet-framework/net48

2. Download and extract the latest release zip of IL2SimFeedback.

Download: https://github.com/PHARTGAMES/IL2SimFeedback/tree/master/Releases

3. Copy the contents of the SimFeedbackPlugin folder within the IL2SimFeedback .zip into your SimFeedback root folder.

4. Edit the file data/startup.cfg located within the Il2 Sturmovik game folder and add the following:

[KEY = motiondevice]  
addr = "127.0.0.1"  
      decimation = 1  
      enable = true  
      port = 4321  
[END]

Simulation produces 50Hz rate data output (output 50 samples per second) of in-game player
body's state: orientation, rotation speed (spin) and acceleration (if game mission has user-controlled
body). To reduce UDP messages output rate the above setup section contains an integer setting
“decimation”:
UDP_output_rate = Data_output_rate / decimation
The default setup makes UDP output rate  at the simulation's rate and is equal 50Hz.

AUTHOR
======

PEZZALUCIFER


SUPPORT
=======

Support available through SimFeedback owner's discord

https://opensfx.com/simfeedback-setup-and-tuning/#modes
