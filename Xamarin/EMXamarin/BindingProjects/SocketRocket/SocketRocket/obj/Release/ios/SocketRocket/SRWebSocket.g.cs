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
	[Protocol (Name = "SRWebSocket", WrapperType = typeof (SRWebSocketWrapper))]
	public interface ISRWebSocket : INativeObject, IDisposable
	{
	}
	
	public static partial class SRWebSocket_Extensions {
		[CompilerGenerated]
		public static void SetDelegateOperationQueue (this ISRWebSocket This, NSOperationQueue queue)
		{
			if (queue == null)
				throw new ArgumentNullException ("queue");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("setDelegateOperationQueue:"), queue.Handle);
		}
		
		[CompilerGenerated]
		public static void SetDelegateDispatchQueue (this ISRWebSocket This, global::CoreFoundation.DispatchQueue queue)
		{
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("setDelegateDispatchQueue:"), queue.Handle);
		}
		
		[CompilerGenerated]
		public static void ScheduleInRunLoop (this ISRWebSocket This, NSRunLoop aRunLoop, string mode)
		{
			if (aRunLoop == null)
				throw new ArgumentNullException ("aRunLoop");
			if (mode == null)
				throw new ArgumentNullException ("mode");
			var nsmode = NSString.CreateNative (mode);
			
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (This.Handle, Selector.GetHandle ("scheduleInRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			NSString.ReleaseNative (nsmode);
			
		}
		
		[CompilerGenerated]
		public static void UnscheduleFromRunLoop (this ISRWebSocket This, NSRunLoop aRunLoop, string mode)
		{
			if (aRunLoop == null)
				throw new ArgumentNullException ("aRunLoop");
			if (mode == null)
				throw new ArgumentNullException ("mode");
			var nsmode = NSString.CreateNative (mode);
			
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (This.Handle, Selector.GetHandle ("unscheduleFromRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			NSString.ReleaseNative (nsmode);
			
		}
		
		[CompilerGenerated]
		public static void Open (this ISRWebSocket This)
		{
			ApiDefinition.Messaging.void_objc_msgSend (This.Handle, Selector.GetHandle ("open"));
		}
		
		[CompilerGenerated]
		public static void Close (this ISRWebSocket This)
		{
			ApiDefinition.Messaging.void_objc_msgSend (This.Handle, Selector.GetHandle ("close"));
		}
		
		[CompilerGenerated]
		public static void CloseWithCode (this ISRWebSocket This, int code, string reason)
		{
			var nsreason = NSString.CreateNative (reason);
			
			ApiDefinition.Messaging.void_objc_msgSend_int_IntPtr (This.Handle, Selector.GetHandle ("closeWithCode:reason:"), code, nsreason);
			NSString.ReleaseNative (nsreason);
			
		}
		
		[CompilerGenerated]
		public static void Send (this ISRWebSocket This, NSObject data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("send:"), data.Handle);
		}
		
		[CompilerGenerated]
		public static void SendPing (this ISRWebSocket This, NSData data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("sendPing:"), data.Handle);
		}
		
		[CompilerGenerated]
		public static NSObject GetWeakDelegate (this ISRWebSocket This)
		{
			return Runtime.GetNSObject (ApiDefinition.Messaging.IntPtr_objc_msgSend (This.Handle, Selector.GetHandle ("delegate")));
		}
		
		[CompilerGenerated]
		public static void SetWeakDelegate (this ISRWebSocket This, NSObject value)
		{
			if (value == null)
				throw new ArgumentNullException ("value");
			ApiDefinition.Messaging.void_objc_msgSend_IntPtr (This.Handle, Selector.GetHandle ("setDelegate:"), value.Handle);
		}
		
		[CompilerGenerated]
		public static SRReadyState GetReadyState (this ISRWebSocket This)
		{
			return (SRReadyState) ApiDefinition.Messaging.int_objc_msgSend (This.Handle, Selector.GetHandle ("readyState"));
		}
		
		[CompilerGenerated]
		public static NSUrl GetUrl (this ISRWebSocket This)
		{
			return  Runtime.GetNSObject<NSUrl> (ApiDefinition.Messaging.IntPtr_objc_msgSend (This.Handle, Selector.GetHandle ("url")));
		}
		
		[CompilerGenerated]
		public static string GetProtocol (this ISRWebSocket This)
		{
			return NSString.FromHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend (This.Handle, Selector.GetHandle ("protocol")));
		}
		
	}
	
	internal sealed class SRWebSocketWrapper : BaseWrapper, ISRWebSocket {
		public SRWebSocketWrapper (IntPtr handle)
			: base (handle, false)
		{
		}
		
		[Preserve (Conditional = true)]
		public SRWebSocketWrapper (IntPtr handle, bool owns)
			: base (handle, owns)
		{
		}
		
	}
}
namespace SocketRocket {
	[Protocol]
	[Register("SRWebSocket", true)]
	public unsafe partial class SRWebSocket : NSObject, ISRWebSocket {
		
