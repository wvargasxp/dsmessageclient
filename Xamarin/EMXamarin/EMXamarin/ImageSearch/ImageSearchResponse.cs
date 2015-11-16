using System;
using System.Collections.Generic;

namespace em {
	public class ImageSearchResponse {
		readonly IList<AbstractSearchImage> searchImages;
		readonly bool success;
		public bool Success {
			get { return success; }
		}

		public IList<AbstractSearchImage> SearchImages {
			get { return searchImages; }
		}

		public ImageSearchResponse (IList<AbstractSearchImage> imgs, bool succes) {
			searchImages = imgs;
			success = succes;
		}
	}
}

