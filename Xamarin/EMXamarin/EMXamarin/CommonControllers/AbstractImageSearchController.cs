using System.Collections.Generic;

namespace em {
	public abstract class AbstractImageSearchController {

		readonly ApplicationModel appModel;

		ImageSearchParty type;
		public ImageSearchParty ImageSearchType {
			get { return type; }
			set { type = value; }
		}

		AbstractImageSearcher imageSearcher;
		public AbstractImageSearcher ImageSearcher {
			get { return imageSearcher; }
			set { imageSearcher = value; }
		}

		IList<AbstractSearchImage> searchImages;
		public IList<AbstractSearchImage> SearchImages {
			get { 
				if (searchImages == null) {
					searchImages = new List<AbstractSearchImage> ();
				}

				return searchImages; 
			}
			set { searchImages = value; }
		}

		static IList<AbstractSearchImage> cachedSearchImages;
		public static IList<AbstractSearchImage> CachedImagesFromSearch {
			get { return cachedSearchImages; }
			set { cachedSearchImages = value; }
		}

		EMAccount account;

		string beginningQueryString = "";
		public string BeginningQueryString {
			get { return beginningQueryString; }
			set { beginningQueryString = value; }
		}

		static string lastQueryString = "";
		public static string LastQueryString {
			get { return lastQueryString; }
			set { lastQueryString = value; }
		}

 		protected AbstractImageSearchController (ApplicationModel am, EMAccount ac, ImageSearchParty thirdParty, string seedString) {
			appModel = am;
			account = ac;
			this.SearchImages = new List<AbstractSearchImage> ();
			this.ImageSearchType = thirdParty;

			if (string.IsNullOrWhiteSpace (seedString)) {
				seedString = appModel.platformFactory.GetTranslation ("RANDOM_SEARCH");
			}
			
			this.BeginningQueryString = seedString;
		}

		public void InitializeAndSearch () {
			switch (this.ImageSearchType) {
			case ImageSearchParty.Flickr:
				{
					this.ImageSearcher = new FlickrSearch (account);
					break;
				}
			case ImageSearchParty.Google:
				{
					this.ImageSearcher = new GoogleSearch (account);
					break;
				}
			case ImageSearchParty.Bing:
				{
					this.ImageSearcher = new BingSearch (account);
					break;
				}
			default:
				break;
			}

			if (!string.IsNullOrWhiteSpace( AbstractImageSearchController.LastQueryString)) {
				this.SearchImages = AbstractImageSearchController.CachedImagesFromSearch;
				ReloadUI ();
			} else {
				SearchForImagesWithTerm (this.BeginningQueryString);
			}
		}

		public void HandleImageSearchResponse (string term, ImageSearchResponse imageSearchResponse) {
			if (imageSearchResponse.Success) {
				this.SearchImages = imageSearchResponse.SearchImages;

				if (this.SearchImages.Count > 0) {
					AbstractImageSearchController.LastQueryString = term;
				} else {
					// display error related to no images found
					AbstractImageSearchController.LastQueryString = string.Empty;
					DisplayError (this.GenericNoImageError);
				}
			} else {
				// display error related to http
				this.SearchImages = new List<AbstractSearchImage> ();
				AbstractImageSearchController.LastQueryString = string.Empty;
				DisplayError (this.GenericHttpError);
			}

			AbstractImageSearchController.CachedImagesFromSearch = this.SearchImages;
			ReloadUI ();
		}

		public void SearchForImagesWithTerm (string term) {
			PauseUI ();
			this.ImageSearcher.SearchImages (term, (ImageSearchResponse imageSearchResponse) => {
				ResumeUI ();
				HandleImageSearchResponse (term, imageSearchResponse);
			});
		}

		public abstract void PauseUI ();
		public abstract void ResumeUI ();
		public abstract void ReloadUI ();
		public abstract void DisplayError (string errorMessage);

		public string GenericNoImageError {
			get { return appModel.platformFactory.GetTranslation ("NO_IMAGES_FOUND"); }
		}

		public string GenericHttpError {
			get { return appModel.platformFactory.GetTranslation ("IMAGE_SEARCH_ERROR"); }
		}
	}
}