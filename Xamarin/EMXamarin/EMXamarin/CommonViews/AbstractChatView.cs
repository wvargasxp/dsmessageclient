using System;
using System.Threading;
using System.Collections.Generic;

namespace em {
	/**
	 * Object that encapsulates the standard behavior of a chat conversation view
	 * (without having any actual view logic of it's own).  This abstract class defines
	 * several callbacks to implement to respond to various UI related events.
	 * 
	 * It also provides several helper methods that automate working with the model
	 * layer to perform actions requiried for chat.
	 */
	public abstract class AbstractChatView {
		public ChatList chatList { get; set; }
		public ChatEntry chatEntry { get; set; }
		public IList<Message> messages { get; set; }
		public bool displayingTypingRow;

		public const int SHOW_TYPING_DELAY_MILLIS = 3000;

		bool viewVisible;

		public AbstractChatView (ChatEntry ce) {
			displayingTypingRow = false;

			chatList = ChatList.GetInstance ();
			chatEntry = ce;

			viewVisible = false;

			messages = (IList<em.Message>) chatEntry.cachedConversation.Target;
			if (messages == null) {
				chatEntry.LoadConversationAsync((IList<em.Message> msgs) => {
					messages = msgs;
					DidFinishLoadingMessages();
				} );
			}

			chatEntry.DelegateDidAddMessage += DidAddMessageAt;
			chatEntry.DelegateDidChangeStatusOfMessage += DidChangeStatusOfMessage;
			chatEntry.DelegateDidReceiveTypingMessage += DidReceiveTypingMessage;
		}

		/**
		 * Must be called when exiting a chat conversation
		 */
		public void Dispose() {
			chatEntry.DelegateDidAddMessage -= DidAddMessageAt;
			chatEntry.DelegateDidChangeStatusOfMessage -= DidChangeStatusOfMessage;
			chatEntry.DelegateDidReceiveTypingMessage -= DidReceiveTypingMessage;
		}

		/**
		 * Messages may not be available immediately and are loaded in a background thread
		 * if this callback is loaded the view layer should essentially reload itself (if
		 * it already exists).  If the messages were available on view creation this may
		 * not be called.
		 */
		public abstract void DidFinishLoadingMessages();

		/**
		 * Called if this was a new conversation session where an option to add contacts
		 * is visible.  The view layer should hide this option when this is called.
		 */
		public abstract void HideAddContactsOption (bool animated);

		/**
		 * Called in response to a message getting sent.  The view should clear it's text
		 * entry area as it's now part of the conversation.
		 */
		public abstract void ClearTextEntryArea();

		/**
		 * For views where the user is adding contacts, this callback indicates that
		 * it should update it's contacts view.
		 */
		public abstract void UpdateToContactsView ();

		/**
		 * Indicates messages where added and this view should animate them into place
		 */
		public abstract void MessageAddedAt (int position);

		/**
		 * Indicates the status (sent/delivered) for a message was updated and the view should
		 * update this change.
		 */
		public abstract void MessageStatusChangedAt(int position);

		/**
		 * Indicates the UI should show that a specific user is typing
		 */
		public abstract void ShowContactIsTyping (Contact contact);

		/**
		 * Indicates the UI should stop showing that a specific user is typing
		 */
		public abstract void HideContactIsTyping ();

		/**
		 * helper routine the view can use to update the under construction string
		 */
		public void UpdateUnderConstructionText(string text) {
			chatEntry.underConstruction = text;
			chatEntry.Save();
			chatEntry.Typing();
		}

		/**
		 * Helper routine for sending a text based message.  The View will
		 * receive updates to add the message into the view.
		 */
		public void SendTextMessage(string text) {
			if ( !chatEntry.isPersisted ) {
				chatEntry.Save();
				chatList.underConstruction = null;

				HideAddContactsOption (true);
			}

			Message message = Message.NewMessage();
			message.chatEntry = chatEntry;
			message.chatEntryID = chatEntry.chatEntryId;
			message.inbound = "N";
			message.readDate = DateTime.MinValue;
			message.deliveredDate = DateTime.MinValue;
			message.message = text;

			chatEntry.AddMessageAsync(message);

			ClearTextEntryArea ();
		}

		public bool ShowContactsInitially() {
			return !chatEntry.isPersisted;
		}

		/**
		 * Helper routine indicating that the user
		 * has added a new contact to the chat entry
		 * (only allowed prior to a message getting sent)
		 */
		public void AddContactToReplyTo(Contact newContact) {
			chatEntry.contacts.Add (newContact);

			UpdateToContactsView ();
		}

		/**
		 * Helper method indicating that the view has become
		 * visible to the user
		 */
		public void ViewBecameVisible() {
			viewVisible = true;
			chatEntry.MarkAllReadAsync ();
		}

		/**
		 * Helper method indicating that the view is nolonger
		 * visible to the user.
		 */
		public void ViewBecameHidden() {
			viewVisible = false;
		}

		protected void DidAddMessageAt(int index) {
			if ( viewVisible )
				chatEntry.MarkAllReadAsync ();

			MessageAddedAt (index);
		}

		protected void DidChangeStatusOfMessage(int index) {
			MessageStatusChangedAt (index);
		}

		DateTime timerCreationTime;
		Timer showTypingAnimationTimer;
		protected void DidReceiveTypingMessage(Contact contact) {
			lock (this) {
				if (showTypingAnimationTimer != null) {
					ScheduleHideTypingTimer ();
				}
				else {
					displayingTypingRow = true;
					ShowContactIsTyping (contact);

					ScheduleHideTypingTimer ();
				}
			}
		}

		protected void ScheduleHideTypingTimer() {
			timerCreationTime = DateTime.Now;
			showTypingAnimationTimer = new Timer ((object o) => {
					ChatList.appModel.platformFactory.runOnMainThread (() => {
						lock (this) {
							// we make sure this timer firing is the 
							if ( timerCreationTime.Equals(o)) {
								showTypingAnimationTimer = null;
								displayingTypingRow = false;
								HideContactIsTyping();
							}
						}
					});
			}, timerCreationTime, SHOW_TYPING_DELAY_MILLIS, Timeout.Infinite);
		}
	}
}