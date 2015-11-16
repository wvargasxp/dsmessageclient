using System;
using System.Collections.Generic;

namespace em {
	public interface IMediaMessagesProvider {
		IList<Message> GetMediaMessages ();
		int GetCurrentPage ();
		void RequestMoreMediaMessages (Message message);
		bool HasMoreMediaMessagesToRequest ();
		void UpdateLastSeenMediaMessage (Message message);
	}
}

