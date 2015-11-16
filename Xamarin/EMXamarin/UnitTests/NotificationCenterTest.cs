using System;
using NUnit.Framework;
using System.Collections.Generic;

namespace em
{
	[TestFixture]
	public class NotificationCenterTest
	{
		public NotificationCenterTest ()
		{
		}

		NotificationCenter notifCenter;
		bool callback;
		Exception observerFailedThrowable;
			
		[Test]
		public void testAddRemove() {
			notifCenter.AddWeakObserver(null, null, TestObserver);
			Assert.AreEqual(2, notifCenter.observers.Count);
			notifCenter.RemoveObserverAction(TestObserver);
			Assert.AreEqual(1, notifCenter.observers.Count);
		}
			
		[Test]
		public void testCallbackMatchesMessage() {
			notifCenter.AddWeakObserver(null, "FOOBAR", TestObserver);
			notifCenter.PostNotification(this, "FOOBAR", null);
			Assert.IsTrue(callback);
		}

		[Test]
		public void testCallbackDoesntMatchMessage() {
			notifCenter.AddWeakObserver(null, "FOOBAR", TestObserver);
			notifCenter.PostNotification(this, "CROBAR", null);
			Assert.IsFalse(callback);
		}

		[Test]
		public void testCallbackMatchesSource() {
			notifCenter.AddWeakObserver(this, null, TestObserver);
			notifCenter.PostNotification(this, "FOOBAR", null);
			Assert.IsTrue(callback);
		}

		[Test]
		public void testCallbackDoesntMatchSource() {
			notifCenter.AddWeakObserver(this, null, TestObserver);
			notifCenter.PostNotification("FOOBAR", "CROBAR", null);
			Assert.IsFalse(callback);
		}

		[Test]
		public void testCallbackGenericObserver() {
			notifCenter.AddWeakObserver(null, null, TestObserver);
			notifCenter.PostNotification(this, "FOOBAR", null);
			Assert.IsTrue(callback);
		}

		[Test]
		public void testCallbackGenericObserverNullSource() {
			notifCenter.AddWeakObserver(null, null, TestObserver);
			notifCenter.PostNotification(null, "FOOBAR", null);
			Assert.IsTrue(callback);
		}

		[Test]
		public void testFailingObserver() {
			notifCenter.AddAssociatedObserver (null, null, (Notification n) => {
				throw new MockException ("Testing Exception, should be handled internally");
			}, this);
			notifCenter.AddWeakObserver(null, null, TestObserver);
			notifCenter.PostNotification(null, "FOOBAR", null);
			Assert.IsTrue(callback);
		}

		[Test]
		public void testSourceAndNameRegistered() {
			notifCenter.AddWeakObserver(this, "FOOBAR", TestObserver);
			notifCenter.PostNotification(this, "FOOBAR", null);
			Assert.IsTrue(callback);		
		}

		[Test]
		public void testSourceAndNameRegisteredOnlyNameMatches() {
			notifCenter.AddWeakObserver(this, "FOOBAR", TestObserver);
			notifCenter.PostNotification("FOOBAR", "FOOBAR", null);
			Assert.IsFalse(callback);		
		}

		[Test]
		public void testSourceAndNameRegisteredOnlySourceMatches() {
			notifCenter.AddWeakObserver(this, "FOOBAR", TestObserver);
			notifCenter.PostNotification(this, "CROBAR", null);
			Assert.IsFalse(callback);		
		}

		[Test]
		public void testNullNotificationNameIsException() {
			try {
				notifCenter.PostNotification(null,null,null);
				Assert.Fail();
			}
			catch (Exception e) {
				// expected
			}
		}

		[Test]
		public void testNullObserverIsException() {
			try {
				notifCenter.AddWeakObserver(null, null, null);
				Assert.Fail();
			}
			catch (Exception e) {
				// expected
			}

			try {
				notifCenter.AddWeakObserver(null, null, null);
				Assert.Fail();
			}
			catch (Exception e) {
				// expected
			}
		}

