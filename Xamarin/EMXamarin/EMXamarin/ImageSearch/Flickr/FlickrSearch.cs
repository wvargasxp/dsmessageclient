using System;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using System.Collections.Generic;
using PCLWebUtility;
using System.Diagnostics;

namespace em {
	public class FlickrSearch : AbstractImageSearcher {
		readonly static string API_KEY = "01cc8ce5ad6934bd517f52a44826d46d";
		readonly static string SECRET_KEY = "993174272de7d438"; // don't think we need this, it's probably for personal yahoo account

		public override int ExpectedNumberOfResults {
			get {
				return 100;
			}
		}

		public FlickrSearch (EMAccount acc) : base (acc) {}

		public static string FlickrPhotoURLForFlickrPhoto (FlickrPhoto photo, string size) {
			if (string.IsNullOrWhiteSpace (size))
				size = "m";
			string f = string.Format ("https://farm{0}.staticflickr.com/{1}/{2}_{3}_{4}.jpg", photo.Farm, photo.Server, photo.PhotoID, photo.Secret, size);
			return f;
		}

		public override string ConstructedSearchTermWithTerm (string searchTerm) {
			string s = string.Format ("https://api.flickr.com/services/rest/?method=flickr.photos.search&api_key={0}&text={1}&per_page=120&format=json&nojsoncallback=1", API_KEY, searchTerm);
			return s;
		}

		public override void HandleSearchResponse (string responseString, Action<ImageSearchResponse> finishedSearch) {
			try {
				if (string.IsNullOrWhiteSpace (responseString))
					finishedSearch (new ImageSearchResponse (null, false));

				FlickrInput searchResults = JsonConvert.DeserializeObject<FlickrInput> (responseString);
				string status = searchResults.Stat;
				if (status.Equals ("ok")) {
					IList<AbstractSearchImage> flickrPhotos = new List<AbstractSearchImage> ();
					FlickrPhotosInput photoInput = searchResults.PhotoInput;
					IList<FlickrPhotoInput> photos = photoInput.Photos;

					foreach (FlickrPhotoInput flickrPhotoInput in photos) {
						FlickrPhoto photo = new FlickrPhoto (flickrPhotoInput, this.Account);
						flickrPhotos.Add (photo);
					}
					this.Photos = flickrPhotos;
				} 

				finishedSearch (new ImageSearchResponse (this.Photos, true));
			} catch (Exception e) {
				Debug.WriteLine ("SearchFlickrWithTerm: " + e);
				finishedSearch (new ImageSearchResponse (null, false));
			}
		}
	}
}



