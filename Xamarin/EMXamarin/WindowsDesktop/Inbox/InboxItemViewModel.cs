using em;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace WindowsDesktop.Inbox {
	class InboxItemViewModel {

		private ChatList _chatList = null;
		private ChatList ChatList {
			get {
				if (this._chatList == null) {
					this._chatList = App.Instance.Model.chatList;
				}

				return this._chatList;
			}
		}

		private IList<InboxItemTemplate> _items = null;
		private IList<InboxItemTemplate> Items {
			get {
				if (this._items == null) {
					this._items = new List<InboxItemTemplate> ();
				}

				return this._items;
			}

			set { this._items = value; }
		}

		public IList<InboxItemTemplate> List {
			get {
				IList<InboxItemTemplate> l = this.Items.ToList ();

				ChatEntry underConstruction = this.ChatList.underConstruction;
				if (underConstruction != null) {
					l.Insert (0, new InboxItemTemplate (underConstruction));
				}

				return l;
			}
		} 

		public bool HasNewChat {
			get { return this.ChatList.underConstruction != null; }
		}

		public InboxItemViewModel () { }

		public void UpdateSource (IList<ChatEntry> entries) {
			this.Items = GetListFromEntries (entries);
		}

		public static InboxItemViewModel From (IList<ChatEntry> entries) {
			InboxItemViewModel v = new InboxItemViewModel ();
			v.Items = GetListFromEntries (entries);

			return v;
		}

		private static IList<InboxItemTemplate> GetListFromEntries (IList<ChatEntry> entries) {
			IList<InboxItemTemplate> items = new List<InboxItemTemplate> ();

			foreach (ChatEntry entry in entries) {
				items.Add (new InboxItemTemplate (entry));
			}

			return items;
		} 
	}
}