		[Test]
		public void testRemoveObserver() {
			notifCenter.AddWeakObserver(this, "FOOBAR", TestObserver);
			notifCenter.AddWeakObserver(null, "CROBAR", TestObserver);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			Assert.AreEqual(4, notifCenter.observers.Count);
			notifCenter.RemoveObserverAction(TestObserver);
			Assert.AreEqual(1, notifCenter.observers.Count);
		}

		[Test]
		public void testRemoveObserverNoObjectJustNotification() {
			notifCenter.AddWeakObserver(null, "FOOBAR", TestObserver);
			notifCenter.AddWeakObserver(null, "CROBAR", TestObserver);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			Assert.AreEqual(4, notifCenter.observers.Count);
			notifCenter.RemoveObserverAction(null, "FOOBAR", TestObserver);
			Assert.AreEqual(3, notifCenter.observers.Count);
		}

		[Test]
		public void testRemoveObserverSpecificCriteria() {
			notifCenter.AddWeakObserver(this, "FOOBAR", TestObserver);
			notifCenter.AddWeakObserver(null, "CROBAR", TestObserver);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			Assert.AreEqual(4, notifCenter.observers.Count);
			notifCenter.RemoveObserverAction(this, null,TestObserver);
			Assert.AreEqual(2, notifCenter.observers.Count);
		}

		[Test]
		public void testRemovesArbitraryObservers() {
			notifCenter.AddAssociatedObserver("FOOBAR", "FOOBAR", (Notification notif) => {}, this);
			notifCenter.AddAssociatedObserver("FOOBAR", "CROBAR", (Notification notif) => {}, this);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			notifCenter.RemoveObserverAction("FOOBAR", null, null);
			Assert.AreEqual (2, notifCenter.observers.Count);
		}

		[Test]
		public void testRemoveAll() {
			notifCenter.AddAssociatedObserver("FOOBAR", "FOOBAR", (Notification n) => { }, this);
			notifCenter.AddAssociatedObserver("FOOBAR", "CROBAR", (Notification n) => { }, this);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			notifCenter.RemoveAllObservers();
			Assert.AreEqual(0, notifCenter.observers.Count);
		}

		[Test]
		public void testRemoveByAssociation() {
			notifCenter.AddAssociatedObserver("FOOBAR", "FOOBAR", (Notification n) => { }, this);
			notifCenter.AddAssociatedObserver("FOOBAR", "CROBAR", (Notification n) => { }, this);
			notifCenter.AddWeakObserver(this, null, TestObserver);

			notifCenter.RemoveObserver(this);
			Assert.AreEqual(0, notifCenter.observers.Count);
		}

		[Test]
		public void testDoesntRemoveWrongObserver() {
			notifCenter.AddAssociatedObserver("FOOBAR", "FOOBAR", (Notification n) => { }, this);
			notifCenter.AddWeakObserver("FOOBAR", "FOOBAR", TestObserver);
			notifCenter.RemoveObserverAction("FOOBAR", "FOOBAR", TestObserver);

			Assert.AreEqual(2, notifCenter.observers.Count);
		}

		[Test]
		public void testReferenceReceivesMessage() {
			notifCenter.AddWeakObserver("FOOBAR", "FOOBAR", TestObserver);
			notifCenter.PostNotification("FOOBAR", "FOOBAR");
			Assert.IsTrue(callback);
		}

		[Test]
		public void testConcurrentModificationFix() {
			notifCenter.AddAssociatedObserver(notifCenter, NotificationCenter.NOTIFICATION_OBSERVER_REMOVED, (Notification n) => {}, null);
			notifCenter.AddWeakObserver("FOOBAR", "FOOBAR", TestObserver);

			// trying to force 'new object()' to be gc'ed
			System.GC.Collect ();
			System.GC.WaitForPendingFinalizers();

			notifCenter.RemoveObserverAction(TestObserver);
			Assert.AreEqual (1, notifCenter.observers.Count);
		}

		[Test]
		public void testConcurrentModificationObserverRemovingSelf() {
			Action<Notification> notifObserver = null;
			notifObserver = (Notification notif) => {
				notifCenter.RemoveObserverAction (notifObserver);
			};
			notifCenter.AddAssociatedObserver(null, "FOOBAR", notifObserver, this);
			notifCenter.AddWeakObserver(null, "FOOBAR", TestObserver);

			notifCenter.PostNotification(this, "FOOBAR");
			Assert.IsTrue(callback);
			Assert.AreEqual(2, notifCenter.observers.Count);
		}

