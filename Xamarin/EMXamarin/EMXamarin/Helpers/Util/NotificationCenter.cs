using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.Diagnostics;

namespace em
{
	public class NotificationCenter {
		/////
		// Notification posted when an observer is added to this notification center.
		// The <em>extra</em> parameter will contain a <code>Map</code> with the following keys
		// <table border="1">
		// <tr><th>key</th><th>description</th></tr>
		// <tr><td>source</th><td>The source the observer registered for (may be <code>null</code>)</td></tr>
		// <tr><td>notificationName</th><td>The notification name the observer registered for (may be <code>null</code>)</td></tr>
		// <tr><td>observer</th><td>The observer getting registered</td></tr>
		// </table>
		///
		public static readonly String NOTIFICATION_OBSERVER_ADDED = "oranjestad.commons.notifications.NOTIFICATION_OBSERVER_ADDED";

		/////
		// Notification posted when an observer is removed from this notification center.
		// The <em>extra</em> parameter will contain a <code>Map</code> with the following keys
		// <table border="1">
		// <tr><th>key</th><th>description</th></tr>
		// <tr><td>source</th><td>The source used to match any registered listeners with
		// (if a rule exists that would result in this observer receiving that event
		// the registration of that observer is removed)</td></tr>
		// <tr><td>notificationName</th><td>The notification name used to match any registered listeners with
		// (if a rule exists that would result in this observer receiving that event
		// the registration of that observer is removed)</td></tr>
		// <tr><td>observer</th><td>The observer getting removed as an observer</td></tr>
		// </table>
		///
		public static readonly String NOTIFICATION_OBSERVER_REMOVED = "oranjestad.commons.notifications.NOTIFICATION_OBSERVER_REMOVED";

		/////
		// Notification posted when an observer threw an exception while handling 
		// the posting of a notification.  The original (error causing) notification
		// will continue to post for subsequent observers.  The <em>extra</em>
		// parameter will contain a <code>Map</code> with the following keys
		// <table border="1">
		// <tr><th>key</th><th>description</th></tr>
		// <tr><td>source</th><td>The source provided when this notification was
		// posted (may be <code>null</code>)</td></tr>
		// <tr><td>notificationName</th><td>The notification provided when the error
		// occurred.</td></tr>
		// <tr><td>observer</th><td>The observer that threw the exception</td></tr>
		// <tr><td>throwable</th><td>The throwable that was caught while trying to
		// forward the posted notification.</td></tr>
		// </table>
		///
		public static readonly String NOTIFICATION_OBSERVER_FAILED = "oranjestad.commons.notifications.NOTIFICATION_OBSERVER_FAILED";

		/** Default culling frequencing looking for dead observers */
		public static readonly int DEFAULT_CULL_FREQUENCE = 100;

		/////
		// Singleton factory method for acquiring a <code>static</code> default
		// notification center.
		// @return The notification center.
		///
		static NotificationCenter defNC;
		public static NotificationCenter DefaultCenter {
			get {
				lock( typeof( NotificationCenter) ) {
					if ( defNC == null )
						defNC = new NotificationCenter();

					return defNC;
				}
			}

			set {
				lock (typeof(NotificationCenter)) {
					defNC = value;
				}
			}
		}

		ThreadLocal<HashSet<NotificationObserverRecord>> itemsToDelete = new ThreadLocal<HashSet<NotificationObserverRecord>>();



		Dictionary<string,IList<NotificationObserverRecord>> notifButNoSourceObservers = new Dictionary<string,IList<NotificationObserverRecord>>();
		WeakDictionary<object,IList<NotificationObserverRecord>> sourceButNoNotificationObservers = new WeakDictionary<object,IList<NotificationObserverRecord>>();
		Dictionary<string,WeakDictionary<object,IList<NotificationObserverRecord>>> sourceAndNotificationObservers = new Dictionary<string,WeakDictionary<object,IList<NotificationObserverRecord>>>();
		IList<NotificationObserverRecord> unrestrictedObservers = new List<NotificationObserverRecord>();
		int cullFrequence = 0;//DEFAULT_CULL_FREQUENCE;
		int currentCullCount = 0;

		public int CullFrequency {
			get {
				return cullFrequence;
			}

			set {
				cullFrequence = value;
			}
		}

