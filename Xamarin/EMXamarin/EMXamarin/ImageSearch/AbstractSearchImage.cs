using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;

namespace em {
	public abstract class AbstractSearchImage {

		EMAccount account = null;
		public EMAccount Account {
			get { return account; }
			set { account = value; }
		}

		private WeakReference thumbnailByteRef = null;
		public WeakReference ThumbnailAsBytesRef {
			get { return thumbnailByteRef; }
			set { thumbnailByteRef = value; }
		}

		private WeakReference fullImageByteRef = null;
		public WeakReference FullImageAsBytesRef {
			get { return fullImageByteRef; }
			set { fullImageByteRef = value; }
		}

		public string ThumbnailKeyForCache {
			get;
			set;
		}

		public abstract string UrlOfImageAsString ();
		public abstract string UrlOfThumbnailAsString ();

		public void GetFullImageAsBytesAsync (Action<byte[]> callback) {
			byte[] retVal = null;
			byte[] fullImageAsBytes = this.FullImageAsBytesRef != null ? this.FullImageAsBytesRef.Target as byte[] : null;
			retVal = fullImageAsBytes != null && fullImageAsBytes.Length > 0 ? fullImageAsBytes : null;

			if (retVal != null) {
				callback (retVal);
				return;
			}

			string url = UrlOfImageAsString ();
			this.ThumbnailKeyForCache = url;
			this.Account.SearchForImage (url, (EMHttpResponse obj) => {
				if (obj.IsSuccess) {
					byte[] bytes = obj.ResponseAsBytes;
					if (bytes != null && bytes.Length > 0) {
						this.FullImageAsBytesRef = new WeakReference (bytes);
						retVal = bytes;
					}
				}

				// Last resort is to use thumbnail ref, otherwise we return null.
				if (retVal == null) {
					byte[] thumbnailBytes = this.ThumbnailAsBytesRef != null ? this.ThumbnailAsBytesRef.Target as byte[] : null;
					retVal = thumbnailBytes != null && thumbnailBytes.Length > 0 ? thumbnailBytes : null;
				}

				callback (retVal);
			});
		}

		public void GetThumbnailAsBytesAsync (int position, Action<int, byte[]> callback) {
			byte[] retVal = null;
			byte[] thumbnailBytes = this.ThumbnailAsBytesRef != null ? this.ThumbnailAsBytesRef.Target as byte[] : null;
			retVal = thumbnailBytes != null && thumbnailBytes.Length > 0 ? thumbnailBytes : null;
			if (retVal != null) {
				callback (position, retVal);
				return;
			}

			string url = UrlOfThumbnailAsString ();
			this.ThumbnailKeyForCache = url;
			this.Account.SearchForImage (url, (EMHttpResponse obj) => {
				if (obj.IsSuccess) {
					byte[] bytes = obj.ResponseAsBytes;
					if (bytes != null && bytes.Length > 0) {
						this.ThumbnailAsBytesRef = new WeakReference (bytes);
						retVal = bytes;
					}
				}

				callback (position, retVal);
			});
		}

		public void GetThumbnailAsBytesAsync (Action<byte[]> callback) {
			byte[] retVal = null;
			byte[] thumbnailBytes = this.ThumbnailAsBytesRef != null ? this.ThumbnailAsBytesRef.Target as byte[] : null;
			retVal = thumbnailBytes != null && thumbnailBytes.Length > 0 ? thumbnailBytes : null;
			if (retVal != null) {
				callback (retVal);
				return;
			}

			string url = UrlOfThumbnailAsString ();
			this.ThumbnailKeyForCache = url;
			this.Account.SearchForImage (url, (EMHttpResponse obj) => {
				if (obj.IsSuccess) {
					byte[] bytes = obj.ResponseAsBytes;
					if (bytes != null && bytes.Length > 0) {
						this.ThumbnailAsBytesRef = new WeakReference (bytes);
						retVal = bytes;
					}
				}

				callback (retVal);
			});
		}
	}
}

