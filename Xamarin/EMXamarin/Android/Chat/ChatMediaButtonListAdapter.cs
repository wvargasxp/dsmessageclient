
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics.Drawables;

namespace Emdroid {		
	public class ChatMediaButtonListAdapter : ArrayAdapter<string> {

		private IList<string> buttonLabels;
		public IList<string> ButtonLabels {
			get { return buttonLabels; }
			set { buttonLabels = value; }
		}

		private Context Context { get; set; }

		public ChatMediaButtonListAdapter (Context context, int resource, List<string> strings) : base (context, resource, strings) {
			this.ButtonLabels = strings;
			this.Context = context;
		}

		public override View GetView (int position, View convertView, ViewGroup parent) {
			if (convertView == null) {
				View v = LayoutInflater.From (this.Context).Inflate (Resource.Layout.chat_media_button, parent, false);
				LinearLayout layout = v.FindViewById <LinearLayout> (Resource.Id.simpleButtonLayout);
				convertView = layout;
			}
			TextView textView = convertView.FindViewById <TextView> (Resource.Id.chatMediaButton);
			textView.Text = this.ButtonLabels [position];
			return convertView;
		}

		public override int Count {
			get { return this.ButtonLabels == null ? 0 : this.ButtonLabels.Count; }
		}

		public string ResultFromPosition (int position) {
			if (position >= this.ButtonLabels.Count) {
				return null;
			}
			return this.ButtonLabels [position];
		}

		public override long GetItemId (int position) {
			return position;
		}

		public override bool IsEnabled (int position) {
			return true;
		}

		public void UpdateFirstButton (string str) {
			this.ButtonLabels [0] = str;
			NotifyDataSetChanged ();
		}
	}
}

