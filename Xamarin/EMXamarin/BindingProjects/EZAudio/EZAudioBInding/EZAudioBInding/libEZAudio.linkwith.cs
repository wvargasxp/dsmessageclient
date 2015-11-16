using System;
using ObjCRuntime;

[assembly: LinkWith ("libEZAudio.a", LinkTarget.ArmV7 | LinkTarget.Simulator | LinkTarget.Arm64, Frameworks="GLKit AudioToolbox AVFoundation", SmartLink = false, ForceLoad = true)]
