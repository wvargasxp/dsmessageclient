namespace em {
	
	public class MoveOrInsertInstruction<T> {
		
		/* NOTE: for bulk updates (missed messages), we set the position after we process all the updates */
		// from position of -1 means add
		public int FromPosition { get; set; }
		// to position of -1 means remove
		public int ToPosition { get; set; }

		public T Entry;

		public MoveOrInsertInstruction(T c) {
			Entry = c;
		}

		public MoveOrInsertInstruction(int from, int to) {
			FromPosition = from;
			ToPosition = to;
		}

		public bool IsAdd() {
			return FromPosition == -1;
		}

		public bool IsRemove() {
			return ToPosition == -1;
		}

		public bool IsMove() {
			return FromPosition != -1 && ToPosition != -1;
		}

		public override string ToString () {
			return string.Format ("[MoveOrInsertInstruction: FromPosition={0}, ToPosition={1}, ChatEntry={2}]", FromPosition, ToPosition, Entry);
		}
	}
}