﻿using System;
using System.Drawing;

using ObjCRuntime;
using Foundation;
using UIKit;
using CoreFoundation;

namespace SocketRocket
{
	[BaseType(typeof(NSObject), Delegates = new string[] { "WeakDelegate" }, Events = new Type[] { typeof(SRWebSocketDelegate) })]
	[Protocol]
	public partial interface SRWebSocket {
		[Export("delegate", ArgumentSemantic.Assign)]
		[NullAllowed]
		NSObject WeakDelegate { get; set; }

		[Wrap("WeakDelegate")]
		[NullAllowed]
		SRWebSocketDelegate Delegate { get; set; }

		[Export("readyState")]
		SRReadyState ReadyState { get; }

		[Export("url")]
		NSUrl Url { get; }

		[Export("protocol")]
		string Protocol { get; }

		[Export("initWithURLRequest:protocols:")]
		IntPtr Constructor(NSUrlRequest request, string[] protocols);

		[Export("initWithURLRequest:")]
		IntPtr Constructor(NSUrlRequest request);

		[Export("initWithURL:protocols:")]
		IntPtr Constructor(NSUrl url, string[] protocols);

		[Export("initWithURL:")]
		IntPtr Constructor(NSUrl url);

		[Export("setDelegateOperationQueue:")]
		void SetDelegateOperationQueue(NSOperationQueue queue);

		[Export("setDelegateDispatchQueue:")]
		void SetDelegateDispatchQueue(DispatchQueue queue);

		[Export("scheduleInRunLoop:forMode:")]
		void ScheduleInRunLoop(NSRunLoop aRunLoop, string mode);

		[Export("unscheduleFromRunLoop:forMode:")]
		void UnscheduleFromRunLoop(NSRunLoop aRunLoop, string mode);

		[Export("open")]
		void Open();

		[Export("close")]
		void Close();

		[Export("closeWithCode:reason:")]
		void CloseWithCode(int code, [NullAllowed] string reason);

		[Export("send:")]
		void Send(NSObject data);

		[Export("sendPing:")]
		void SendPing(NSData data);
	}

	[BaseType(typeof(NSObject)), Model, Protocol]
	public partial interface SRWebSocketDelegate {
		[Export("webSocket:didReceiveMessage:")]
		[EventArgs("SRMessageReceived")]
		void MessageReceived(SRWebSocket webSocket, NSObject message);

		[Export("webSocketDidOpen:")]
		[EventArgs("SROpen")]
		void Opened(SRWebSocket webSocket);

		[Export("webSocket:didFailWithError:")]
		[EventArgs("SRError")]
		void Error(SRWebSocket webSocket, NSError err);

		[Export("webSocket:didCloseWithCode:reason:wasClean:")]
		[EventArgs("SRClosed")]
		void Closed(SRWebSocket webSocket, nint code, string reason, bool wasClean);

		[Export("webSocket:didReceivePong:")]
		[EventArgs("SRPong")]
		void DidReceivePong(SRWebSocket webSocket, NSData pongPayload);
	}

	/*[Category, BaseType (typeof (NSUrlRequest))]
	public partial interface CertificateAdditions_NSURLRequest {
		
		[Export ("SR_SSLPinnedCertificates")]
		NSArray SR_SSLPinnedCertificates { get; }
	}*/
	/*[Category, BaseType (typeof (NSMutableUrRequest))]
	public partial interface CertificateAdditions_NSMutableURLRequest {

		[Export ("SR_SSLPinnedCertificates")]
		NSArray SR_SSLPinnedCertificates { get; set; }
	}*/
	/*[Category, BaseType (typeof (NSRunLoop))]
	public partial interface SRWebSocket_NSRunLoop {

		[Static, Export ("SR_networkRunLoop")]
		NSRunLoop SR_networkRunLoop ();
	}*/
}

