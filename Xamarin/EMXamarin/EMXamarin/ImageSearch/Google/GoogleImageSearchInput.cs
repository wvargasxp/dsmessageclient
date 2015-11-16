using System;
using System.Collections.Generic;
using Newtonsoft.Json;

//
// NOTE: Don't swap to Auto-Properties, otherwise the json conversion will break.
//
//
namespace em {
	public class GoogleImageSearchInput {
		string kind;
		public string Kind { 
			get { return kind; } 
			set { kind = value; } 
		}

		GoogleImageSearchUrl url;
		public GoogleImageSearchUrl Url { 
			get { return url; } 
			set { url = value; } 
		}

		Queries queries;
		public Queries Queries { 
			get { return queries; } 
			set { queries = value; }
		}

		GoogleImageSearchContext context;
		public GoogleImageSearchContext Context { 
			get { return context; } 
			set { context = value; }
		}

		SearchInformation searchInformation;
		public SearchInformation SearchInformation { 
			get { return searchInformation; } 
			set { searchInformation = value; } 
		}

		List<GoogleImageItem> items;
		[JsonProperty(PropertyName = "items")]
		public List<GoogleImageItem> ImageItems { 
			get { return items; }
			set { items = value; }
		}

		public GoogleImageSearchInput () {}
	}

	public class GoogleImageSearchUrl {
		string type;
		public string Type { 
			get { return type; }
			set { type = value; }
		}
		string template;
		public string Template { 
			get { return template; }
			set { template = value; }
		}
	}

	public class GoogleNextPage {
		string title;
		public string Title { 
			get { return title; }
			set { title = value; }
		}

		string totalResults;
		public string TotalResults { 
			get { return totalResults; }
			set { totalResults = value; }
		}

		string searchTerms;
		public string SearchTerms { 
			get { return searchTerms; }
			set { searchTerms = value; }
		}

		int count;
		public int Count { 
			get { return count; }
			set { count = value; }
		}

		int startIndex;
		public int StartIndex { 
			get { return startIndex; }
			set { startIndex = value; }
		}

		string inputEncoding;
		public string InputEncoding { 
			get { return inputEncoding; }
			set { inputEncoding = value; }
		}

		string outputEncoding;
		public string OutputEncoding { 
			get { return outputEncoding; }
			set { outputEncoding = value; }
		}

		string safe;
		public string Safe { 
			get { return safe; }
			set { safe = value; }
		}

		string cx;
		public string Cx { 
			get { return cx; }
			set { cx = value; }
		}

		string searchType;
		public string SearchType { 
			get { return searchType; }
			set { searchType = value; }
		}
	}

	public class GoogleImageRequest {
		string title;
		public string Title { 
			get { return title; } 
			set { title = value; } 
		}

		string totalResults;
		public string TotalResults { 
			get { return totalResults; }
			set { totalResults = value; }
		}

		string searchTerms;
		public string SearchTerms { 
			get { return searchTerms; }
			set { searchTerms = value; }
		}

		int count;
		public int Count { 
			get { return count; } 
			set { count = value; }
		}

		int startIndex;
		public int StartIndex { 
			get { return startIndex; }
			set { startIndex = value; }
		}

		string inputEncoding;
		public string InputEncoding { 
			get { return inputEncoding; }
			set { inputEncoding = value; }
		}

		string outputEncoding;
		public string OutputEncoding { 
			get { return outputEncoding; }
			set { outputEncoding = value; }
		}

		string safe;
		public string Safe { 
			get { return safe; } 
			set { safe = value; }
		}

		string cx;
		public string Cx { 
			get { return cx; }
			set { cx = value; }
		}

		string searchType;
		public string SearchType { 
			get { return searchType; }
			set { searchType = value; }
		}
	}
		
	public class Queries {
		List<GoogleNextPage> nextPage;
		public List<GoogleNextPage> NextPage { 
			get { return nextPage; }
			set { nextPage = value; }
		}

		List<GoogleImageRequest> request;
		public List<GoogleImageRequest> Request { 
			get { return request; }
			set { request = value; }
		}
	}

	public class GoogleImageSearchContext {
		string title;
		public string Title { 
			get { return title; }
			set { title = value; }
		}
	}

	public class SearchInformation {
		double searchTime;
		public double SearchTime { 
			get { return searchTime; } 
			set { searchTime = value; } 
		}

		string formattedSearchTime;
		public string FormattedSearchTime { 
			get { return formattedSearchTime; } 
			set { formattedSearchTime = value; } 
		}

		string totalResults;
		public string TotalResults { 
			get { return totalResults; } 
			set { totalResults = value; } 
		}

		string formattedTotalResults;
		public string FormattedTotalResults { 
			get { return formattedTotalResults; } 
			set { formattedTotalResults = value; } 
		}
	}

	public class GoogleImage {
		string contextLink;
		public string ContextLink { 
			get { return contextLink; } 
			set { contextLink = value; } 
		}

		int height;
		public int Height { 
			get { return height; } 
			set { height = value; } 
		}

		int width;
		public int Width { 
			get { return width; } 
			set { width = value; } 
		}

		int byteSize;
		public int ByteSize { 
			get { return byteSize; } 
			set { byteSize = value; } 
		}

		string thumbnailLink;
		public string ThumbnailLink { 
			get { return thumbnailLink; } 
			set { thumbnailLink = value; } 
		}

		int thumbnailHeight;
		public int ThumbnailHeight { 
			get { return thumbnailHeight; } 
			set { thumbnailHeight = value; } 
		}

		int thumbnailWidth;
		public int ThumbnailWidth { 
			get { return thumbnailWidth; } 
			set { thumbnailWidth = value; } 
		}
	}
		
	public class GoogleImageItem {
		string kind;
		public string Kind { 
			get { return kind; } 
			set { kind = value; }
		}

		string title;
		public string Title { 
			get { return title; } 
			set { title = value; }
		}

		string htmlTitle;
		public string HtmlTitle { 
			get { return htmlTitle; }
			set { htmlTitle = value; }
		}

		string link;
		public string Link { 
			get { return link; }
			set { link = value; }
		}

		string displayLink;
		public string DisplayLink { 
			get { return displayLink; }
			set { displayLink = value; }
		}

		string snippet;
		public string Snippet { 
			get { return snippet; }
			set { snippet = value; }
		}

		string htmlSnippet;
		public string HtmlSnippet { 
			get { return htmlSnippet; }
			set { htmlSnippet = value; }
		}

		string mime;
		public string Mime { 
			get { return mime; }
			set { mime = value; }
		}

		GoogleImage image;
		public GoogleImage Image { 
			get { return image; }
			set { image = value; }
		}

		string fileFormat;
		public string FileFormat { 
			get { return fileFormat; }
			set { fileFormat = value; }
		}
	}
}