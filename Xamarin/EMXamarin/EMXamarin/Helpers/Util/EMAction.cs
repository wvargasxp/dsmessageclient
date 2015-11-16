using System;
using em;

namespace EMAction
{
	// Category to allow you to wrap an action (up to two parameters) with a weak reference
	public static class EMActionHelper
	{
		public static Action WeakAction(this object o, Action a) {
			return WeakDelegateProxy.CreateProxy (a).HandleEvent;
		}

		public static Action<T> WeakAction<T>(this object o, Action<T> a) {
			return WeakDelegateProxy.CreateProxy<T> (a).HandleEvent<T>;
		}

		public static Action<T,V> WeakAction<T,V>(this object o, Action<T,V> a) {
			return WeakDelegateProxy.CreateProxy<T,V> (a).HandleEvent<T,V>;
		}
	}
}

