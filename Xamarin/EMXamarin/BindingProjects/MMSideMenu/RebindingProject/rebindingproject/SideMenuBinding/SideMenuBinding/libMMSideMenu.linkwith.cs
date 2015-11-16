using System;
using ObjCRuntime;

[assembly: LinkWith ("libMMSideMenu.a", LinkTarget.Simulator | LinkTarget.ArmV6 | LinkTarget.ArmV7 | LinkTarget.Arm64, Frameworks = "UIKit Foundation CoreGraphics", ForceLoad = true)]
