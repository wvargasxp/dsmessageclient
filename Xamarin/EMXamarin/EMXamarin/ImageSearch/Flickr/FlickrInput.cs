using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace em {
	public class FlickrInput {
		#region error
		int code;
		public int Code { 
			get { return code; }
			set { code = value; }
		}

		string message;
		public string Message { 
			get { return message; }
			set { message = value; }
		}
		#endregion

		string stat;
		public string Stat { 
			get { return stat; }
			set { stat = value; }
		}

		// The local var matches the json.
		// The property getter/setter is to define what the json means semantically.
		// In this, even though it's called photos, 
		// it's actually just a photo input that contains a list of photos, so it's best to make that clear.
		FlickrPhotosInput photos;
		[JsonProperty(PropertyName = "photos")]
		public FlickrPhotosInput PhotoInput {
			get { return photos; }
			set { photos = value; }
		}

		public FlickrInput () {
		}
	}

	public class FlickrPhotoInput {
		string id; 
		public string Id { 
			get { return id; }
			set { id = value; }
		}

		string owner; 
		public string Owner { 
			get { return owner; }
			set { owner = value; }
		}

		string secret; 
		public string Secret { 
			get { return secret; }
			set { secret = value; }
		}

		string server; 
		public string Server { 
			get { return server; }
			set { server = value; }
		}

		int farm; 
		public int Farm { 
			get { return farm; }
			set { farm = value; }
		}

		string title;
		public string Title { 
			get { return title; }
			set { title = value; }
		}

		int ispublic;
		[JsonProperty(PropertyName = "ispublic")]
		public int IsPublic { 
			get { return ispublic; }
			set { ispublic = value; }
		}

		int isfriend;
		[JsonProperty(PropertyName = "isfriend")]
		public int IsFriend { 
			get { return isfriend; }
			set { isfriend = value; }
		}

		int isfamily;
		[JsonProperty(PropertyName = "isfamily")]
		public int IsFamily { 
			get { return isfamily; }
			set { isfamily = value; }
		}
	}

	public class FlickrPhotosInput {
		int page;
		public int Page { 
			get { return page; }
			set { page = value; }
		}

		int pages;
		public int Pages { 
			get { return pages; }
			set { pages = value; }
		}

		int perpage;
		[JsonProperty(PropertyName = "perpage")]
		public int PerPage { 
			get { return perpage; }
			set { perpage = value; }
		}

		string total;
		public string Total { 
			get { return total; }
			set { total = value; }
		}

		// Though the json returns back photo, it's actually a list of photos.
		List<FlickrPhotoInput> photo;
		[JsonProperty(PropertyName = "photo")]
		public List<FlickrPhotoInput> Photos { 
			get { return photo; }
			set { photo = value; }
		}
	}
}

