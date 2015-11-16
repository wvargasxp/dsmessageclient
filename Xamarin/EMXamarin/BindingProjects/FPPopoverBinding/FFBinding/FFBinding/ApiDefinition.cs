using System.Drawing;
using System;

using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.ObjCRuntime;

namespace FFBinding {

	// [BaseType (typeof (UIView))]
	// public partial interface FPPopoverView {
	//
	// [Export ("title", ArgumentSemantic.Retain)]
	// string Title { get; set; }
	//
	// [Export ("relativeOrigin", ArgumentSemantic.Assign)]
	// PointF RelativeOrigin { get; set; }
	//
	// [Export ("tint")]
	// FPPopoverTint Tint { get; set; }
	//
	// [Export ("draw3dBorder")]
	// bool Draw3dBorder { get; set; }
	//
	// [Export ("border")]
	// bool Border { get; set; }
	//
	// [Export ("arrowDirection"), Verify ("ObjC method massaged into setter property", "/Users/james/Downloads/Download All Attachments [Bindings]/FPPopover/FPPopover/FPPopoverView.h", Line = 53), Verify ("Backing getter method to ObjC property removed: arrowDirection", "/Users/james/Downloads/Download All Attachments [Bindings]/FPPopover/FPPopover/FPPopoverView.h", Line = 54)]
	// FPPopoverArrowDirection ArrowDirection { get; set; }
	//
	// [Export ("addContentView:")]
	// void AddContentView (UIView contentView);
	// }

	[BaseType (typeof (UIView))]
	interface FPPopoverView {

		// @property (nonatomic, strong) NSString * title;
		[Export ("title", ArgumentSemantic.Retain)]
		string Title { get; set; }

		// @property (assign, nonatomic) CGPoint relativeOrigin;
		[Export ("relativeOrigin", ArgumentSemantic.UnsafeUnretained)]
		RectangleF RelativeOrigin { get; set; }

		// @property (assign, nonatomic) FPPopoverTint tint;
		[Export ("tint", ArgumentSemantic.UnsafeUnretained)]
		FPPopoverTint Tint { get; set; }

		// @property (assign, nonatomic) BOOL draw3dBorder;
		[Export ("draw3dBorder", ArgumentSemantic.UnsafeUnretained)]
		bool Draw3dBorder { get; set; }

		// @property (assign, nonatomic) BOOL border;
		[Export ("border", ArgumentSemantic.UnsafeUnretained)]
		bool Border { get; set; }

		// // @required - (void)setArrowDirection:(FPPopoverArrowDirection)arrowDirection;
		// [Export ("setArrowDirection:")]
		// void SetArrowDirection (FPPopoverArrowDirection arrowDirection);
		//
		// // @required - (FPPopoverArrowDirection)arrowDirection;
		// [Export ("arrowDirection")]
		// FPPopoverArrowDirection ArrowDirection ();

		FPPopoverArrowDirection ArrowDirection {
			[Export ("arrowDirection")]
			get;
			[Export ("setArrowDirection:")]
			set;
		}

		// @required - (void)addContentView:(UIView *)contentView;
		[Export ("addContentView:")]
		void AddContentView (UIView contentView);
	}


	// [BaseType (typeof (UIView))]
	// public partial interface FPTouchView {
	//
	// [Export ("touchedOutsideBlock"), Verify ("ObjC method massaged into setter property", "/Users/james/Downloads/Download All Attachments [Bindings]/FPPopover/FPPopover/FPTouchView.h", Line = 20)]
	//// FPTouchedOutsideBlock TouchedOutsideBlock { set; }
	// Action TouchedOutsideBlock { set; }
	//
	// [Export ("touchedInsideBlock"), Verify ("ObjC method massaged into setter property", "/Users/james/Downloads/Download All Attachments [Bindings]/FPPopover/FPPopover/FPTouchView.h", Line = 22)]
	//// FPTouchedInsideBlock TouchedInsideBlock { set; }
	// Action TouchedInsideBlock { set; }
	// }

	[BaseType (typeof (UIView))]
	interface FPTouchView {

		// @required - (void)setTouchedOutsideBlock:(FPTouchedOutsideBlock)outsideBlock;
		[Export ("setTouchedOutsideBlock:")]
		void SetTouchedOutsideBlock (Action outsideBlock);

		// @required - (void)setTouchedInsideBlock:(FPTouchedInsideBlock)insideBlock;
		[Export ("setTouchedInsideBlock:")]
		void SetTouchedInsideBlock (Action insideBlock);
	}


	// [Model, BaseType (typeof (NSObject))]
	// public partial interface FPPopoverControllerDelegate {
	//
	// [Export ("popoverControllerDidDismissPopover:")]
	// void DidDismissPopover (FPPopoverController popoverController);
	//
	// [Export ("presentedNewPopoverController:shouldDismissVisiblePopover:")]
	// void ShouldDismissVisiblePopover (FPPopoverController newPopoverController, FPPopoverController visiblePopoverController);
	// }

	interface IFPPopoverControllerDelegate {}

	[Protocol, Model]
	[BaseType (typeof (NSObject))]
	interface FPPopoverControllerDelegate {

		// @optional - (void)popoverControllerDidDismissPopover:(FPPopoverController *)popoverController;
		[Export ("popoverControllerDidDismissPopover:")]
		void PopoverControllerDidDismissPopover (FPPopoverController popoverController);

