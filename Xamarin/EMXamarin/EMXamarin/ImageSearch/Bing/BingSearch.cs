using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using PCLWebUtility;

namespace em {
	public class BingSearch : AbstractImageSearcher {
		public readonly static string API_KEY = "NR5I80O0ZUuVSIXsCUVZphIVYtDk5Q0y7HtxTvTrjMQ";

		public override int ExpectedNumberOfResults {
			get { return 25; }
		}

		public BingSearch (EMAccount acc) : base (acc) {}

		public override string ConstructedSearchTermWithTerm (string searchTerm) {
			string s = string.Format ("https://api.datamarket.azure.com/Bing/Search/v1/Image?Query={0}&$skip={1}&$format=json", searchTerm, this.CurrentPage * this.ExpectedNumberOfResults);
			return s;
		}

		public override void HandleSearchResponse (string responseString, Action<ImageSearchResponse> finishedSearch) {
			lock (this.SearchLock) {
				try {
					if (string.IsNullOrWhiteSpace (responseString)) {
						finishedSearch (new ImageSearchResponse (null, false));
						return;
					}

					BingImageSearchInput searchResults = JsonConvert.DeserializeObject<BingImageSearchInput> (responseString);
					if (searchResults.ImageSearchResult == null) {
						finishedSearch (new ImageSearchResponse (this.Photos, true));
						return;
					}

					List<BingImageResult> imageItems = searchResults.ImageSearchResult.Results;
					if (imageItems == null || imageItems.Count == 0) {
						finishedSearch (new ImageSearchResponse (this.Photos, true));
						return;
					}

					foreach (BingImageResult imageItem in imageItems) {
						BingSearchImagePhoto photo = new BingSearchImagePhoto (imageItem, this.Account);
						this.Photos.Add (photo);
					}

					finishedSearch (new ImageSearchResponse (this.Photos, true));
					return;

				} catch (Exception e) {
					Debug.WriteLine ("Bing:HandleSearchResponse: " + e);
					finishedSearch (new ImageSearchResponse (null, false));
				}
			}
		}


	}
}

