using System;
using ObjCRuntime;

[assembly: LinkWith ("libSocketRocket.a", LinkTarget.ArmV7 | LinkTarget.Simulator | LinkTarget.Arm64, "-licucore", ForceLoad = true)]
