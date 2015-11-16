using System;

namespace em
{
	/* A genericall observable property */
	public class Property<T>
	{
		public delegate void DidChangePropertyValue(Property<T> prop, T previous);
		public DidChangePropertyValue DelegateDidChangePropertyValue = delegate(Property<T> prop, T oldValue) {
		};

		private T v;
		public T Value {
			get {
				return v;
			}

			set {
				T previous = v;
				if ( (v == null && value != null) ||
					 (v != null && value == null) ||
					 (!v.Equals(value)) ) {
					v = value;

					DelegateDidChangePropertyValue(this, previous);
				}
			}
		}
	}
}