		// @optional - (void)presentedNewPopoverController:(FPPopoverController *)newPopoverController shouldDismissVisiblePopover:(FPPopoverController *)visiblePopoverController;
		[Export ("presentedNewPopoverController:shouldDismissVisiblePopover:")]
		void PresentedNewPopoverController (FPPopoverController newPopoverController, FPPopoverController visiblePopoverController);
	}


	// [BaseType (typeof (UIViewController))]
	// public partial interface FPPopoverController {
	//
	// [Export ("delegate", ArgumentSemantic.Assign)]
	// FPPopoverControllerDelegate Delegate { get; set; }
	//
	// [Export ("touchView")]
	// FPTouchView TouchView { get; }
	//
	// [Export ("contentView")]
	// FPPopoverView ContentView { get; }
	//
	// [Export ("arrowDirection")]
	// FPPopoverArrowDirection ArrowDirection { get; set; }
	//
	// [Export ("contentSize", ArgumentSemantic.Assign)]
	// SizeF ContentSize { get; set; }
	//
	// [Export ("origin", ArgumentSemantic.Assign)]
	// PointF Origin { get; set; }
	//
	// [Export ("alpha")]
	// float Alpha { get; set; }
	//
	// [Export ("tint")]
	// FPPopoverTint Tint { get; set; }
	//
	// [Export ("border")]
	// bool Border { get; set; }
	//
	// [Export ("initWithViewController:")]
	// IntPtr Constructor (UIViewController viewController);
	//
	// [Export ("initWithViewController:delegate:")]
	// IntPtr Constructor (UIViewController viewController, FPPopoverControllerDelegate _delegate);
	//
	// [Export ("presentPopoverFromView:")]
	// void PresentPopoverFromView (UIView fromView);
	//
	// [Export ("presentPopoverFromPoint:")]
	// void PresentPopoverFromPoint (PointF fromPoint);
	//
	// [Export ("dismissPopoverAnimated:")]
	// void DismissPopoverAnimated (bool animated);
	//
	// [Export ("dismissPopoverAnimated:completion:")]
	// void DismissPopoverAnimated (bool animated, Action completionBlock);
	//
	// [Export ("shadowsHidden"), Verify ("ObjC method massaged into setter property", "/Users/james/Downloads/Download All Attachments [Bindings]/FPPopover/FPPopover/FPPopoverController.h", Line = 78)]
	// bool ShadowsHidden { set; }
	//
	// [Export ("setupView")]
	// void SetupView ();
	// }

	[BaseType (typeof (UIViewController))]
	interface FPPopoverController {

		// @required - (id)initWithViewController:(UIViewController *)viewController;
		[Export ("initWithViewController:")]
		IntPtr Constructor (UIViewController viewController);

		// @required - (id)initWithViewController:(UIViewController *)viewController delegate:(id<FPPopoverControllerDelegate>)delegate;
		[Export ("initWithViewController:delegate:")]
		IntPtr Constructor (UIViewController viewController, FPPopoverControllerDelegate del);

		// @property (assign, nonatomic) id<FPPopoverControllerDelegate> delegate;
		[Export ("delegate", ArgumentSemantic.UnsafeUnretained)]
		[NullAllowed]
		IFPPopoverControllerDelegate Delegate { get; set; }

		// @property (readonly, nonatomic) FPTouchView * touchView;
		[Export ("touchView")]
		FPTouchView TouchView { get; }

		// @property (readonly, nonatomic) FPPopoverView * contentView;
		[Export ("contentView")]
		FPPopoverView ContentView { get; }

		// @property (assign, nonatomic) FPPopoverArrowDirection arrowDirection;
		[Export ("arrowDirection", ArgumentSemantic.UnsafeUnretained)]
		FPPopoverArrowDirection ArrowDirection { get; set; }

		// @property (assign, nonatomic) CGSize contentSize;
		[Export ("contentSize", ArgumentSemantic.UnsafeUnretained)]
		SizeF ContentSize { get; set; }

		// @property (assign, nonatomic) CGPoint origin;
		[Export ("origin", ArgumentSemantic.UnsafeUnretained)]
		PointF Origin { get; set; }

		// @property (assign, nonatomic) CGFloat alpha;
		[Export ("alpha", ArgumentSemantic.UnsafeUnretained)]
		float Alpha { get; set; }

		// @property (assign, nonatomic) FPPopoverTint tint;
		[Export ("tint", ArgumentSemantic.UnsafeUnretained)]
		FPPopoverTint Tint { get; set; }

		// @property (assign, nonatomic) BOOL border;
		[Export ("border", ArgumentSemantic.UnsafeUnretained)]
		bool Border { get; set; }

		// @required - (void)presentPopoverFromView:(UIView *)fromView;
		[Export ("presentPopoverFromView:")]
		void PresentPopoverFromView (UIView fromView);

		// @required - (void)presentPopoverFromPoint:(CGPoint)fromPoint;
		[Export ("presentPopoverFromPoint:")]
		void PresentPopoverFromPoint (PointF fromPoint);

		// @required - (void)dismissPopoverAnimated:(BOOL)animated;
		[Export ("dismissPopoverAnimated:")]
		void DismissPopoverAnimated (bool animated);

		// @required - (void)dismissPopoverAnimated:(BOOL)animated completion:(FPPopoverCompletion)completionBlock;
		[Export ("dismissPopoverAnimated:completion:")]
		void DismissPopoverAnimated (bool animated, Action completionBlock);

		// @required - (void)setShadowsHidden:(BOOL)hidden;
		[Export ("setShadowsHidden:")]
		void SetShadowsHidden (bool hidden);

		// @required - (void)setupView;
		[Export ("setupView")]
		void SetupView ();
	}

}