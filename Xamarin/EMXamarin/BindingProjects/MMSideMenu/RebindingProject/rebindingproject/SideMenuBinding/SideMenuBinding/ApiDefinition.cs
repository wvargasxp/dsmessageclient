using System;
using System.Drawing;
using ObjCRuntime;
using Foundation;
using UIKit;

namespace SideMenuBinding
{

	[BaseType (typeof (UIBarButtonItem))]
	public partial interface MMDrawerBarButtonItem {

		[Export ("initWithTarget:action:")]
		IntPtr Constructor (NSObject target, Selector action);

		[Export ("menuButtonColorForState:")]
		UIColor MenuButtonColorForState (UIControlState state);

		[Export ("setMenuButtonColor:forState:")]
		void SetMenuButtonColor (UIColor color, UIControlState state);

		[Export ("shadowColorForState:")]
		UIColor ShadowColorForState (UIControlState state);

		[Export ("setShadowColor:forState:")]
		void SetShadowColor (UIColor color, UIControlState state);
	}



	[BaseType (typeof (UIViewController))]
	public partial interface MMDrawerController {

		[Export ("centerViewController", ArgumentSemantic.Retain)]
		UIViewController CenterViewController { get; set; }

		[Export ("leftDrawerViewController", ArgumentSemantic.Retain)]
		UIViewController LeftDrawerViewController { get; set; }

		[Export ("rightDrawerViewController", ArgumentSemantic.Retain)]
		UIViewController RightDrawerViewController { get; set; }

		[Export ("maximumLeftDrawerWidth")]
		float MaximumLeftDrawerWidth { get; set; }

		[Export ("maximumRightDrawerWidth")]
		float MaximumRightDrawerWidth { get; set; }

		[Export ("visibleLeftDrawerWidth")]
		float VisibleLeftDrawerWidth { get; }

		[Export ("visibleRightDrawerWidth")]
		float VisibleRightDrawerWidth { get; }

		[Export ("animationVelocity")]
		float AnimationVelocity { get; set; }

		[Export ("shouldStretchDrawer")]
		bool ShouldStretchDrawer { get; set; }

		[Export ("openSide")]
		MMDrawerSide OpenSide { get; }

		[Export ("openDrawerGestureModeMask")]
		MMOpenDrawerGestureMode OpenDrawerGestureModeMask { get; set; }

		[Export ("closeDrawerGestureModeMask")]
		MMCloseDrawerGestureMode CloseDrawerGestureModeMask { get; set; }

		[Export ("centerHiddenInteractionMode")]
		MMDrawerOpenCenterInteractionMode CenterHiddenInteractionMode { get; set; }

		[Export ("showsShadow")]
		bool ShowsShadow { get; set; }

		[Export ("showsStatusBarBackgroundView")]
		bool ShowsStatusBarBackgroundView { get; set; }

		[Export ("statusBarViewBackgroundColor", ArgumentSemantic.Retain)]
		UIColor StatusBarViewBackgroundColor { get; set; }

		[Export ("initWithCenterViewController:leftDrawerViewController:rightDrawerViewController:")]
		IntPtr Constructor (UIViewController centerViewController, UIViewController leftDrawerViewController, UIViewController rightDrawerViewController);

		[Export ("initWithCenterViewController:leftDrawerViewController:")]
		IntPtr Constructor (UIViewController centerViewController, UIViewController leftDrawerViewController);

//		[Export ("toggleDrawerSide:animated:completion:")]
//		void ToggleDrawerSide (MMDrawerSide drawerSide, bool animated, Delegate completion);
//
//		[Export ("closeDrawerAnimated:completion:")]
//		void CloseDrawerAnimated (bool animated, Delegate completion);
//
//		[Export ("openDrawerSide:animated:completion:")]
//		void OpenDrawerSide (MMDrawerSide drawerSide, bool animated, Delegate completion);

		//can't pass Delegate completion to objective c from c#, so omit parameter
		[Export ("toggleDrawerSide:animated:")]
		void ToggleDrawerSide (MMDrawerSide drawerSide, bool animated);

		[Export ("closeDrawerAnimated:")]
		void CloseDrawerAnimated (bool animated);

		[Export ("openDrawerSide:animated:")]
		void OpenDrawerSide (MMDrawerSide drawerSide, bool animated);


		//*used internally, if you need to expose thses you need a method signature that does not contain Delegate completion

//		[Export ("setCenterViewController:withCloseAnimation:completion:")]
//		void SetCenterViewControllerWithCloseAnimation (UIViewController centerViewController, bool closeAnimated, Delegate completion);
//
//		[Export ("setCenterViewController:withFullCloseAnimation:completion:")]
//		void SetCenterViewControllerWithFullCloseAnimation (UIViewController newCenterViewController, bool fullCloseAnimated, Delegate completion);



//		[Export ("setMaximumLeftDrawerWidth:animated:completion:")]
//		void SetMaximumLeftDrawerWidth (float width, bool animated, Delegate completion);
//
//		[Export ("setMaximumRightDrawerWidth:animated:completion:")]
//		void SetMaximumRightDrawerWidth (float width, bool animated, Delegate completion);

