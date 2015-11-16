using System;
using System.Drawing;
using Foundation;
using UIKit;
using ObjCRuntime;

namespace AFNetworkingLibrary {

    public delegate void AFLibraryResponseCallback (AFResponse response);

    [BaseType(typeof(NSObject))]
	public partial interface AFLibrary {
		[Export ("sendRequestWithAddress:json:httpMethod:contentType:completion:")]
		void SendRequest (
                string address, 
                [NullAllowed] string json, 
                string httpMethod, 
                [NullAllowed] string contentType, 
                AFLibraryResponseCallback callback);

		[Export ("sendRequestWithAddress:json:httpMethod:contentType:timeout:completion:")]
		void SendRequest (
                string address, 
                [NullAllowed] string json, 
                string httpMethod, 
                [NullAllowed] string contentType, 
                float timeout,
                AFLibraryResponseCallback callback);

        [Export ("sendMediaRequestForMedia:")]
        void SendMediaRequest (DownloadRequestHelper helper);

        [Export ("sendUploadMediaRequest:")]
        void SendUploadMediaRequest (UploadRequestHelper helper);

        [Export ("sendImageSearchRequest:apiKey:completion:")]
        void SendImageSearchRequest (string address, [NullAllowed] string key, AFLibraryResponseCallback callback);
	}

    [BaseType(typeof(NSObject))]
    public partial interface AFResponse {
        [Export("success")]
        bool Success { get; [NullAllowed] set; }
        
        [Export("headers")]
        NSDictionary Headers { get; [NullAllowed] set; }

        [Export("responseString")]
        string Response { get; [NullAllowed] set; }

        [Export("statusCode")]
        int StatusCode { get; set; }

        [Export("error")]
        NSError Error { get; [NullAllowed] set; }

        [Export("data")]
        NSData Data { get; [NullAllowed] set; }
    }

    [BaseType (typeof (NSObject))]
    [Model, Protocol]
    public interface ProgressHelper {
        // Use [Abstract] when the method is defined in the @required section
        // of the protocol definition in Objective-C

        [Abstract]
        [Export ("mediaAddress")]
        string MediaAddress { get; }

        [Abstract]
        [Export ("begin")]
        void Begin ();

        [Abstract]
        [Export ("progress:")]
        void HasProgress (NSNumber progress);
    }

    [BaseType (typeof (ProgressHelper))]
    [Model, Protocol]
    public interface DownloadRequestHelper {
        // Use [Abstract] when the method is defined in the @required section
        // of the protocol definition in Objective-C
        [Abstract]
        [Export ("end:withResponse:atFilePath:")]
        void End (NSError error, NSUrlResponse response, NSUrl filePath);
    }

    [BaseType (typeof (ProgressHelper))]
    [Model, Protocol]
    public interface UploadRequestHelper {
        // Use [Abstract] when the method is defined in the @required section
        // of the protocol definition in Objective-C
        [Abstract]
        [Export ("end:withResponse:responseObject:")]
        void End (NSError error, NSUrlResponse response, NSObject responseObject);

        [Abstract]
        [Export ("endWithError:")]
        void EndWithError (NSError error);

        [Abstract]
        [Export ("names")]
        NSMutableArray Names { get; }

        [Abstract]
        [Export ("filenames")]
        NSMutableArray FileNames { get; }

        [Abstract]
        [Export ("filepaths")]
        NSMutableArray FilePaths { get; }

        [Abstract]
        [Export ("mimeTypes")]
        NSMutableArray MimeTypes { get; }

    }
}