		/////
		// Ands an observer for a given notification name (optional) and source (optional).
		// If the observer is added with no name or source provided it will receive
		// all notifications from this notification center.
		// @param source The source of the object to limit receiving events from
		// (only notifications sourced from the supplied object will be sent to
		// the observer).  If <code>null</code> then notifications from all sources
		// will be sent.
		// @param notificationName The notification name to limit the observer receiving
		// posts for.  Only posts matching this name will be forwarded to the observer.
		// If <code>null</code> the observer will receive notifications regardless
		// of name.
		// @param observer The (non <code>null</code>) observer that will handle the
		// notifications.
		// @param association An optional object that you can associate the observer
		// with to aid in removing observers later.  Since Java supports anonymous
		// inner classes, often times its easy to add an observer using an inline
		// defined class e.g.
		// <pre></code>    NotificationCenter.defaultCenter().addObserver(null, null, new NotificationObserver() {
		//        public void didReceiveNotifiction(Notification notification) {
		//            // handle here.
		//        }
		//    }, this);</code></pre>
		// In this case, removing the observer is difficult because there's no variable
		// that references the listener.  By associating another object with the
		// observer (in this case using <code>this</code>) we can later remove the
		// associated observers. e.g.
		// <pre><code>    // remove previously added observer
		//    NotificationCenter.defaultCenter().removeObserverByAssociation(this);</code></pre>
		//  
		// It's also possible for the association to be a Java {@link Reference} object.
		// If it is a reference, if the reference is ever cleared the observer is
		// automatically removed.
		///
		public void AddAssociatedObserver(Object source, String notificationName, Action<Notification> observer, object association) {
			if ( observer == null )
				throw new Exception("Observer cannot be null in addObserver");

			NotificationObserverRecord record = new NotificationObserverRecord(source, notificationName, observer, association);
			AddObserver (record);
		}

		/////
		// Ands an observer for a given notification name (optional) and source (optional).
		// If the observer is added with no name or source provided it will receive
		// all notifications from this notification center.
		// @param source The source of the object to limit receiving events from
		// (only notifications sourced from the supplied object will be sent to
		// the observer).  If <code>null</code> then notifications from all sources
		// will be sent.
		// @param notificationName The notification name to limit the observer receiving
		// posts for.  Only posts matching this name will be forwarded to the observer.
		// If <code>null</code> the observer will receive notifications regardless
		// of name.
		// @param observerRef A Reference to an observer that can handle notifications.
		// When this reference becomes cleared, the notification center will remove
		// it from the list at a time in the future.
		// @param association An optional object that you can associate the observer
		// with to aid in removing observers later.  Since Java supports anonymous
		// inner classes, often times its easy to add an observer using an inline
		// defined class e.g.
		// <pre></code>    NotificationCenter.defaultCenter().addObserver(null, null, new NotificationObserver() {
		//        public void didReceiveNotifiction(Notification notification) {
		//            // handle here.
		//        }
		//    }, this);</code></pre>
		// In this case, removing the observer is difficult because there's no variable
		// that references the listener.  By associating another object with the
		// observer (in this case using <code>this</code>) we can later remove the
		// associated observers. e.g.
		// <pre><code>    // remove previously added observer
		//    NotificationCenter.defaultCenter().removeObserverByAssociation(this);</code></pre>
		//    
		// It's also possible for the association to be a Java {@link Reference} object.
		// If it is a reference, if the reference is ever cleared the observer is
		// automatically removed.
		///
		public void AddWeakObserver(Object source, String notificationName, Action<Notification> observerRef) {
			if ( observerRef == null )
				throw new Exception("ObserverRef cannot be null in addObserver");
			if (observerRef.Target == null)
				throw new Exception ("delegate callsbacks shoud be added as associated observers, otherwise they will gc");

			WeakDelegateProxy proxy = WeakDelegateProxy.CreateProxy (observerRef);
			NotificationObserverRecord record = new NotificationObserverRecord(source, notificationName, proxy);
			AddObserver (record);
		}

		public void AddWeakObservers<T>(IList<T> sources, String notificationName, Action<Notification> observerRef) {
			foreach (Object src in sources) {
				AddWeakObserver (src, notificationName, observerRef);
			}
		}

		protected void AddObserver(NotificationObserverRecord record) {
			lock (this) {
				if (record.MatchesEverything)
					unrestrictedObservers.Add (record);
				else if (record.MatchesNotificationOnly) {
					IList<NotificationObserverRecord> observers = notifButNoSourceObservers.ContainsKey (record.NotificationName) ?
												notifButNoSourceObservers [record.NotificationName] : null;
					if (observers == null) {
						observers = new List<NotificationObserverRecord> ();
						notifButNoSourceObservers [record.NotificationName] = observers;
					}
					observers.Add (record);
				} else if (record.MatchesSourceAndNotification ) {
					WeakDictionary<object,IList<NotificationObserverRecord>> weakObjectTable = sourceAndNotificationObservers.ContainsKey (record.NotificationName) ? sourceAndNotificationObservers[record.NotificationName] : null;
					if (weakObjectTable == null) {
						weakObjectTable = new WeakDictionary<object, IList<NotificationObserverRecord>>();
						sourceAndNotificationObservers [record.NotificationName] = weakObjectTable;
					}
					IList<NotificationObserverRecord> observers;
					if (!weakObjectTable.TryGetValue (record.Source, out observers)) {
						observers = new List<NotificationObserverRecord> ();
						weakObjectTable [record.Source] = observers;
					}
					observers.Add (record);	
				} else if (record.MatchesSourceOnly) {
					IList<NotificationObserverRecord> observers;
					if (!sourceButNoNotificationObservers.TryGetValue (record.Source, out observers)) {
						observers = new List<NotificationObserverRecord> ();
						sourceButNoNotificationObservers [record.Source] = observers;
					}
					observers.Add (record);	
				}

				Dictionary<string,object> extra = new Dictionary<string,object> ();
				extra["source"] = record.Source;
				extra["notificationName"] = record.NotificationName;
				//extra["observer"] = record.ObserverRef.GetAction();
				PostNotification (this, NOTIFICATION_OBSERVER_ADDED, extra);
			}
		}

