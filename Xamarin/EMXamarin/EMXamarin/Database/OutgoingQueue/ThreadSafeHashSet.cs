using System;
using System.Collections.Generic;

namespace em {
	public class ThreadSafeHashSet<Type> {

		private object setLock = new object ();

		private ISet<Type> innerSet = new HashSet<Type> ();

		public bool Add (Type item) {
			lock (setLock) {
				return innerSet.Add (item);
			}
		}

		public bool Remove (Type item) {
			lock (setLock) {
				return innerSet.Remove (item);
			}
		}
	}
}

