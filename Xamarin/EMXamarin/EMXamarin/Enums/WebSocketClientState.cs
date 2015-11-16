using System;

// A copy of the WebSocketState enum class from Websocket4net.
// https://github.com/kerryjiang/WebSocket4Net/blob/master/WebSocket4Net/WebSocketState.cs

namespace em {
	public enum WebsocketClientState {
		Started,
		Stopped,
		Disposed,
	}
}