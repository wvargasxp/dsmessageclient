using System;
using System.Collections.Generic;

namespace em {
	public class MoveOrInsertInstruction {
		// from position of -1 means add
		public int fromPosition { get; set; }
		// to position of -1 means delete
		public int toPosition { get; set; }

		public MoveOrInsertInstruction(int f, int t) {
			fromPosition = f;
			toPosition = t;
		}
	}

	/**
	 * Object that encapsulates the standard behavior of an inbox
	 * (without having any actual view logic of it's own).  This abstract class defines
	 * several callbacks to implement to respond to various UI related events.
	 *
	 * To facilitate animating of updates this class also batches up updates
	 * and only submits them when the view is prepared to handle them.
	 */
	public abstract class AbstractInBoxView {
		public ChatList chatList { get; set; }

		bool suspendingUpdates;
		IList<MoveOrInsertInstruction> pendingMovesOrInserts;
		IList<int> pendingPreviewUpdates;

		public AbstractInBoxView () {
			chatList = ChatList.GetInstance ();

			pendingMovesOrInserts = new List<MoveOrInsertInstruction> ();
			pendingPreviewUpdates = new List<int> ();

			chatList.DelegateDidAdd += DidAddChatEntry;
			chatList.DelegateDidRemove += DidRemoveChatEntry;
			chatList.DelegateDidMove += DidMoveChatEntry;
			chatList.DelegateDidChangePreview += DidChangePreviewAt;
		}

		/**
		 * Must be called when the inbox is being destroyed.
		 */
		public void Dispose() {
			chatList.DelegateDidAdd -= DidAddChatEntry;
			chatList.DelegateDidRemove -=  DidRemoveChatEntry;
			chatList.DelegateDidMove -= DidMoveChatEntry;
			chatList.DelegateDidChangePreview -= DidChangePreviewAt;
		}

		/**
		 * Called during animation so new updates won't be forwarded
		 * to the view layer.
		 */
		public void SuspendUpdates() {
			suspendingUpdates = true; 
		}

		/**
		 * Called after animations complete to allow the resumptions of
		 * events being sent to the view layer.
		 */
		public void ResumeUpdates() {
			suspendingUpdates = false;

			if (pendingMovesOrInserts.Count > 0 || pendingPreviewUpdates.Count > 0) {
				HandleUpdatesToChatList (pendingMovesOrInserts, pendingPreviewUpdates);
				pendingMovesOrInserts.Clear ();
				pendingPreviewUpdates.Clear ();
			}
		}

		/**
		 * Callback that is used to tell the inbox it has either new chat entries or
		 * the status of some chat entries has changed.
		 */
		public abstract void HandleUpdatesToChatList(IList<MoveOrInsertInstruction> repositionChatItems, IList<int> previewUpdates);

		protected void DidAddChatEntry(int addedPosition) {
			if (suspendingUpdates)
				pendingMovesOrInserts.Add (new MoveOrInsertInstruction (-1, addedPosition));
			else {
				List<MoveOrInsertInstruction> list = new List<MoveOrInsertInstruction> ();
				list.Add(new MoveOrInsertInstruction (-1, addedPosition));
				HandleUpdatesToChatList (list, new List<int> ());
			}
		}

		protected void DidRemoveChatEntry(int removePosition) {
			if (suspendingUpdates)
				pendingMovesOrInserts.Add (new MoveOrInsertInstruction (removePosition, -1));
			else {
				List<MoveOrInsertInstruction> list = new List<MoveOrInsertInstruction> ();
				list.Add(new MoveOrInsertInstruction (removePosition, -1));
				HandleUpdatesToChatList (list, new List<int> ());
			}
		}

		protected void DidMoveChatEntry(int originalPosition, int newPosition) {
			if (suspendingUpdates)
				pendingMovesOrInserts.Add (new MoveOrInsertInstruction (originalPosition, newPosition));
			else {
				List<MoveOrInsertInstruction> list = new List<MoveOrInsertInstruction> ();
				list.Add(new MoveOrInsertInstruction (originalPosition, newPosition));
				HandleUpdatesToChatList (list, new List<int> ());
			}
		}

		protected void DidChangePreviewAt(int position) {
			if (suspendingUpdates)
				pendingPreviewUpdates.Add (position);
			else {
				List<int> list = new List<int> ();
				list.Add(position);
				HandleUpdatesToChatList (new List<MoveOrInsertInstruction> (), list);
			}
		}
	}
}

