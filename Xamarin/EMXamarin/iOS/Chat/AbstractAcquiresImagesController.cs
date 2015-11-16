using System;
using System.Collections.Generic;
using System.Diagnostics;
using AssetsLibrary;
using CoreGraphics;
using em;
using Foundation;
using Media_iOS_Extension;
using MobileCoreServices;
using UIDevice_Extension;
using UIKit;

namespace iOS {
	public abstract class AbstractAcquiresImagesController : UIViewController {

		#region switch statement cases 
		const int CASE_USING_PHOTO_LIBRARY = 0;
		const int CASE_USING_CAMERA = 1;
		const int CASE_HAS_CAMERA_SEARCH = 2;
		const int CASE_HAS_CAMERA_CANCEL = 3;

		const int CASE_NO_CAMERA_SEARCH = 1;
		const int CASE_NO_CAMERA_CANCEL = 2;
		#endregion

		bool cameraAvailable;
		public bool CameraAvailable {
			get { return cameraAvailable; }
			set { cameraAvailable = value; }
		}

		string imageSearchTitle;
		protected string ImageSearchTitle {
			get { return imageSearchTitle; }
			set { imageSearchTitle = value; }
		}

		bool allowVideo;
		public bool AllowVideoOnImageSelection {
			get { return allowVideo; }
			set { allowVideo = value; }
		}

		bool allowEditing;
		protected bool AllowImageEditingOnImageSelection {
			get { return allowEditing; }
			set { allowEditing = value; }
		}

		bool allowMediaPickerController;
		protected bool AllowMediaPickerController {
			get { return allowMediaPickerController; }
			set { allowMediaPickerController = value; }
		}

		ChatMediaPickerController MediaPickerController { get; set; }

        private bool _usingSquareCropper = false;
        public bool UsingSquareCropper { get { return this._usingSquareCropper; } set { this._usingSquareCropper = value; } }

		public AbstractAcquiresImagesController () {
			this.CameraAvailable = UIImagePickerController.IsSourceTypeAvailable (UIImagePickerControllerSourceType.Camera);
			this.ImageSearchTitle = "WEB_SEARCH".t ();
			this.AllowVideoOnImageSelection = true;
			this.AllowImageEditingOnImageSelection = false;
		}

		public void StartAcquiringImage() {
			var libraryButtonText = AllowVideoOnImageSelection ? "PHOTO_AND_VIDEO_LIBRARY_BUTTON".t () : "PHOTO_LIBRARY_BUTTON".t ();

			if (UIDevice.CurrentDevice.IsIos8Later ()) {
				UIAlertController alert = UIAlertController.Create (null, null, UIAlertControllerStyle.ActionSheet);
				alert.AddAction (UIAlertAction.Create (libraryButtonText, UIAlertActionStyle.Default, obj => ShowImagePicker (UIImagePickerControllerSourceType.PhotoLibrary)));

				if (CameraAvailable)
					alert.AddAction (UIAlertAction.Create ("CAMERA_BUTTON".t (), UIAlertActionStyle.Default, obj => ShowImagePicker (UIImagePickerControllerSourceType.Camera)));

				alert.AddAction (UIAlertAction.Create (ImageSearchTitle, UIAlertActionStyle.Default, obj => ShowImageSearch ()));

				alert.AddAction (UIAlertAction.Create ("CANCEL_BUTTON".t (), UIAlertActionStyle.Cancel, obj => { }));

				this.PresentViewController (alert, true, null);
			} else {

				var actionSheet = new UIActionSheet ();
				actionSheet.AddButton (libraryButtonText);
				if (this.CameraAvailable)
					actionSheet.AddButton ("CAMERA_BUTTON".t ());
				actionSheet.AddButton (this.ImageSearchTitle);
				actionSheet.AddButton ("CANCEL_BUTTON".t ());
				if (this.CameraAvailable)
					actionSheet.CancelButtonIndex = CASE_HAS_CAMERA_CANCEL;
				else
					actionSheet.CancelButtonIndex = CASE_NO_CAMERA_CANCEL;
					
				actionSheet.Clicked += WeakDelegateProxy.CreateProxy<object,UIButtonEventArgs>( AcquireImageActionSheetClicked ).HandleEvent<object,UIButtonEventArgs>;
				actionSheet.ShowInView (this.View);

			}
		}

		public void StartAcquiringImageForChat() {
			this.MediaPickerController = new ChatMediaPickerController (this, this.AllowMediaPickerController);
			this.MediaPickerController.InitializeAndShow ();
		}

		private void AcquireImageActionSheetClicked (object sender, UIButtonEventArgs buttonEventArgs) {
			if (this.CameraAvailable) {
				switch (buttonEventArgs.ButtonIndex) {
				case CASE_USING_PHOTO_LIBRARY:
					{
						ShowImagePicker(UIImagePickerControllerSourceType.PhotoLibrary);
						break;
					}
				case CASE_USING_CAMERA:
					{
						ShowImagePicker(UIImagePickerControllerSourceType.Camera);
						break;
					}
				case CASE_HAS_CAMERA_SEARCH: 
					{
						ShowImageSearch ();
						break;
					}
				default:
					break;
				}
			} else {
				switch (buttonEventArgs.ButtonIndex) {
				case CASE_USING_PHOTO_LIBRARY:
					{
						ShowImagePicker(UIImagePickerControllerSourceType.PhotoLibrary);
						break;
					}
				case CASE_NO_CAMERA_SEARCH: 
					{
						ShowImageSearch ();
						break;
					}

				default:
					break;
				}
			}
		}

