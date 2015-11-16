using System;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace em {
	public class BingImageSearchInput {
		BingImageSearchResult d;
		[JsonProperty(PropertyName = "d")]
		public BingImageSearchResult ImageSearchResult { 
			get { return d; }
			set { d = value; }
		}
	}

	public class BingImageMetadata {
		string uri;
		public string Uri { 
			get { return uri; }
			set { uri = value; }
		}

		string type;
		public string Type { 
			get { return type; }
			set { type = value; }
		}
	}

	public class BingImageMetadata2 {
		string type;
		public string Type { 
			get { return type; }
			set { type = value; }
		}
	}

	public class BingImageThumbnail {

		BingImageMetadata2 metaData;
		[JsonProperty(PropertyName = "__metadata")]
		public BingImageMetadata2 MetaData { 
			get { return metaData; }
			set { metaData = value; }
		}

		string mediaUrl;
		public string MediaUrl { 
			get { return mediaUrl; } 
			set { mediaUrl = value; }
		}

		string contentType;
		public string ContentType { 
			get { return contentType; }
			set { contentType = value; }
		}

		string width;
		public string Width { 
			get { return width; }
			set { width = value; }
		}

		string height;
		public string Height { 
			get { return height; }
			set { height = value; }
		}

		string fileSize;
		public string FileSize { 
			get { return fileSize; } 
			set { fileSize = value; }
		}
	}

	public class BingImageResult {
		BingImageMetadata metaData;
		[JsonProperty(PropertyName = "__metadata")]
		public BingImageMetadata MetaData { 
			get { return metaData; }
			set { metaData = value; }
		}

		string iD;
		public string ID { 
			get { return iD; }
			set { iD = value; }
		}

		string title;
		public string Title { 
			get { return title; }
			set { title = value; }
		}

		string mediaUrl;
		public string MediaUrl { 
			get { return mediaUrl; }
			set { mediaUrl = value; }
		}

		string sourceUrl;
		public string SourceUrl { 
			get { return sourceUrl; }
			set { sourceUrl = value; }
		}

		string displayUrl;
		public string DisplayUrl { 
			get { return displayUrl; }
			set { displayUrl = value; }
		}

		string width;
		public string Width { 
			get { return width; }
			set { width = value; }
		}

		string height;
		public string Height { 
			get { return height; }
			set { height = value; }
		}

		string fileSize;
		public string FileSize { 
			get { return fileSize; }
			set { fileSize = value; }
		}

		string contentType;
		public string ContentType { 
			get { return contentType; }
			set { contentType = value; }
		}

		BingImageThumbnail thumbnail;
		public BingImageThumbnail Thumbnail { 
			get { return thumbnail; }
			set { thumbnail = value; }
		}
	}

	public class BingImageSearchResult {
		List<BingImageResult> results;
		public List<BingImageResult> Results { 
			get { return results; }
			set { results = value; }
		}

		string next;
		[JsonProperty(PropertyName = "__next")]
		public string Next {
			get { return next; }
			set { next = value; }
		}
	}
}