		/////
		// <p>Removes an observer for a given notification name and observer.  The supplied
		// <em>source</em> and <em>notificationName</em> will remove any instance of
		// the supplied observer that would receive messages of that type.  That is
		// if a posted notification with the supplied <em>source</em> and <em>notificationName</em>
		// would forward along to that observer, then that observer is removed in this
		// call.</p>
		// <p>Also, if <em>observer</em> is <code>null</code> then any observers
		// that would match the <em>source</em> and <em>notificationName</em> are
		// removed.</p>
		// @param source The source type to remove observers for.  (<code>null</code>
		// indicates any source).
		// @param notificationName The notification name to remove observers for.
		// (<code>null</code> indicates any notification name).
		// @param observer The observer to remove (<code>null</code> indicates any
		// observer).
		///
		public void RemoveObserverAction(Object source, String notificationName, Action<Notification> observer) {
			InnerRemoveObserver(source,notificationName,observer,null);
		}

		/////
		// Method that removes all observers.  (equivalent to
		// <code>removeObserver(null,null,null);</code>)
		///
		public void RemoveAllObservers() {
			RemoveObserverAction(null,null,null);
		}

		/////
		// Removes the specified observer regardless of source or notification
		// name. (equivalent to <code>removeObserver(null,null,<em>observer</em>);</code>)
		// @param observer
		///
		public void RemoveObserverAction(Action<Notification> observer) {
			RemoveObserverAction(null, null, observer);
		}

		/////
		// Removes observers based on the associated object they were registered with.
		// When you add an observer you can optional add an <em>association</em> object.
		// This is useful if the observer is an anonymous inner class for which you don't
		// maintain a reference.  This method let's you remove those references by
		// the associated object.
		// @param association The associated object (must not be null).  If the associated
		// object was wrapped in a {@link Reference} when it was registered, it should
		// not be wrapped here in this call.  (e.g) <br />
		// <pre><code>
		// NotificationCenter notifCenter = ...;
		// notifCenter.addObserver(<em>source</em>, <em>notifName</em>, <em>someObserver</em>, new WeakReference(<strong>this</strong>));
		// ...
		// notifCenter.removeObserverByAssociation( <strong>this</strong> ); // don't wrap association in reference here.
		// </code></pre>
		///
		public void RemoveObserver(Object association) {
			RemoveObserver(null, null, association);
		}

		/////
		// Removes observers based on the associated object they were registered with.
		// When you add an observer you can optional add an <em>association</em> object.
		// This is useful if the observer is an anonymous inner class for which you don't
		// maintain a reference.  This method let's you remove those references by
		// the associated object.
		// @param source The source type to remove observers for.  (<code>null</code>
		// indicates any source).
		// @param notificationName The notification name to remove observers for.
		// (<code>null</code> indicates any notification name).
		// @param association The associated object (must not be null).  If the associated
		// object was wrapped in a {@link Reference} when it was registered, it should
		// not be wrapped here in this call.  (e.g) <br />
		// <pre><code>
		// NotificationCenter notifCenter = ...;
		// notifCenter.addObserver(<em>source</em>, <em>notifName</em>, <em>someObserver</em>, new WeakReference(<strong>this</strong>));
		// ...
		// notifCenter.removeObserverByAssociation( <em>source</em>, <em>notifName</em>, <strong>this</strong> ); // don't wrap association in reference here.
		// </code></pre>
		///
		public void RemoveObserver(Object source, String notificationName, Object association) {
			if ( association == null )
				throw new Exception("can't remove observers by association with a null assocation");
			if ( association.GetType() == typeof( WeakReference) )
				throw new Exception("Even if an association was added as a Reference, it should not be" +
					"removed as a Reference.  Just pass in the unwrapped association object directly, not a Reference to it.");

			InnerRemoveObserver(null,null,null,association);
		}


