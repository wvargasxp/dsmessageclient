using System;
using System.IO;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using em;
using Media_Android_Extension;
using System.Collections.Generic;

namespace Emdroid {
	enum ActivityWithResultOption {
		Camera = 1,
		VideoCamera = 2,
		PhotoLibrary = 3,
		CroppingImage = 4
	}

	public abstract class AbstractAcquiresImagesFragment : Fragment {
		static readonly string CAMERA_ACTIVITY_EXTERNAL_RESULTS_PATH = "Camera/MediaOutputFile";
		static readonly string CROPPED_IMAGE_RESULT_PATH = "CroppedImage/CroppedImageFile";

		protected Java.IO.File mediaFile;
		Java.IO.File croppedImgFile;
		private bool allowMediaPickerFragment;
		public bool AllowMediaPickerFragment {
			get { return allowMediaPickerFragment; }
			set { allowMediaPickerFragment = value; }
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);

			Java.IO.File externalDir = Activity.GetExternalFilesDir (null);
			mediaFile = new Java.IO.File (externalDir, CAMERA_ACTIVITY_EXTERNAL_RESULTS_PATH);
			croppedImgFile = new Java.IO.File (externalDir, CROPPED_IMAGE_RESULT_PATH);
		}

		protected abstract int PopupMenuInflateResource ();
		protected abstract View PopupMenuAnchorView ();
		protected abstract string ImageIntentMediaType (); //"video/*, image/*"
		protected abstract void DidAcquireMedia (string mediaType, string path);
		protected abstract bool AllowsImageCropping ();
		public abstract string ImageSearchSeedString { get; }

		public virtual void HandleBulkImages (List<ChatMediaEntry> mediaPaths) {}

        private bool _usingSquareCropper = false;
        public bool UsingSquareCropper { get { return this._usingSquareCropper; } set { this._usingSquareCropper = value; } }

		public void StartAcquiringImage () {
			MainActivity ma = Activity as MainActivity;

			PopupMenu menu = new PopupMenu (Activity, PopupMenuAnchorView ());

			// with Android 4 Inflate can be called directly on the menu
			menu.Inflate (PopupMenuInflateResource());

			menu.MenuItemClick += (s1, arg1) => {

				bool launchingExternalActivity = true;
				IMenuItem item = arg1.Item;
				switch ( item.ItemId ) {
				case Resource.Id.MediaLibrary: {
						LaunchMediaLibrary ();
					}
					break;

				case Resource.Id.Camera: {
						LaunchCamera ();
					}
					break;

				case Resource.Id.Search: 
					{
						launchingExternalActivity = false;
						LaunchImageSearch ();
						break;
					}

				case Resource.Id.VideoCamera: {
						LaunchVideoCamera ();
					}
					break;
				}

				if (ma != null && launchingExternalActivity) {
					ma.LaunchingExternalActivity ();
				}
			};

			// Android 4 now has the DismissEvent
			menu.DismissEvent += (s2, arg2) => {};

			menu.Show ();
		}

		public void LaunchMediaLibrary () {
			var imageIntent = new Intent ();
			CreateMediaFile();
			imageIntent.SetType (ImageIntentMediaType());
			imageIntent.SetAction (Intent.ActionGetContent);
			StartActivityForResult (Intent.CreateChooser (imageIntent, "SELECT_MEDIA".t ()), (int) ActivityWithResultOption.PhotoLibrary);
		}

		public void LaunchCamera () {
			var imageCaptureIntent = new Intent(MediaStore.ActionImageCapture);
			CreateMediaFile();
			Android.Net.Uri captureUri= Android.Net.Uri.FromFile( mediaFile );
			imageCaptureIntent.PutExtra(MediaStore.ExtraOutput, captureUri);
			StartActivityForResult(imageCaptureIntent, (int) ActivityWithResultOption.Camera);
		}

