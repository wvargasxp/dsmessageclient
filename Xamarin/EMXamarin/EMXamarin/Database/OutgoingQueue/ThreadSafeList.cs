using System;
using System.Collections.Generic;

namespace em {
	public class ThreadSafeList<Type> {

		private object listLock = new object ();

		private IList<Type> innerList = new List<Type> ();

		public void Add (Type item) {
			lock (listLock) {
				innerList.Add (item);
			}
		}

		public IList<Type> Drain () {
			IList<Type> retval = new List<Type> ();
			lock (listLock) {
				foreach (Type item in innerList) {
					retval.Add (item);
				}

				innerList.Clear ();
			}

			return retval;
		}
	}
}