		/////
		// Posts a notification for any listeners.
		// @param notificationName The notification name (cannot be <code>null</code>)
		///
		public void PostNotification (string notificationName) {
			PostNotification (null, notificationName);
		}

		/////
		// Posts a notification for any listeners.
		// @param source The source of the notification (may be <code>null</code>
		// if no source is relevant).
		// @param notificationName The notification name (cannot be <code>null</code>)
		///
		public void PostNotification(Object source, String notificationName) {
			PostNotification(source,notificationName,null);
		}

		/////
		// Posts a notification for any listeners.
		// @param source The source of the notification (may be <code>null</code>
		// if no source is relevant).
		// @param notificationName The notification name (cannot be <code>null</code>)
		// @param extra Some extra parameters that are specific to the notification
		// name.  They can be any type that is appropriate.  Often times a
		// {@link Map} is a good choice, but any type could be used.
		///
		public void PostNotification(Object source, String notificationName, Object extra) {
			lock(this) {
				// We can't use iter.remove() to remove items, since it's possible we
				// might (through recursive calls) end up with a ConcurrentModificationException
				// to work around this we 'schedule' removals to occur at the end of
				// recursive calls
				bool handleRemove = false;
				HashSet<NotificationObserverRecord> itemsToRemove = itemsToDelete.Value;
				if ( itemsToRemove == null ) {
					itemsToRemove = new HashSet<NotificationObserverRecord>();
					itemsToDelete.Value = itemsToRemove;
					handleRemove = true;
				}

				IList<NotificationObserverRecord> observers;

				observers = notifButNoSourceObservers.ContainsKey(notificationName) ? notifButNoSourceObservers[notificationName] : null;
				if (observers != null && observers.Count > 0) {
					//Debug.WriteLine ("Posting Notification found " + observers.Count + " that match notification name only " + notificationName);
					InnerPostNotification (source, notificationName, extra, observers, itemsToRemove);
				}

				if (source != null && sourceButNoNotificationObservers.TryGetValue (source, out observers) && observers.Count > 0) {
					Debug.WriteLine ("Posting Notification found " + observers.Count + " that match notification and source " + notificationName);
					InnerPostNotification (source, notificationName, extra, observers, itemsToRemove);
				}
					
				WeakDictionary<object,IList<NotificationObserverRecord>> weakTable = sourceAndNotificationObservers.ContainsKey (notificationName) ? sourceAndNotificationObservers [notificationName] : null;
				if (weakTable != null && weakTable.TryGetValue(source, out observers) && observers != null && observers.Count > 0) {
					Debug.WriteLine ("Posting Notification found " + observers.Count + " that match source only. " + source);
					InnerPostNotification (source, notificationName, extra, observers, itemsToRemove);
				}

				observers = unrestrictedObservers;
				if (observers != null && observers.Count > 0) {
					//Debug.WriteLine ("Posting Notification found " + observers.Count + " that match all notifications");
					InnerPostNotification (source, notificationName, extra, observers, itemsToRemove);
				}

				if ( handleRemove ) {
					observers = notifButNoSourceObservers.ContainsKey (notificationName) ? notifButNoSourceObservers [notificationName] : null;
					if ( observers != null ) {
						foreach (NotificationObserverRecord rec in itemsToRemove)
							observers.Remove (rec);
						
						if ( observers.Count == 0 )
							notifButNoSourceObservers.Remove(notificationName);
					}

					if ( source != null && sourceButNoNotificationObservers.TryGetValue (source, out observers) && observers.Count > 0 ) {
						foreach (NotificationObserverRecord rec in itemsToRemove)
							observers.Remove (rec);
						if ( observers.Count == 0 )
							sourceButNoNotificationObservers.Remove( source );
					}

					weakTable = sourceAndNotificationObservers.ContainsKey (notificationName) ? sourceAndNotificationObservers [notificationName] : null;
					if ( weakTable != null && weakTable.TryGetValue(source, out observers) && observers != null && observers.Count > 0 ) {
						foreach (NotificationObserverRecord rec in itemsToRemove)
							observers.Remove (rec);
					}

					observers = unrestrictedObservers;
					foreach (NotificationObserverRecord rec in itemsToRemove)
						observers.Remove (rec);

					itemsToDelete.Remove();

					//Debug.WriteLine (this.observers.Count + " left after remove");
				}
			}
		}

