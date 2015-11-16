using System;
using MonoTouch.ObjCRuntime;

namespace FFBinding
{
	// public enum FPPopoverArrowDirection {
	// Up = (int)1UL << 0,
	// Down = (int)1UL << 1,
	// Left = (int)1UL << 2,
	// Right = (int)1UL << 3,
	// NoArrow = (int)1UL << 4,
	// Vertical = FPPopoverArrowDirection.Up | FPPopoverArrowDirection.Down | FPPopoverArrowDirection.NoArrow,
	// Horizontal = FPPopoverArrowDirection.Left | FPPopoverArrowDirection.Right,
	// Any = FPPopoverArrowDirection.Up | FPPopoverArrowDirection.Down | FPPopoverArrowDirection.Left | FPPopoverArrowDirection.Right
	// }
	//
	// public enum FPPopoverTint {
	// WhiteTint,
	// BlackTint,
	// LightGrayTint,
	// GreenTint,
	// RedTint,
	// DefaultTint = FPPopoverTint.BlackTint
	// }

	[Native]
	public enum FPPopoverArrowDirection : uint {
		Up = 1U << 0,
		Down = 1U << 1,
		Left = 1U << 2,
		Right = 1U << 3,
		NoArrow = 1U << 4,
		Vertical = FPPopoverArrowDirection.Up | FPPopoverArrowDirection.Down | FPPopoverArrowDirection.NoArrow,
		Horizontal = FPPopoverArrowDirection.Left | FPPopoverArrowDirection.Right,
		Any = FPPopoverArrowDirection.Up | FPPopoverArrowDirection.Down | FPPopoverArrowDirection.Left | FPPopoverArrowDirection.Right
	}

	public enum FPPopoverTint : uint {
		WhiteTint,
		BlackTint,
		LightGrayTint,
		GreenTint,
		RedTint,
		DefaultTint = FPPopoverTint.BlackTint
	}

}