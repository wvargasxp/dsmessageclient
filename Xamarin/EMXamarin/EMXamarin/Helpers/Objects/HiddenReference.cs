using System;
using System.Collections.Generic;

namespace em {
	// https://developer.xamarin.com/guides/android/advanced_topics/garbage_collection/#Reduce_Referenced_Instances
	/* The gist of this class is to reduce the amount of referenced instances that a Java.Lang.Object may have, effectively optimizing GC collection times. */
	public class HiddenReference<T> {

		private static Dictionary<int, T> table = new Dictionary<int, T> ();
		private static int idgen = 0;

		private int id;

		public HiddenReference (T obj) {
			lock (table) {
				this.id = idgen++;
			}

			this.Value = obj;
		}

		public HiddenReference () {
			lock (table) {
				this.id = idgen++;
			}
		}

		~HiddenReference () {
			lock (table) {
				table.Remove (this.id);
			}
		}

		public T Value {
			get { 
				lock (table) { 
					T val = default(T);
					table.TryGetValue (this.id, out val);
					return val;
				} 
			}

			set { 
				lock (table) {
					table [this.id] = value; 
				} 
			}
		}
	}
}