		/////
		// Internal method that handles the post.  If it finds any observers that are obsolete
		// it schedules them for removal by adding them to the <em>itemsToRemove</em>
		// set.
		// @param source The source of the notification (may be <code>null</code>
		// if no source is relevant).
		// @param notificationName The notification name (cannot be <code>null</code>)
		// @param extra Some extra parameters that are specific to the notification
		// name.  They can be any type that is appropriate.  Often times a
		// {@link Map} is a good choice, but any type could be used.
		// @param itemsToRemove Set of pending observers to remove, this method may
		// add to this set.
		///
		void InnerPostNotification(Object source, String notificationName, Object extra, IList<NotificationObserverRecord> observers, HashSet<NotificationObserverRecord> itemsToRemove) {
			lock ( this ) {
				if ( notificationName == null )
					throw new Exception("notificationName cannot be null when posting a notification");

				NotificationObserverRecord[] observersArray = (observers as List<NotificationObserverRecord>).ToArray ();
				foreach ( NotificationObserverRecord record in observersArray ) {
					if ( itemsToRemove.Contains(record))
						continue;

					Notification notif = null;
					if ( !record.InError && record.RecordMatches(source, notificationName,true)) {
						if ( notif == null )
							notif = new Notification(source, notificationName, extra);
						try {
							NotificationObserverRecord.InternalRef aRef = record.ObserverRef;
							if ( !aRef.Exists ) {
								itemsToRemove.Add(record);
								continue;
							}

							Action<Notification> observer = aRef.GetAction;
							observer( notif );
						}
						catch (Exception t) {
							record.InError = true;

							Dictionary<String,Object> errExtra = new Dictionary<String,Object>();
							errExtra["source"] = record.Source;
							errExtra["notificationName"] = record.NotificationName;
							errExtra["observer"] = record.ObserverRef.GetAction;
							errExtra["throwable"] = t;
							InnerPostNotification(this, NOTIFICATION_OBSERVER_FAILED, errExtra, observers, itemsToRemove);

							record.InError = false;
						}
					}
				}	
			}
		}

		/////
		// Internal method for removing observers.  Convenience call that calls
		// {@link #innerRemoveObserver(Object, String, NotificationObserver, Object, Set)}
		// @param source The source (if <code>null</code> can be any source).
		// @param notificationName The notification name (if <code>null</code> can be any notification name)
		// @param observer The observer to remove (or <code>null</code> if could be any observer).
		// @param association The associated object to identify observers to remove (if
		// <code>null</code> don't use associations).
		///
		void InnerRemoveObserver(Object source, String notificationName, Action<Notification> observer, Object association) {
			lock(this) {
				if ( currentCullCount < cullFrequence )
					currentCullCount++;
				else {
					FindAndRemoveObsoleteObservers();
					currentCullCount = 0;
				}

				// We can't use iter.remove() to remove items, since it's possible we
				// might (through recursive calls) end up with a ConcurrentModificationException
				// to work around this we 'schedule' removals to occur at the end of
				// recursive calls
				bool handleRemove = false;
				HashSet<NotificationObserverRecord> itemsToRemove = itemsToDelete.Value;
				if ( itemsToRemove == null ) {
					itemsToRemove = new HashSet<NotificationObserverRecord>();
					itemsToDelete.Value = itemsToRemove;
					handleRemove = true;
				}

				HashSet<IList<NotificationObserverRecord>> groupOfObservers;

				groupOfObservers = notificationName == null ? new HashSet<IList<NotificationObserverRecord>>(notifButNoSourceObservers.Values) : notifButNoSourceObservers.ContainsKey(notificationName) ? ToHashSet(notifButNoSourceObservers[notificationName]) : null;
				if ( groupOfObservers != null && groupOfObservers.Count > 0 )
					foreach ( IList<NotificationObserverRecord> observers in groupOfObservers )
						InnerRemoveObserver(source, notificationName, observer, association, observers, itemsToRemove);

				groupOfObservers = source == null ? new HashSet<IList<NotificationObserverRecord>>(sourceButNoNotificationObservers.Values) : sourceButNoNotificationObservers.ContainsKey(source) ? ToHashSet(sourceButNoNotificationObservers[source]) : null;
				if ( groupOfObservers != null && groupOfObservers.Count > 0 )
					foreach ( List<NotificationObserverRecord> observers in groupOfObservers )
						InnerRemoveObserver(source, notificationName, observer, association, observers, itemsToRemove);

				groupOfObservers = null;
				if (notificationName == null) {
					HashSet<WeakDictionary<object,IList<NotificationObserverRecord>>> allWeakDictionaries = new HashSet<WeakDictionary<object,IList<NotificationObserverRecord>>> (sourceAndNotificationObservers.Values);
					foreach (WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary in allWeakDictionaries) {
						groupOfObservers = new HashSet<IList<NotificationObserverRecord>> (weakDictionary.Values);
						foreach (IList<NotificationObserverRecord> observers in groupOfObservers )
							InnerRemoveObserver (source, notificationName, observer, association, observers, itemsToRemove);
					}
				}
				else {
					WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary;
					if (sourceAndNotificationObservers.TryGetValue (notificationName, out weakDictionary)) {
						groupOfObservers = new HashSet<IList<NotificationObserverRecord>> (weakDictionary.Values);
						foreach (IList<NotificationObserverRecord> observers in groupOfObservers )
							InnerRemoveObserver (source, notificationName, observer, association, observers, itemsToRemove);
					}
				}

				InnerRemoveObserver(source, notificationName, observer, association, unrestrictedObservers, itemsToRemove);

				if ( handleRemove ) {
					groupOfObservers = notificationName == null ? new HashSet<IList<NotificationObserverRecord>>(notifButNoSourceObservers.Values) : notifButNoSourceObservers.ContainsKey(notificationName) ? ToHashSet(notifButNoSourceObservers[notificationName]) : null;
					if ( groupOfObservers != null )
						foreach ( List<NotificationObserverRecord> observers in groupOfObservers ) {
							foreach (NotificationObserverRecord rec in itemsToRemove)
								observers.Remove (rec);
						}

					groupOfObservers = source == null ? new HashSet<IList<NotificationObserverRecord>>(sourceButNoNotificationObservers.Values) : sourceButNoNotificationObservers.ContainsKey(source) ? ToHashSet(sourceButNoNotificationObservers[source]) : null;
					if ( groupOfObservers != null && groupOfObservers.Count > 0 )
						foreach ( List<NotificationObserverRecord> observers in groupOfObservers )
							foreach (NotificationObserverRecord rec in itemsToRemove)
								observers.Remove (rec);

					groupOfObservers = null;
					if (notificationName == null) {
						HashSet<WeakDictionary<object,IList<NotificationObserverRecord>>> allWeakDictionaries = new HashSet<WeakDictionary<object,IList<NotificationObserverRecord>>> (sourceAndNotificationObservers.Values);
						foreach (WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary in allWeakDictionaries) {
							groupOfObservers = new HashSet<IList<NotificationObserverRecord>> (weakDictionary.Values);
							foreach (IList<NotificationObserverRecord> observers in groupOfObservers )
								foreach (NotificationObserverRecord rec in itemsToRemove)
									observers.Remove (rec);
						}
					}
					else {
						WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary;
						if (sourceAndNotificationObservers.TryGetValue (notificationName, out weakDictionary)) {
							groupOfObservers = new HashSet<IList<NotificationObserverRecord>> (weakDictionary.Values);
							foreach (IList<NotificationObserverRecord> observers in groupOfObservers )
								foreach (NotificationObserverRecord rec in itemsToRemove)
									observers.Remove (rec);
						}
					}

					foreach (NotificationObserverRecord rec in (unrestrictedObservers as List<NotificationObserverRecord>).ToArray())
						unrestrictedObservers.Remove (rec);

					itemsToDelete.Remove();

					//Debug.WriteLine (this.observers.Count + " left after remove");
				}
			}
		}

