using System;
using ObjCRuntime;

[assembly: LinkWith ("libAFLibrary.a", LinkTarget.Simulator | LinkTarget.Arm64 | LinkTarget.ArmV7, ForceLoad = true)]