		public void ShowImagePicker(UIImagePickerControllerSourceType sourceType) {
			UIImagePickerController imagePicker = new UIImagePickerController ();
			imagePicker.SourceType = sourceType;

			if (this.AllowVideoOnImageSelection)
				imagePicker.MediaTypes = UIImagePickerController.AvailableMediaTypes (sourceType);
			else
				imagePicker.MediaTypes = new string[] { UTType.Image };

			imagePicker.VideoQuality = UIImagePickerControllerQualityType.High;
			imagePicker.FinishedPickingMedia += WeakDelegateProxy.CreateProxy<object,UIImagePickerMediaPickedEventArgs> (HandleFinishPickingMedia).HandleEvent<object,UIImagePickerMediaPickedEventArgs>;
			imagePicker.Canceled += WeakDelegateProxy.CreateProxy<object,EventArgs> (HandleMediaSelectionCancelled).HandleEvent<object,EventArgs>;

			NavigationController.PresentViewController (imagePicker, true, null);
		}

		public void ShowImageSearch () {
			ImageSearchController c = new ImageSearchController (ImageSearchCollectionViewFlowLayout.NewInstance (), ImageSearchParty.Bing, ImageSelectedCallback, this.ImageSearchSeedString);
			this.NavigationController.PresentViewController (new UINavigationController (c), true, null);
		}

		private void ImageSelectedCallback (UIImage selectedImage) {
			EMTask.DispatchMain (() => {
				if ( !AllowImageEditingOnImageSelection )
					HandleSearchImageSelected (selectedImage);
				else {
					DHBezierCropperViewController cropper = new DHBezierCropperViewController (selectedImage, this.UsingSquareCropper);
					cropper.Completion = (UIImage editedImage) => {
						if ( editedImage != null )
							HandleSearchImageSelected (editedImage);
						NavigationController.DismissViewController(false, null);
					};
					NavigationController.PresentViewController (cropper, true, null);
				}
			});
		}

		public void HandleFinishPickingMedia (object sender, UIImagePickerMediaPickedEventArgs e) {
			UIImagePickerControllerSourceType type = ((UIImagePickerController)sender).SourceType;

			// determine what was selected, video or image
			bool isImage = false;
			switch(e.Info[UIImagePickerController.MediaType].ToString()) {
			case "public.image":
				{
					Debug.WriteLine ("Image selected");
					isImage = true;
					break;
				}
			case "public.video":
				{
					Debug.WriteLine ("Video selected");
					break;
				}
			case "public.movie":
				{
					Debug.WriteLine ("Video selected");
					break;
				}
			default:
				{
					Debug.WriteLine ("Unexpected media type " + e.Info [UIImagePickerController.MediaType]);
					// dismiss the picker
					NavigationController.DismissViewController (true, null);
					return;
				}
			}

			if (!AllowImageEditingOnImageSelection || !isImage) {
				HandleImageSelected (sender, e, isImage);
			} else {
				NavigationController.DismissViewController (true, () => {
					var image = e.Info[UIImagePickerController.OriginalImage] as UIImage;
					DHBezierCropperViewController cropper = new DHBezierCropperViewController (image, this.UsingSquareCropper);
					cropper.Completion = (UIImage editedImage) => {
						if ( editedImage != null ) {
							UIImagePickerMediaPickedEventArgs spoofedArgs = new UIImagePickerMediaPickedEventArgs(new NSDictionary(UIImagePickerController.EditedImage, editedImage));
							HandleImageSelected(sender, spoofedArgs, true);
						} else {
							this.NavigationController.DismissViewController (true, null);
						}
					};

					NavigationController.PresentViewController (cropper, true, null);;
				});
			}
		}

		void HandleMediaSelectionCancelled (object sender, EventArgs e) {
			NavigationController.DismissViewController(true, null);
		}

		public UIImage ScaleImage (UIImage editedImage, nint maxSize) {
			UIImage imageToUse = null;
			if(editedImage != null) {
				if (editedImage.Size.Height != editedImage.Size.Width) {
					nfloat lengthToUse;
					if (editedImage.Size.Height < editedImage.Size.Width) {
						lengthToUse = editedImage.Size.Width;
					} else {
						lengthToUse = editedImage.Size.Height;
					}

					nfloat newHeight = lengthToUse;
					nfloat newWidth = lengthToUse;

					UIGraphics.BeginImageContextWithOptions (new CGSize (newWidth, newHeight), true, 1.0f);
					CGContext context = UIGraphics.GetCurrentContext ();
					UIGraphics.PushContext (context);

					UIColor.Black.SetFill ();
					UIGraphics.RectFill (new CGRect (0.0f, 0.0f, newWidth, newHeight));

					CGPoint origin = new CGPoint ((newWidth - editedImage.Size.Width) / 2.0f, (newHeight - editedImage.Size.Height) / 2.0f);

					editedImage.Draw (origin);

					UIGraphics.PopContext ();
					UIImage correctedImage = UIGraphics.GetImageFromCurrentImageContext ();
					UIGraphics.EndImageContext ();

					imageToUse = correctedImage;
				} else {
					imageToUse = editedImage;
				}

				imageToUse = imageToUse.ScaleImage (maxSize);
			}

			return imageToUse;
		}
			
		protected abstract void HandleImageSelected (object sender, UIImagePickerMediaPickedEventArgs e, bool isImage);
		protected abstract void HandleSearchImageSelected (UIImage originalImage);
		public abstract string ImageSearchSeedString { get; }
		public virtual void HandleBulkMedia (List<ALAsset> mediaPaths) {}
	}
}

