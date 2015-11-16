using System.Collections.Generic;

namespace em {
	public class AddressBookContact {
		public string firstName { get; set; }
		public string lastName { get; set; }
		public string displayName { get; set; }
		public string clientID { get; set; }
		string thumbnailUri;
		public IList<ContactInfo> contactInfo { get; set; }

		public AddressBookContact () {
			contactInfo = new List<ContactInfo> ();
		}

		public string GetThumbnailUri() {
			return thumbnailUri;
		}

		public void SetThumbnailUri(string uri) {
			thumbnailUri = uri;
		}
	}

	public class ContactInfo {
		public string localizedLabel { get; set; }
		public string value { get; set; }
		public ContactInfoType contactInfoType { get; set; }
	}

	public enum ContactInfoType {
		email,
		phone
	}
}