		public void LaunchImageSearch () {
			var fragment = ImageSearchFragment.NewInstance (ImageSearchParty.Bing, (byte[] imageInBytes) => {
				CopyFromByteArrayToTargetMediaFile (imageInBytes);
			}, this.ImageSearchSeedString);

			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.transitionTo, Resource.Animation.transitionOut, Resource.Animation.transitionTo, Resource.Animation.transitionOut)
				.Replace (Resource.Id.content_frame, fragment)
				.AddToBackStack (null)
				.Commit ();
		}

		public void LaunchVideoCamera () {
			var imageCaptureIntent = new Intent(MediaStore.ActionVideoCapture);
			CreateMediaFile();

			if (!EMApplication.Instance.IsSenseUI) {
				// https://stackoverflow.com/questions/4123505/video-capture-ignoring-extra-output-dilemma
				// For some reason, some (or all) HTC devices ignore the extra output.
				// We don't set the Extra then and use the data returned by the intent to get our media file.
				Android.Net.Uri captureUri= Android.Net.Uri.FromFile( mediaFile );
				imageCaptureIntent.PutExtra(MediaStore.ExtraOutput, captureUri);
			}

			StartActivityForResult(imageCaptureIntent, (int) ActivityWithResultOption.VideoCamera);
		}

		public void StartAcquiringImageForChat () {
			ChatMediaPickerFragment cmpf = ChatMediaPickerFragment.NewInstance (this, this.AllowMediaPickerFragment);
			Activity.FragmentManager.BeginTransaction ()
				.SetCustomAnimations (Resource.Animation.slide_in, Resource.Animation.slide_out, Resource.Animation.slide_in, Resource.Animation.slide_out)
				.Add (Resource.Id.content_frame, cmpf)
				.AddToBackStack (null)
				.Commit ();
		}


		protected void CreateMediaFile () {
			if ( !mediaFile.ParentFile.Exists() )
				mediaFile.ParentFile.Mkdirs();
			if ( mediaFile.Exists() )
				mediaFile.Delete();
			mediaFile.CreateNewFile();
		}

		protected void CreatedCroppedImageFile () {
			// Copy of CreateMediaFile() function.
			if ( !croppedImgFile.ParentFile.Exists() )
				croppedImgFile.ParentFile.Mkdirs();
			if ( croppedImgFile.Exists() )
				croppedImgFile.Delete();
			croppedImgFile.CreateNewFile();
		}

		protected void BeginStagingImageOrVideoFromIntent (Intent data) {
			string mimeType = null;
			GetMimeTypeFromIntent (data, ref mimeType);

			if (IsVideoMimeType (mimeType)) {
				// We're showing a progress spinner here because copying a video file can take a long time (many seconds).
				// The ending InProgressHelper.Hide () will be called later, after we've generated a thumbnail for it. 
				// Look in possibly StagedMediaGetAspectRatio in ChatFragment.
				InProgressHelper.Show (this);
			}

			CopyFromDataIntentToTargetMediaFileAsync (data, () => {
				if (!AllowsImageCropping () || !IsPhotoMimeType (mimeType))
					DidAcquireMedia (mimeType, mediaFile.AbsolutePath);
				else
					LaunchCropImageActivity ();
			});
		}

		protected void BeginStagingImageOrVideoFromURI (Android.Net.Uri uri) {
			Intent intent = new Intent ();
			intent.SetData (uri);
			BeginStagingImageOrVideoFromIntent (intent);
		}

		public override void OnActivityResult (int requestCode, Result resultCode, Intent data) {
			var ma = Activity as MainActivity;
			if(ma != null)
				ma.CompletedExternalActivity ();

			string mimeType = null;
			switch (requestCode) {
			case (int)ActivityWithResultOption.CroppingImage: {
					if (resultCode == Result.Ok) {
						DidAcquireMedia ("image/jpeg", croppedImgFile.AbsolutePath);
					}
						
					break;
				}

			case (int)ActivityWithResultOption.PhotoLibrary:
				if (resultCode == Result.Ok) {
					BeginStagingImageOrVideoFromIntent (data);
				}
				break;

			case (int)ActivityWithResultOption.Camera:
				if (resultCode == Result.Ok) {
					if (data != null && data.Data != null) {
						GetMimeTypeFromIntent (data, ref mimeType);
						CopyFromDataIntentToTargetMediaFileAsync (data, () => {
							if (!AllowsImageCropping ())
								DidAcquireMedia (mimeType, mediaFile.AbsolutePath);
							else
								LaunchCropImageActivity ();
						});
					}
					else if (mediaFile.Exists () && mediaFile.Length () > 0) {
						BitmapUtils.ScaleAndFixOrientationOfBitmap (mediaFile.AbsolutePath);
						if ( !AllowsImageCropping() ) {
							// The mime type here is guessed from the fact that it's coming from a camera.
							DidAcquireMedia ("image/jpeg", mediaFile.AbsolutePath);
						} else {
							LaunchCropImageActivity ();
						}
					}
				}
				break;

			case (int)ActivityWithResultOption.VideoCamera:
				if (resultCode == Result.Ok) {
					if (data != null) {
						mimeType = "video/mp4";
						// We're showing a progress spinner here because copying a video file can take a long time (many seconds).
						// The ending InProgressHelper.Hide () will be called later, after we've generated a thumbnail for it. 
						// Look in possibly StagedMediaGetAspectRatio in ChatFragment.
						InProgressHelper.Show (this);
						CopyFromDataIntentToTargetMediaFileAsync (data, () => {
							DidAcquireMedia (mimeType, mediaFile.AbsolutePath);
						});
					}
					else if (mediaFile.Exists () && mediaFile.Length () > 0) {
						// The mime type here is guessed from the fact that it's coming from a video camera.
						DidAcquireMedia ("video/mp4", mediaFile.AbsolutePath);
					}
				}
				break;
			}
		}

		private void LaunchCropImageActivity () {
			var ma = Activity as MainActivity;
			ma.LaunchingExternalActivity ();

			CreatedCroppedImageFile ();
			Android.Net.Uri croppedImgUri = Android.Net.Uri.FromFile (croppedImgFile);

			EmCropImageIntentBuilder cropImage = new EmCropImageIntentBuilder (200, 200, croppedImgUri);
			Java.IO.File file = mediaFile.AbsoluteFile;
			if (file.Exists ()) {
				cropImage.SourceImage = Android.Net.Uri.FromFile (file);
				Intent intent = cropImage.GetIntent (this.Activity);
				if (!this.UsingSquareCropper) {
					intent.PutExtra ("circleCrop", true);
				}
				StartActivityForResult (intent, (int)ActivityWithResultOption.CroppingImage);
			}	
		}

		protected void CopyFromByteArrayToTargetMediaFile (byte[] imageInBytes) {
			CreateMediaFile ();
			EMApplication.Instance.appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (mediaFile.AbsolutePath, imageInBytes, (double e) => {
				// todo, we're using 1 as success, revisit to make it semantically clear
				if ((int)e == 1)  {
					EMTask.Dispatch (() => {
						BitmapUtils.ScaleAndFixOrientationOfBitmap (mediaFile.AbsolutePath);

						if (!AllowsImageCropping ())
							DidAcquireMedia ("image/jpeg", mediaFile.AbsolutePath);
						else 
							LaunchCropImageActivity ();
					});
				}
			});
		}

		protected void CopyFromDataIntentToTargetMediaFileAsync(Intent data, Action completed) {
			// content uri
			Android.Net.Uri uri = data.Data;
			string path = mediaFile.AbsolutePath;

			if (uri.Path.Equals (path)) {
				if ( completed != null )
					completed ();
				return;
			}

			ContentResolver resolver = Activity.ContentResolver;

			EMTask.DispatchBackground( () => {
				try {
					using (Stream s = uri.Scheme.StartsWith ("content") ? resolver.OpenInputStream (uri) : new FileStream (uri.Path, FileMode.Open)) {
						EMApplication.Instance.appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (path, s, null);
						BitmapUtils.ScaleAndFixOrientationOfBitmap (path);
					}
				}
				catch (Exception e) {
					System.Diagnostics.Debug.WriteLine(string.Format("Failed to copy bytes out of data intent to file: {0}\n{1}", e.Message, e.StackTrace));
				}
				finally {
					if ( completed != null ) {
						EMTask.DispatchMain( completed );
					}
				}
			});
		}

		private void GetMimeTypeFromIntent (Intent data, ref string mimeType) {
			if (data == null) return;

			Android.Net.Uri uri = data.Data;

			ContentResolver resolver = Activity.ContentResolver;
			string resolveMimeType = resolver.GetType (uri);

			if (resolveMimeType != null) {
				mimeType = resolveMimeType;
			}

			if (mimeType == null) {
				// If mimeType wasn't resolved, we try to resolve it with another.
				// This case would resolve a mimetype from dropbox.
				string extension = MimeTypeMap.GetFileExtensionFromUrl (uri.ToString ());
				MimeTypeMap mime = MimeTypeMap.Singleton;
				mimeType = mime.GetMimeTypeFromExtension (extension);
			}
		}

		protected void GetMimeTypeFromURI (Android.Net.Uri uri, ref string mimeType) {
			Intent intent = new Intent ();
			intent.SetData (uri);
			GetMimeTypeFromIntent (intent, ref mimeType);
		}

		protected bool IsPhotoMimeType(string mimeType) {
			return mimeType != null &&  mimeType.StartsWith ("image");
		}

		protected bool IsVideoMimeType (string mimeType) {
			return mimeType != null && mimeType.StartsWith ("video");
		}

		protected void MoveToPathAsync(string destination, Action completed) {
			EMTask.DispatchBackground( () => {
				try {
					if (!mediaFile.RenameTo (new Java.IO.File (destination))) {
						// can't move might happen if different storage devices
						EMApplication.Instance.appModel.platformFactory.GetFileSystemManager ().MoveFileAtPath (mediaFile.AbsolutePath, destination);
						mediaFile.Delete ();
					}
				} catch (Exception e) {
					System.Diagnostics.Debug.WriteLine(string.Format("Failed to copy move media into staging directory: {0}\n{1}", e.Message, e.StackTrace));
					return;
				}
				finally {
					if ( completed != null )
						EMTask.DispatchMain( completed );
				}
			});
		}
	}
}