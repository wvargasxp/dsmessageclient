using System;

namespace em {
	/*
	s	small square 75x75
	q	large square 150x150
	t	thumbnail, 100 on longest side
	m	small, 240 on longest side
	n	small, 320 on longest side
	-	medium, 500 on longest side
	z	medium 640, 640 on longest side
	c	medium 800, 800 on longest side†
	b	large, 1024 on longest side*
	h	large 1600, 1600 on longest side†
	k	large 2048, 2048 on longest side†
	o	original image, either a jpg, gif or png, depending on source format
	*/
	public class FlickrPhoto : AbstractSearchImage {
		// Lookup info
		string photoID;
		int farm;
		string server;
		string secret;

		public string PhotoID {
			get { return photoID; }
			set { photoID = value; }
		}

		public int Farm {
			get { return farm; }
			set { farm = value; }
		}

		public string Server {
			get { return server; }
			set { server = value; }
		}

		public string Secret {
			get { return secret; }
			set { secret = value; }
		}

		public override string UrlOfImageAsString () {
			return FlickrSearch.FlickrPhotoURLForFlickrPhoto (this, "z"); // todo; we want to use flickr.photo.getSizes to get the largest size of various sizes
		}


		public override string UrlOfThumbnailAsString () {
			return FlickrSearch.FlickrPhotoURLForFlickrPhoto (this, "t");
		}

		public FlickrPhoto (FlickrPhotoInput input, EMAccount eAccount) {
			this.Farm = input.Farm;
			this.Server = input.Server;
			this.Secret = input.Secret;
			this.PhotoID = input.Id;
			this.Account = eAccount;
		}
	}
}

