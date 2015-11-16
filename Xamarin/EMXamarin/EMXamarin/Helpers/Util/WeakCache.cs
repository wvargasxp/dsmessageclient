using System;
using System.Collections.Generic;

namespace em {
	/**
	 * A Cache that weakly references the value (allowing it to slip out of memory
	 * if it is nolonger being accessed anywhere.  The idea is to cache the object but
	 * not to 'own' it.  When the 'owner' discards it we no longer need it.
	 * 
	 * TODO It should periodically purge stale weak references
	 */
	public class WeakCache<K,V> {
		Dictionary<K,WeakReference> underlyingDictionary;

		public WeakCache () {
			underlyingDictionary = new Dictionary<K,WeakReference>();
		}

		public bool ContainsKey(K key) {
			lock (this) {
				WeakReference weakRef;
				if ( !underlyingDictionary.TryGetValue(key, out weakRef))
					return false;
				else {
					if (weakRef.IsAlive)
						return true;

					underlyingDictionary.Remove (key);
					return false;
				}
			}
		}

		public void Put(K key, V value) {
			lock (this) {
				WeakReference weakRef;
				if (underlyingDictionary.TryGetValue (key, out weakRef))
					underlyingDictionary.Remove (key);
				underlyingDictionary.Add (key, new WeakReference (value));
			}
		}

		public V Get(K key) {
			lock (this) {
				WeakReference weakRef;
				if (underlyingDictionary.TryGetValue (key, out weakRef)) {
					if (weakRef.IsAlive)
						return (V)weakRef.Target;

					underlyingDictionary.Remove (key);
				}

				return default(V);
			}
		}

		public void Delete(K key) {
			lock (this) {
				if (underlyingDictionary.ContainsKey (key))
					underlyingDictionary.Remove (key);
			}
		}

		public void Purge() {
			lock (this) {
				List<K> keys = new List<K>(underlyingDictionary.Keys);
				foreach (K key in keys) {
					WeakReference weakRef = underlyingDictionary [key];
					if (!weakRef.IsAlive)
						underlyingDictionary.Remove (key);
				}
			}
		}

		public IList<V> Values() {
			List<V> retVal = new List<V> ();
			lock (this) {
				List<K> keys = new List<K>(underlyingDictionary.Keys);
				foreach (K key in keys) {
					WeakReference weakRef = underlyingDictionary [key];
					if (!weakRef.IsAlive)
						underlyingDictionary.Remove (key);
					else {
						V value = (V)weakRef.Target;
						retVal.Add (value);
					}
				}
			}

			return retVal;
		}
	}
}

