using System;

namespace em {
	public interface WebsocketConnectionFactory {
		WebsocketConnection BuildConnection ();
	}
}