		//*can't pass Delegate completion to objective c from c#, so omit parameter

		[Export ("setMaximumLeftDrawerWidth:animated:")]
		void SetMaximumLeftDrawerWidth (float width, bool animated);

		[Export ("setMaximumRightDrawerWidth:animated:")]
		void SetMaximumRightDrawerWidth (float width, bool animated);


		//*used internally, if you need to expose thses you need a method signature that does not contain Delegate completion

//		[Export ("bouncePreviewForDrawerSide:completion:")]
//		void BouncePreviewForDrawerSide (MMDrawerSide drawerSide, Delegate completion);
//
//		[Export ("bouncePreviewForDrawerSide:distance:completion:")]
//		void BouncePreviewForDrawerSide (MMDrawerSide drawerSide, float distance, Delegate completion);


		//*You can omit these since you will never need to use them. These are used internally by the library. 

//		[Export ("drawerVisualStateBlock"), Verify ("ObjC method massaged into setter property", "/Users/james/Development/MMDrawerController/MMDrawerController.h", Line = 383)]
//		Delegate DrawerVisualStateBlock { set; }
//
//		[Export ("gestureCompletionBlock"), Verify ("ObjC method massaged into setter property", "/Users/james/Development/MMDrawerController/MMDrawerController.h", Line = 396)]
//		Delegate GestureCompletionBlock { set; }
//
//		[Export ("gestureShouldRecognizeTouchBlock"), Verify ("ObjC method massaged into setter property", "/Users/james/Development/MMDrawerController/MMDrawerController.h", Line = 411)]
//		Delegate GestureShouldRecognizeTouchBlock { set; }
	}




	[Category, BaseType (typeof (MMDrawerController))]
	public partial interface Subclass_MMDrawerController {

		[Export ("tapGestureCallback:")]
		void TapGestureCallback (UITapGestureRecognizer tapGesture);

		[Export ("panGestureCallback:")]
		void PanGestureCallback (UIPanGestureRecognizer panGesture);

		[Export ("gestureRecognizer:shouldReceiveTouch:")]
		bool GestureRecognizer (UIGestureRecognizer gestureRecognizer, UITouch touch);

		[Export ("prepareToPresentDrawer:animated:")]
		void PrepareToPresentDrawer (MMDrawerSide drawer, bool animated);

		//*used internally, if you need to expose thses you need a method signature that does not contain Delegate completion

//		[Export ("closeDrawerAnimated:velocity:animationOptions:completion:")]
//		void CloseDrawerAnimated (bool animated, float velocity, UIViewAnimationOptions options, Delegate completion);
//
//		[Export ("openDrawerSide:animated:velocity:animationOptions:completion:")]
//		void OpenDrawerSide (MMDrawerSide drawerSide, bool animated, float velocity, UIViewAnimationOptions options, Delegate completion);

		[Export ("willRotateToInterfaceOrientation:duration:")]
		void WillRotateToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation, double duration);

		[Export ("willAnimateRotationToInterfaceOrientation:duration:")]
		void WillAnimateRotationToInterfaceOrientation (UIInterfaceOrientation toInterfaceOrientation, double duration);
	}



//	You can omit these since you will never need to use them. These are used internally by the library. 

//	[BaseType (typeof (NSObject))]
//	public partial interface MMDrawerVisualState {
//
//		[Static, Export ("slideAndScaleVisualStateBlock"), Verify ("ObjC method massaged into getter property", "/Users/james/Development/MMDrawerController/MMDrawerVisualState.h", Line = 36)]
//		MMDrawerControllerDrawerVisualStateBlock SlideAndScaleVisualStateBlock { get; }
//
//		[Static, Export ("slideVisualStateBlock"), Verify ("ObjC method massaged into getter property", "/Users/james/Development/MMDrawerController/MMDrawerVisualState.h", Line = 43)]
//		MMDrawerControllerDrawerVisualStateBlock SlideVisualStateBlock { get; }
//
//		[Static, Export ("swingingDoorVisualStateBlock"), Verify ("ObjC method massaged into getter property", "/Users/james/Development/MMDrawerController/MMDrawerVisualState.h", Line = 50)]
//		MMDrawerControllerDrawerVisualStateBlock SwingingDoorVisualStateBlock { get; }
//
//		[Static, Export ("parallaxVisualStateBlockWithParallaxFactor:")]
//		MMDrawerControllerDrawerVisualStateBlock ParallaxVisualStateBlockWithParallaxFactor (float parallaxFactor);
//	}



//	[Category, BaseType (typeof (UIViewController))]
//	public partial interface MMDrawerController_UIViewController {
//
//		[Export ("mm_drawerController", ArgumentSemantic.Retain)]
//		MMDrawerController Mm_drawerController { get; }
//
//		[Export ("mm_visibleDrawerFrame", ArgumentSemantic.Assign)]
//		RectangleF Mm_visibleDrawerFrame { get; }
//	}

}