		[CompilerGenerated]
		static readonly IntPtr class_ptr = Class.GetHandle ("SRWebSocket");
		
		public override IntPtr ClassHandle { get { return class_ptr; } }
		
		[CompilerGenerated]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		[Export ("init")]
		public SRWebSocket () : base (NSObjectFlag.Empty)
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
		protected SRWebSocket (NSObjectFlag t) : base (t)
		{
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
		}

		[CompilerGenerated]
		[EditorBrowsable (EditorBrowsableState.Advanced)]
		protected internal SRWebSocket (IntPtr handle) : base (handle)
		{
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
		}

		[Export ("initWithURLRequest:protocols:")]
		[CompilerGenerated]
		public SRWebSocket (NSUrlRequest request, global::System.String[] protocols)
			: base (NSObjectFlag.Empty)
		{
			if (request == null)
				throw new ArgumentNullException ("request");
			if (protocols == null)
				throw new ArgumentNullException ("protocols");
			var nsa_protocols = NSArray.FromStrings (protocols);
			
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
			if (IsDirectBinding) {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend_IntPtr_IntPtr (this.Handle, Selector.GetHandle ("initWithURLRequest:protocols:"), request.Handle, nsa_protocols.Handle), "initWithURLRequest:protocols:");
			} else {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper_IntPtr_IntPtr (this.SuperHandle, Selector.GetHandle ("initWithURLRequest:protocols:"), request.Handle, nsa_protocols.Handle), "initWithURLRequest:protocols:");
			}
			nsa_protocols.Dispose ();
			
		}
		
		[Export ("initWithURLRequest:")]
		[CompilerGenerated]
		public SRWebSocket (NSUrlRequest request)
			: base (NSObjectFlag.Empty)
		{
			if (request == null)
				throw new ArgumentNullException ("request");
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
			if (IsDirectBinding) {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("initWithURLRequest:"), request.Handle), "initWithURLRequest:");
			} else {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("initWithURLRequest:"), request.Handle), "initWithURLRequest:");
			}
		}
		
		[Export ("initWithURL:protocols:")]
		[CompilerGenerated]
		public SRWebSocket (NSUrl url, global::System.String[] protocols)
			: base (NSObjectFlag.Empty)
		{
			if (url == null)
				throw new ArgumentNullException ("url");
			if (protocols == null)
				throw new ArgumentNullException ("protocols");
			var nsa_protocols = NSArray.FromStrings (protocols);
			
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
			if (IsDirectBinding) {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend_IntPtr_IntPtr (this.Handle, Selector.GetHandle ("initWithURL:protocols:"), url.Handle, nsa_protocols.Handle), "initWithURL:protocols:");
			} else {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper_IntPtr_IntPtr (this.SuperHandle, Selector.GetHandle ("initWithURL:protocols:"), url.Handle, nsa_protocols.Handle), "initWithURL:protocols:");
			}
			nsa_protocols.Dispose ();
			
		}
		
		[Export ("initWithURL:")]
		[CompilerGenerated]
		public SRWebSocket (NSUrl url)
			: base (NSObjectFlag.Empty)
		{
			if (url == null)
				throw new ArgumentNullException ("url");
			IsDirectBinding = GetType ().Assembly == global::ApiDefinition.Messaging.this_assembly;
			if (IsDirectBinding) {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("initWithURL:"), url.Handle), "initWithURL:");
			} else {
				InitializeHandle (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("initWithURL:"), url.Handle), "initWithURL:");
			}
		}
		
		[Export ("close")]
		[CompilerGenerated]
		public virtual void Close ()
		{
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend (this.Handle, Selector.GetHandle ("close"));
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("close"));
			}
		}
		
		[Export ("closeWithCode:reason:")]
		[CompilerGenerated]
		public virtual void CloseWithCode (int code, string reason)
		{
			var nsreason = NSString.CreateNative (reason);
			
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_int_IntPtr (this.Handle, Selector.GetHandle ("closeWithCode:reason:"), code, nsreason);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_int_IntPtr (this.SuperHandle, Selector.GetHandle ("closeWithCode:reason:"), code, nsreason);
			}
			NSString.ReleaseNative (nsreason);
			
		}
		
		[Export ("open")]
		[CompilerGenerated]
		public virtual void Open ()
		{
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend (this.Handle, Selector.GetHandle ("open"));
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("open"));
			}
		}
		
		[Export ("scheduleInRunLoop:forMode:")]
		[CompilerGenerated]
		public virtual void ScheduleInRunLoop (NSRunLoop aRunLoop, string mode)
		{
			if (aRunLoop == null)
				throw new ArgumentNullException ("aRunLoop");
			if (mode == null)
				throw new ArgumentNullException ("mode");
			var nsmode = NSString.CreateNative (mode);
			
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (this.Handle, Selector.GetHandle ("scheduleInRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr_IntPtr (this.SuperHandle, Selector.GetHandle ("scheduleInRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			}
			NSString.ReleaseNative (nsmode);
			
		}
		
		[Export ("send:")]
		[CompilerGenerated]
		public virtual void Send (NSObject data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("send:"), data.Handle);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("send:"), data.Handle);
			}
		}
		
		[Export ("sendPing:")]
		[CompilerGenerated]
		public virtual void SendPing (NSData data)
		{
			if (data == null)
				throw new ArgumentNullException ("data");
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("sendPing:"), data.Handle);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("sendPing:"), data.Handle);
			}
		}
		
		[Export ("setDelegateDispatchQueue:")]
		[CompilerGenerated]
		public virtual void SetDelegateDispatchQueue (global::CoreFoundation.DispatchQueue queue)
		{
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("setDelegateDispatchQueue:"), queue.Handle);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("setDelegateDispatchQueue:"), queue.Handle);
			}
		}
		
		[Export ("setDelegateOperationQueue:")]
		[CompilerGenerated]
		public virtual void SetDelegateOperationQueue (NSOperationQueue queue)
		{
			if (queue == null)
				throw new ArgumentNullException ("queue");
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("setDelegateOperationQueue:"), queue.Handle);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("setDelegateOperationQueue:"), queue.Handle);
			}
		}
		
		[Export ("unscheduleFromRunLoop:forMode:")]
		[CompilerGenerated]
		public virtual void UnscheduleFromRunLoop (NSRunLoop aRunLoop, string mode)
		{
			if (aRunLoop == null)
				throw new ArgumentNullException ("aRunLoop");
			if (mode == null)
				throw new ArgumentNullException ("mode");
			var nsmode = NSString.CreateNative (mode);
			
			if (IsDirectBinding) {
				ApiDefinition.Messaging.void_objc_msgSend_IntPtr_IntPtr (this.Handle, Selector.GetHandle ("unscheduleFromRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			} else {
				ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr_IntPtr (this.SuperHandle, Selector.GetHandle ("unscheduleFromRunLoop:forMode:"), aRunLoop.Handle, nsmode);
			}
			NSString.ReleaseNative (nsmode);
			
		}
		
		[CompilerGenerated]
		public SRWebSocketDelegate Delegate {
			get {
				return WeakDelegate as /**/SRWebSocketDelegate;
			}
			set {
				WeakDelegate = value;
			}
		}
		
		[CompilerGenerated]
		public virtual string Protocol {
			[Export ("protocol")]
			get {
				if (IsDirectBinding) {
					return NSString.FromHandle (ApiDefinition.Messaging.IntPtr_objc_msgSend (this.Handle, Selector.GetHandle ("protocol")));
				} else {
					return NSString.FromHandle (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("protocol")));
				}
			}
			
		}
		
		[CompilerGenerated]
		public virtual SRReadyState ReadyState {
			[Export ("readyState")]
			get {
				if (IsDirectBinding) {
					return (SRReadyState) ApiDefinition.Messaging.int_objc_msgSend (this.Handle, Selector.GetHandle ("readyState"));
				} else {
					return (SRReadyState) ApiDefinition.Messaging.int_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("readyState"));
				}
			}
			
		}
		
		[CompilerGenerated]
		object __mt_Url_var;
		[CompilerGenerated]
		public virtual NSUrl Url {
			[Export ("url")]
			get {
				NSUrl ret;
				if (IsDirectBinding) {
					ret =  Runtime.GetNSObject<NSUrl> (ApiDefinition.Messaging.IntPtr_objc_msgSend (this.Handle, Selector.GetHandle ("url")));
				} else {
					ret =  Runtime.GetNSObject<NSUrl> (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("url")));
				}
				if (!IsNewRefcountEnabled ())
					__mt_Url_var = ret;
				return ret;
			}
			
		}
		
		[CompilerGenerated]
		object __mt_WeakDelegate_var;
		[CompilerGenerated]
		public virtual NSObject WeakDelegate {
			[Export ("delegate", ArgumentSemantic.UnsafeUnretained)]
			get {
				NSObject ret;
				if (IsDirectBinding) {
					ret = Runtime.GetNSObject (ApiDefinition.Messaging.IntPtr_objc_msgSend (this.Handle, Selector.GetHandle ("delegate")));
				} else {
					ret = Runtime.GetNSObject (ApiDefinition.Messaging.IntPtr_objc_msgSendSuper (this.SuperHandle, Selector.GetHandle ("delegate")));
				}
				MarkDirty ();
				__mt_WeakDelegate_var = ret;
				return ret;
			}
			
			[Export ("setDelegate:", ArgumentSemantic.UnsafeUnretained)]
			set {
				if (IsDirectBinding) {
					ApiDefinition.Messaging.void_objc_msgSend_IntPtr (this.Handle, Selector.GetHandle ("setDelegate:"), value == null ? IntPtr.Zero : value.Handle);
				} else {
					ApiDefinition.Messaging.void_objc_msgSendSuper_IntPtr (this.SuperHandle, Selector.GetHandle ("setDelegate:"), value == null ? IntPtr.Zero : value.Handle);
				}
				MarkDirty ();
				__mt_WeakDelegate_var = value;
			}
		}
		
		//
		// Events and properties from the delegate
		//
		
		_SRWebSocketDelegate EnsureSRWebSocketDelegate ()
		{
			var del = WeakDelegate;
			if (del == null || (!(del is _SRWebSocketDelegate))){
				del = new _SRWebSocketDelegate ();
				WeakDelegate = del;
			}
			return (_SRWebSocketDelegate) del;
		}
		
		#pragma warning disable 672
		[Register]
		sealed class _SRWebSocketDelegate : SocketRocket.SRWebSocketDelegate { 
			public _SRWebSocketDelegate () { IsDirectBinding = false; }
			
			internal EventHandler<SRClosedEventArgs> closed;
			[Preserve (Conditional = true)]
			public override void Closed (SocketRocket.SRWebSocket webSocket, nint code, string reason, bool wasClean)
			{
				EventHandler<SRClosedEventArgs> handler = closed;
				if (handler != null){
					var args = new SRClosedEventArgs (code, reason, wasClean);
					handler (webSocket, args);
				}
			}
			
			internal EventHandler<SRPongEventArgs> didReceivePong;
			[Preserve (Conditional = true)]
			public override void DidReceivePong (SocketRocket.SRWebSocket webSocket, NSData pongPayload)
			{
				EventHandler<SRPongEventArgs> handler = didReceivePong;
				if (handler != null){
					var args = new SRPongEventArgs (pongPayload);
					handler (webSocket, args);
				}
			}
			
			internal EventHandler<SRErrorEventArgs> error;
			[Preserve (Conditional = true)]
			public override void Error (SocketRocket.SRWebSocket webSocket, NSError err)
			{
				EventHandler<SRErrorEventArgs> handler = error;
				if (handler != null){
					var args = new SRErrorEventArgs (err);
					handler (webSocket, args);
				}
			}
			
			internal EventHandler<SRMessageReceivedEventArgs> messageReceived;
			[Preserve (Conditional = true)]
			public override void MessageReceived (SocketRocket.SRWebSocket webSocket, NSObject message)
			{
				EventHandler<SRMessageReceivedEventArgs> handler = messageReceived;
				if (handler != null){
					var args = new SRMessageReceivedEventArgs (message);
					handler (webSocket, args);
				}
			}
			
			internal EventHandler opened;
			[Preserve (Conditional = true)]
			public override void Opened (SocketRocket.SRWebSocket webSocket)
			{
				EventHandler handler = opened;
				if (handler != null){
					handler (webSocket, EventArgs.Empty);
				}
			}
			
		}
		#pragma warning restore 672
		
		public event EventHandler<SRClosedEventArgs> Closed {
			add { EnsureSRWebSocketDelegate ().closed += value; }
			remove { EnsureSRWebSocketDelegate ().closed -= value; }
		}
		
		public event EventHandler<SRPongEventArgs> DidReceivePong {
			add { EnsureSRWebSocketDelegate ().didReceivePong += value; }
			remove { EnsureSRWebSocketDelegate ().didReceivePong -= value; }
		}
		
		public event EventHandler<SRErrorEventArgs> Error {
			add { EnsureSRWebSocketDelegate ().error += value; }
			remove { EnsureSRWebSocketDelegate ().error -= value; }
		}
		
		public event EventHandler<SRMessageReceivedEventArgs> MessageReceived {
			add { EnsureSRWebSocketDelegate ().messageReceived += value; }
			remove { EnsureSRWebSocketDelegate ().messageReceived -= value; }
		}
		
		public event EventHandler Opened {
			add { EnsureSRWebSocketDelegate ().opened += value; }
			remove { EnsureSRWebSocketDelegate ().opened -= value; }
		}
		
		[CompilerGenerated]
		protected override void Dispose (bool disposing)
		{
			base.Dispose (disposing);
			if (Handle == IntPtr.Zero) {
				__mt_Url_var = null;
				__mt_WeakDelegate_var = null;
			}
		}
	} /* class SRWebSocket */
	
	
	//
	// EventArgs classes
	//
	public partial class SRClosedEventArgs : EventArgs {
		public SRClosedEventArgs (nint code, string reason, bool wasClean)
		{
			this.Code = code;
			this.Reason = reason;
			this.WasClean = wasClean;
		}
		public nint Code { get; set; }
		public string Reason { get; set; }
		public bool WasClean { get; set; }
	}
	
	public partial class SRErrorEventArgs : EventArgs {
		public SRErrorEventArgs (NSError err)
		{
			this.Err = err;
		}
		public NSError Err { get; set; }
	}
	
	public partial class SRMessageReceivedEventArgs : EventArgs {
		public SRMessageReceivedEventArgs (NSObject message)
		{
			this.Message = message;
		}
		public NSObject Message { get; set; }
	}
	
	public partial class SRPongEventArgs : EventArgs {
		public SRPongEventArgs (NSData pongPayload)
		{
			this.PongPayload = pongPayload;
		}
		public NSData PongPayload { get; set; }
	}
	
}
