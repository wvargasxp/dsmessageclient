using System;
using System.Collections.Generic;

namespace em {
	public class SearchResponseInput {
		private bool aliasSearchResult;
		public bool AliasSearchResult {
			get {
				return aliasSearchResult;
			}

			set {
				aliasSearchResult = value;
			} 
		}

		private List<ContactInput> contacts;
		public List<ContactInput> Contacts {
			get {
				return contacts;
			}

			set {
				contacts = value;
			}
		}
	}
}

