using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PCLWebUtility;
using System.Net;
using System.Net.Http;

namespace em {
	public abstract class AbstractImageSearcher {

		#region multiple searches

		public abstract int ExpectedNumberOfResults {
			get;
		}

		object searchLock = new object ();
		protected object SearchLock {
			get { return searchLock; }
			set { searchLock = value; }
		}

		int currentPage = 0;
		public int CurrentPage {
			get { return currentPage; }
			set { currentPage = value; }
		}

		IList<AbstractSearchImage> photos = new List<AbstractSearchImage> ();
		protected IList<AbstractSearchImage> Photos {
			get { return photos; }
			set { photos = value; }
		}
		#endregion


		EMAccount account;
		public EMAccount Account {
			get { return account; }
			set { account = value; }
		}

		public AbstractImageSearcher (EMAccount acc) {
			this.Account = acc;
		}

		public string SearchUrlForTerm (string searchTerm) {
			searchTerm = "'" + searchTerm + "'";
			searchTerm = WebUtility.UrlEncode (searchTerm); // put this in the background?
			string finalSearchTerm = ConstructedSearchTermWithTerm (searchTerm);
			return finalSearchTerm;
		}

		// Searching for a list of images with a certain term.
		public void SearchImages (string term, Action<ImageSearchResponse> finishedSearch) {
			OnBeforeSearch ();
			EMTask.DispatchBackground (() => {
				string searchURL = this.SearchUrlForTerm (term);
				this.Account.SearchForImage (searchURL, (EMHttpResponse obj) => {
					if (obj.IsSuccess) {
						string responseStr = obj.ResponseAsString;
						HandleSearchResponse (responseStr, finishedSearch);
					} else {
						HandleSearchResponse (null, finishedSearch);
					}
				});
			});
		}

		public abstract string ConstructedSearchTermWithTerm (string searchTerm);
		public abstract void HandleSearchResponse (string responseString, Action<ImageSearchResponse> finishedSearch);

		public void OnBeforeSearch () {
			lock (this.SearchLock) {
				this.Photos = new List<AbstractSearchImage> ();
			}
		}
	}
}