		HashSet<IList<NotificationObserverRecord>> ToHashSet(IList<NotificationObserverRecord> list) {
			HashSet<IList<NotificationObserverRecord>> retVal = new HashSet<IList<NotificationObserverRecord>>();
			retVal.Add (list);

			return retVal;
		}

		/////
		// Internal method for removing observers.
		// @param source The source (if <code>null</code> can be any source).
		// @param notificationName The notification name (if <code>null</code> can be any notification name)
		// @param observer The observer to remove (or <code>null</code> if could be any observer).
		// @param association The associated object to identify observers to remove (if
		// <code>null</code> don't use associations).
		// @param itemsToRemove Set of pending observers to remove, this method may
		// add to this set.
		///
		void InnerRemoveObserver(Object source, String notificationName, Action<Notification> observer, Object associatedObject, IList<NotificationObserverRecord> observers, HashSet<NotificationObserverRecord> itemsToRemove) {
			lock(this) {
				bool useAssociatedObject = associatedObject != null;
				NotificationObserverRecord[] observersArray = (observers as List<NotificationObserverRecord>).ToArray ();
				foreach ( NotificationObserverRecord record in  observersArray) {
					NotificationObserverRecord.InternalRef recordAssociation = record.ObserverRef;
					if ( record.RecordMatches(source, notificationName, false) ) {
						if ( !record.ObserverRef.Exists || (record.Association.Target == null) ||
							(useAssociatedObject && (associatedObject == null || record.Association.Target == associatedObject)) ||
							(!useAssociatedObject && (observer == null || record.ObserverRef.RefersTo(observer))) ) {

							itemsToRemove.Add(record);

							Dictionary<String,Object> extra = new Dictionary<String,Object>();
							extra["source"] = record.Source;
							extra["notificationName"] = record.NotificationName;
							extra["observer"] = record.ObserverRef.GetAction;
							InnerPostNotification(this, NOTIFICATION_OBSERVER_REMOVED, extra, observers, itemsToRemove);
						}
					}
				}
			}
		}
			
