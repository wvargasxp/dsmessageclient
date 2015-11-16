using System;
using System.Reflection;

namespace em {
	public class WeakDelegateProxy : IDisposable {
		public WeakReference _targetReference { get; set; }
		public MethodInfo _method { get; set; }

		public static WeakDelegateProxy CreateProxy(Action action) {
			var retVal = new WeakDelegateProxy ();
			retVal._method = action.Method;
			retVal._targetReference = new WeakReference (action.Target);

			return retVal;
		}

		public static WeakDelegateProxy CreateProxy<T>(Action<T> action) {
			var retVal = new WeakDelegateProxy ();
			retVal._method = action.Method;
			retVal._targetReference = new WeakReference (action.Target);

			return retVal;
		}

		public static WeakDelegateProxy CreateProxy<T, J>(Action<T, J> action) {
			var retVal = new WeakDelegateProxy ();
			retVal._method = action.Method;
			retVal._targetReference = new WeakReference (action.Target);

			return retVal;
		}

		public bool TargerExists {
			get { return _targetReference.IsAlive; }
		}

		public void Dispose() {
		}

		public void HandleEvent() {
			var target = _targetReference.Target;
			if (target != null) {
				var callback = (Action)Delegate.CreateDelegate(typeof(Action), target, _method);
				if (callback != null)
					callback ();
			}
		}

		public void HandleEvent<T>(T obj) {
			var target = _targetReference.Target;
			if (target != null) {
				var callback = (Action<T>)Delegate.CreateDelegate(typeof(Action<T>), target, _method);
				if (callback != null)
					callback (obj);
			}
		}

		public void HandleEvent<T, J>(T obj, J obj2) {
			var target = _targetReference.Target;
			if (target != null) {
				var callback = (Action<T,J>)Delegate.CreateDelegate(typeof(Action<T, J>), target, _method);
				if (callback != null)
					callback (obj, obj2);
			}
		}
	}
}