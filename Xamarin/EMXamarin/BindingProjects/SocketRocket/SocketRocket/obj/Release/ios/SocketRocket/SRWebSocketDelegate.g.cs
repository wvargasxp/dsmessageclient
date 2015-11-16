//
// Auto-generated from generator.cs, do not edit
//
// We keep references to objects, so warning 414 is expected

#pragma warning disable 414

using System;
using System.Drawing;
using System.Diagnostics;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using UIKit;
using GLKit;
using MapKit;
using Security;
using SceneKit;
using CoreVideo;
using CoreMedia;
using QuickLook;
using Foundation;
using CoreMotion;
using ObjCRuntime;
using AddressBook;
using CoreGraphics;
using CoreLocation;
using NewsstandKit;
using AVFoundation;
using CoreAnimation;
using CoreFoundation;

namespace SocketRocket {
	[Protocol (Name = "SRWebSocketDelegate", WrapperType = typeof (SRWebSocketDelegateWrapper))]
	public interface ISRWebSocketDelegate : INativeObject, IDisposable
	{
	}
	
	public static partial class SRWebSocketDelegate_Extensions {
		[CompilerGenerated]
		public static void MessageReceived (this ISRWebSocketDelegate This, SRWebSocket webSocket, NSObject message)
		{
			if (webSocket == null)
				throw new ArgumentNullException ("webSocket");
			if (message == null)
				throw new ArgumentNullException ("message");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (This.Handle, Selector.GetHandle ("webSocket:didReceiveMessage:"), webSocket.Handle, message.Handle);
		}
		
		[CompilerGenerated]
		public static void Opened (this ISRWebSocketDelegate This, SRWebSocket webSocket)
		{
			if (webSocket == null)
				throw new ArgumentNullException ("webSocket");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("webSocketDidOpen:"), webSocket.Handle);
		}
		
		[CompilerGenerated]
		public static void Error (this ISRWebSocketDelegate This, SRWebSocket webSocket, NSError err)
		{
			if (webSocket == null)
				throw new ArgumentNullException ("webSocket");
			if (err == null)
				throw new ArgumentNullException ("err");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (This.Handle, Selector.GetHandle ("webSocket:didFailWithError:"), webSocket.Handle, err.Handle);
		}
		
		[CompilerGenerated]
		public static void Closed (this ISRWebSocketDelegate This, SRWebSocket webSocket, global::System.nint code, string reason, bool wasClean)
		{
			if (webSocket == null)
				throw new ArgumentNullException ("webSocket");
			if (reason == null)
				throw new ArgumentNullException ("reason");
			var nsreason = NSString.CreateNative (reason);
			
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_nint_IntPtr_bool (This.Handle, Selector.GetHandle ("webSocket:didCloseWithCode:reason:wasClean:"), webSocket.Handle, code, nsreason, wasClean);
			NSString.ReleaseNative (nsreason);
			
		}
		
		[CompilerGenerated]
		public static void DidReceivePong (this ISRWebSocketDelegate This, SRWebSocket webSocket, NSData pongPayload)
		{
			if (webSocket == null)
				throw new ArgumentNullException ("webSocket");
			if (pongPayload == null)
				throw new ArgumentNullException ("pongPayload");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (This.Handle, Selector.GetHandle ("webSocket:didReceivePong:"), webSocket.Handle, pongPayload.Handle);
		}
		
	}
	
	internal sealed class SRWebSocketDelegateWrapper : BaseWrapper, ISRWebSocketDelegate {
		public SRWebSocketDelegateWrapper (IntPtr handle)
			: base (handle, false)
		{
		}
		
		[Preserve (Conditional = true)]
		public SRWebSocketDelegateWrapper (IntPtr handle, bool owns)
			: base (handle, owns)
		{
		}
		
	}
}
namespace SocketRocket {
	[Protocol]
	[Register("SRWebSocketDelegate", true)]
	[Model]
	public unsafe partial class SRWebSocketDelegate : NSObject, ISRWebSocketDelegate {
		
		[CompilerGenerated]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		[Export ("init")]
		public SRWebSocketDelegate () : base (NSObjectFlag.Empty)
		{
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
			if (IsDirectBinding) {
				InitializeHandle (global::ApiDefinition.Messaging.IntPtr_objc_msgSend (this.Handle, global::ObjCRuntime.Selector.GetHandle ("init")), "init");
			} else {
				InitializeHandle (global::ApiDefinition.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, global::ObjCRuntime.Selector.GetHandle ("init")), "init");
			}
		}

		[CompilerGenerated]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		protected SRWebSocketDelegate (NSObjectFlag t) : base (t)
		{
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
		}

		[CompilerGenerated]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		protected internal SRWebSocketDelegate (IntPtr handle) : base (handle)
		{
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
		}

		[Export ("webSocket:didCloseWithCode:reason:wasClean:")]
		[CompilerGenerated]
		public virtual void Closed (SRWebSocket webSocket, global::System.nint code, string reason, bool wasClean)
		{
			throw new You_Should_Not_Call_base_In_This_Method ();
		}
		
		[Export ("webSocket:didReceivePong:")]
		[CompilerGenerated]
		public virtual void DidReceivePong (SRWebSocket webSocket, NSData pongPayload)
		{
			throw new You_Should_Not_Call_base_In_This_Method ();
		}
		
		[Export ("webSocket:didFailWithError:")]
		[CompilerGenerated]
		public virtual void Error (SRWebSocket webSocket, NSError err)
		{
			throw new You_Should_Not_Call_base_In_This_Method ();
		}
		
		[Export ("webSocket:didReceiveMessage:")]
		[CompilerGenerated]
		public virtual void MessageReceived (SRWebSocket webSocket, NSObject message)
		{
			throw new You_Should_Not_Call_base_In_This_Method ();
		}
		
		[Export ("webSocketDidOpen:")]
		[CompilerGenerated]
		public virtual void Opened (SRWebSocket webSocket)
		{
			throw new You_Should_Not_Call_base_In_This_Method ();
		}
		
	} /* class SRWebSocketDelegate */
}
