using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace em {
	public class GoogleSearch : AbstractImageSearcher {
		readonly static string API_KEY = "AIzaSyCGzRdOvYd7QpHeaYOiego0rWvksX-6Jzc"; // only for iOS // todo create one for android
		readonly static string SEARCH_ENGINE_ID = "010167230144641736844:wofsc-dufkw";

		public override int ExpectedNumberOfResults {
			get {
				return 10;
			}
		}

		public GoogleSearch (EMAccount acc) : base (acc) {}

		public override string ConstructedSearchTermWithTerm (string searchTerm) {
			string s = string.Format ("https://www.googleapis.com/customsearch/v1?key={0}&cx={1}&q={2}&searchType=image&start={3}", API_KEY, SEARCH_ENGINE_ID, searchTerm, this.CurrentPage * this.ExpectedNumberOfResults);
			return s;
		}

		public override void HandleSearchResponse (string responseString, Action<ImageSearchResponse> finishedSearch) {
			lock (this.SearchLock) {
				try {
					if (string.IsNullOrWhiteSpace (responseString)) {
						finishedSearch (new ImageSearchResponse (null, false));
						return;
					}

					GoogleImageSearchInput searchResults = JsonConvert.DeserializeObject<GoogleImageSearchInput> (responseString);

					List<GoogleImageItem> imageItems = searchResults.ImageItems;
					if (imageItems == null || imageItems.Count == 0) {
						finishedSearch (new ImageSearchResponse (this.Photos, true));
					}

					foreach (GoogleImageItem imageItem in imageItems) {
						GoogleSearchImagePhoto photo = new GoogleSearchImagePhoto (imageItem, this.Account);
						this.Photos.Add (photo);
					}

					finishedSearch (new ImageSearchResponse (this.Photos, true));

				} catch (Exception e) {
					Debug.WriteLine ("SearchFlickrWithTerm: " + e);
					finishedSearch (new ImageSearchResponse (null, false));
				}
			}
		}
			
	}
}