		[Test]
		public void testConcurrentModificationObserverRemovingSelfThenPosting() {
			int callbackCount = 0;
			Action<Notification> notifObserver = null;
			notifObserver = (Notification notification) => {
				switch ( callbackCount ) {
				case 0:
					callbackCount++;
					notifCenter.RemoveObserverAction(notifObserver);
					notifCenter.PostNotification(this, "FOOBAR");
					break;

				default:
					callbackCount++;
					Assert.Fail();
					break;
				}
			};
			notifCenter.AddAssociatedObserver(null, "FOOBAR", notifObserver, this);
			notifCenter.AddWeakObserver(null, "FOOBAR", TestObserver);

			notifCenter.PostNotification(this, "FOOBAR");
			Assert.IsTrue(callback);
			Assert.AreEqual(2, notifCenter.observers.Count);
		}

		[Test]
		public void testExceptionThrownForDelegateWeakAssociation() {
			try {
				notifCenter.AddWeakObserver(null, "FOOBAR", (Notification n) => { } );
				Assert.Fail();
			}
			catch (Exception e) {
				// expected
			}
		}
			
		[Test]
		public void testCleansObsoleteObserver() {
			notifCenter.CullFrequency = 0;
			WeakReference notifObserver = WeaklyReffedObjectWithACallback ();

			AddWeakObserverToNotificatinCenter (notifObserver);

			System.GC.Collect();
			System.GC.WaitForPendingFinalizers();

			Assert.IsFalse (notifObserver.IsAlive);
			notifCenter.FindAndRemoveObsoleteObservers();

			Assert.AreEqual (1, notifCenter.observers.Count);
		}

		[Test]
		public void testCleansObserverForObsoleteSource() {
			notifCenter.CullFrequency = 0;

			WeakReference theSourceRef = AddObserverOverWeakSource (notifCenter);

			System.GC.Collect();
			System.GC.WaitForPendingFinalizers();

			Assert.IsFalse (theSourceRef.IsAlive);
			notifCenter.FindAndRemoveObsoleteObservers();

			Assert.AreEqual (1, notifCenter.observers.Count);
		}

		[SetUp]
		protected void setUp() {
			notifCenter = new NotificationCenter();

			notifCenter.AddAssociatedObserver(null, NotificationCenter.NOTIFICATION_OBSERVER_FAILED, (Notification notification) => {
				Dictionary<String,Object> details = notification.Extra as Dictionary<String,Object>;
				observerFailedThrowable = details["throwable"] as Exception;
			}, this);

			callback = false;
			observerFailedThrowable = null;
		}

		/*
		[TearDown]
		protected void tearDown() {
			if ( observerFailedThrowable != null ) {
				if ( observerFailedThrowable.GetType..Equals( typeof( MockException)) )
					; // This is part of testing, no problem
				else if ( observerFailedThrowable.GetType == typeof(SystemException) )
					throw (SystemException) observerFailedThrowable;
			}
		}
		*/
			
		class MockException : SystemException {
			public MockException() {
			}

			public MockException(String message, SystemException cause) : base (message, cause) {
			}

			public MockException(String message) : base (message) {
			}
		}

		protected void TestObserver(Notification notif) {
			callback = true;
		}

		protected WeakReference WeaklyReffedObjectWithACallback() {
			return new WeakReference(new ObjectWithACallback());
		}

		protected void AddWeakObserverToNotificatinCenter(WeakReference weakRef) {
			ObjectWithACallback cb = weakRef.Target as ObjectWithACallback;
			notifCenter.AddWeakObserver(null, "FOOBAR", cb.TestObserver);
		}

		protected WeakReference AddObserverOverWeakSource(NotificationCenter notifCenter) {
			object source = new object ();
			notifCenter.AddWeakObserver (source, "FOOBAR", TestObserver);
			return new WeakReference (source);
		}

		class ObjectWithACallback {
			public void TestObserver(Notification notif) {
			}
		}
	}
}