		public void FindAndRemoveObsoleteObservers() {
			lock (this) {
				FindAndRemoveObsoleteObservers (notifButNoSourceObservers);
				sourceButNoNotificationObservers.RemoveCollectedEntries ();
				FindAndRemoveObsoleteObservers (sourceAndNotificationObservers);
				FindAndRemoveObsoleteObservers (unrestrictedObservers);
			}
		}

		void FindAndRemoveObsoleteObservers(Dictionary<string,IList<NotificationObserverRecord>> observers) {
			List<string> keyList = new List<string>(observers.Keys);
			foreach ( string key in keyList ) {
				if ( FindAndRemoveObsoleteObservers(observers[key]))
					observers.Remove(key);
			}
		}
			
		void FindAndRemoveObsoleteObservers(Dictionary<string,WeakDictionary<object,IList<NotificationObserverRecord>>> observers) {
			List<string> allkeys = new List<string>(observers.Keys);
			foreach (string key in allkeys) {
				WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary;
				if ( observers.TryGetValue(key, out weakDictionary)) {
					weakDictionary.RemoveCollectedEntries();
					if ( weakDictionary.Count == 0 )
						observers.Remove(key);
				}
			}
		}

		bool FindAndRemoveObsoleteObservers(IList<NotificationObserverRecord> observers) {
			NotificationObserverRecord[] observersArray = (observers as List<NotificationObserverRecord>).ToArray();
			foreach( NotificationObserverRecord record in observersArray ) {
				if ( !record.ObserverRef.Exists )
					observers.Remove(record);
			}

			return observers.Count == 0;
		}

		/**
	 	* Internal method used for testing, where all observers are returned as a single set.
	 	* @return The observers currently registered with this notification center.
	 	*/
		public HashSet<NotificationObserverRecord> observers {
			get {
				HashSet<NotificationObserverRecord> retVal = new HashSet<NotificationObserverRecord> ();

				List<IList<NotificationObserverRecord>> valuesCopy = new List<IList<NotificationObserverRecord>> (notifButNoSourceObservers.Values);
				foreach (IList<NotificationObserverRecord> observers in valuesCopy)
					foreach (NotificationObserverRecord record in observers)
						retVal.Add (record);

				valuesCopy = new List<IList<NotificationObserverRecord>> (sourceButNoNotificationObservers.Values);
				foreach (IList<NotificationObserverRecord> observers in valuesCopy)
					foreach (NotificationObserverRecord record in observers)
						retVal.Add (record);

				IList<WeakDictionary<object,IList<NotificationObserverRecord>>> weakList = new List<WeakDictionary<object,IList<NotificationObserverRecord>>> (sourceAndNotificationObservers.Values);
				foreach (WeakDictionary<object,IList<NotificationObserverRecord>> weakDictionary in weakList) {
					valuesCopy = new List<IList<NotificationObserverRecord>> (weakDictionary.Values);
					foreach (IList<NotificationObserverRecord> observers in valuesCopy) {
						if (observers != null) {
							foreach (NotificationObserverRecord record in observers)
								retVal.Add (record);
						}
					}
				}

				foreach (NotificationObserverRecord record in unrestrictedObservers)
					retVal.Add (record);

				return retVal;
			}
		}
	}

	/////
	// Object representing a posted notification.  This includes the following.
	// <ul>
	// <li>{@link #getSource() source} The Source of the event</li>
	// <li>{@link #getNotificationName() name} The notification name</li>
	// <li>{@link #getExtra() extra} Any extra arguments that are notification specific</li>
	// </ul>
	// @author bryant_harris
	///
	public class Notification {
		public Object Source { get; set; }
		public String NotificationName { get; set; }
		public Object Extra { get; set; }

		public Notification() {
			Source = null;
			NotificationName = null;
			Extra = null;
		}

		public Notification(Object s, String name) {
			Source = s;
			NotificationName = name;
			Extra = null;
		}

		public Notification(Object s, String name, Object e) {
			Source = s;
			NotificationName = name;
			Extra = e;
		}

		public override string ToString () {
			return NotificationName;
		}
	}

	/////
	// Internal class used by {@link NotificationCenter} to track the registration
	// info of a particular observer.
	// @author bryant_harris
	///
	public class NotificationObserverRecord {
		WeakReference sourceRef;
		public object Source { 
			get {
				return sourceRef == null ? null : sourceRef.Target;
			}

			set {
				sourceRef = value == null ? null : new WeakReference (value);
			}
		}
		public String NotificationName { get; set; }
		public InternalRef ObserverRef { get; set; }
		public WeakReference Association { get; set; }
		public bool InError { get; set; }

		public NotificationObserverRecord(Object theSource, String notifName, Action<Notification> obs, object assoc) {
			InError = false;

			Source = theSource;
			NotificationName = notifName;
			ObserverRef = new BasicInternalRef (obs);
			Association = new WeakReference (assoc);
		}

		public NotificationObserverRecord(Object theSource, String notifName, WeakDelegateProxy proxy) {
			InError = false;

			Source = theSource;
			NotificationName = notifName;
			ObserverRef = new ProxyRef (proxy);
			Association = proxy._targetReference;
		}

		public bool MatchesNotificationOnly {
			get {
				return NotificationName != null && Source == null;
			}
		}

		public bool MatchesSourceOnly {
			get {
				return NotificationName == null && Source != null;
			}
		}

		public bool MatchesSourceAndNotification {
			get {
				return NotificationName != null && Source != null;
			}
		}

		public bool MatchesEverything {
			get {
				return NotificationName == null && Source == null;
			}
		}

		/////
		// Method determines if there's a match between this record and the supplied
		// parameters.  Either this records values or the supplied values are used
		// as the matching template depending on the value of <em>useRecordAsTemplate</em>.
		// @param aSource The source to match with.
		// @param notifName The notification name to match with.
		// @param useRecordAsTemplate <code>null</code> values imply a wild card match
		// there fore it's important to know are we using the records values as
		// the template values to match with or are we using the supplied values as
		// templates.  The value indicates which approach to take.  When removing
		// records we tend to match records based on the supplied values.  When we
		// are posting notifications we tend to match the records values to that
		// of the notifications.
		// @return
		///
		public bool RecordMatches(Object aSource, String notifName, bool useRecordAsTemplate) {
			Object templateSource;
			Object compareSource;
			String templateNotificationName;
			String compareNotificationName;
			if ( useRecordAsTemplate ) {
				templateSource = Source;
				compareSource = aSource;
				templateNotificationName = NotificationName;
				compareNotificationName = notifName;
			}
			else {
				templateSource = aSource;
				compareSource = Source;
				templateNotificationName = notifName;
				compareNotificationName = NotificationName;
			}

			if ( templateSource != null ) {
				if ( compareSource == null)
					return false;

				if ( templateSource != compareSource && !templateSource.Equals(compareSource))
					return false;
			}

			if ( templateNotificationName != null ) {
				if ( compareNotificationName == null )
					return false;

				if ( templateNotificationName != compareNotificationName && !templateNotificationName.Equals(compareNotificationName))
					return false;
			}

			return true;
		}

		public override string ToString () {
			return "source: " + Source + " notificationName: " + NotificationName;
		}

		/////
		// Common interface to refer to our observers with, regardless of whether
		// we have a hard ref or a java <code>Reference</code> to our observer
		// @author bryant_harris
		///
		public interface InternalRef {
			Action<Notification> GetAction { get; }
			bool Exists { get; }
			bool RefersTo (Action<Notification> other);
		}

		/////
		// A wrapper over an observer.
		// @author bryant_harris
		///
		class BasicInternalRef : InternalRef {
			public BasicInternalRef(Action<Notification> obs) {
				GetAction = obs;
			}

			public Action<Notification> GetAction { get; set; }
				
			public bool Exists { get { return true; } }

			public bool RefersTo (Action<Notification> other) {
				return GetAction.Equals(other);
			}
		}

		/////
		// A wrapper over a Weak Proxy
		// @author bryant_harris
		///
		class ProxyRef : InternalRef {
			WeakDelegateProxy proxy { get; set; }
			public ProxyRef(WeakDelegateProxy r) {
				proxy = r;
			}

			public Action<Notification> GetAction { get { return HandleNotification; } }

			public bool Exists { get { return proxy.TargerExists; }}

			public bool RefersTo (Action<Notification> other) {
				object ourTarget = proxy._targetReference.Target;
				if (ourTarget == null)
					return false;
				MethodInfo ourMethod = proxy._method;

				object otherTarget = other.Target;
				MethodInfo otherMethod = other.Method;
				bool methods = otherMethod.Equals (ourMethod);
				bool  target = otherTarget == ourTarget;

				return methods && target;
			}

			void HandleNotification(Notification n) {
				proxy.HandleEvent<Notification> (n);
			}
		}
	}

	class ThreadLocal<T> {
		Dictionary<Thread,T> locals;
		public T Value {
			get {
				return (locals.ContainsKey (Thread.CurrentThread )) ? locals [Thread.CurrentThread] : default(T);
			}

			set {
				locals [Thread.CurrentThread] = value;
			}
		}

		public void Remove() {
			locals.Remove (Thread.CurrentThread);
		}

		public ThreadLocal() {
			locals = new Dictionary<Thread,T>();
		}
	}
}

