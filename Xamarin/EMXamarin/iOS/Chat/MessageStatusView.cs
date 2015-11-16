using CoreGraphics;
using UIKit;
using em;

namespace iOS {
	public class MessageStatusView : UIView {
		MessageStatus ms;
		public MessageStatus messageStatus { 
			get {
				return ms;
			}

			set {
				ms = value;
				DidSetMessageStatus ();
			}
		}

		readonly UIImageView messageStatusImageView;

		public MessageStatusView () {
			Frame = new CGRect (0, 0, 26, 12);
			messageStatusImageView = new UIImageView ();
			messageStatusImageView.Frame = Frame;
			AddSubview (messageStatusImageView);
		}

		bool showDebugMessageStatusUpdates = false;
		protected void DidSetMessageStatus () {
			if (showDebugMessageStatusUpdates || AppDelegate.Instance.applicationModel.ShowVerboseMessageStatusUpdates) {
				switch (messageStatus) {
				default:
				case MessageStatus.pending:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/pending.png");
					break;
				case MessageStatus.sent: 
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/sent.png");
					break;
				case MessageStatus.delivered:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/envelope.png");
					break;
				case MessageStatus.failed:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/failed.png");
					break;
				case MessageStatus.ignored:
				case MessageStatus.read:
					messageStatusImageView.Image = null;
					break;
				}
			} else {
				switch (messageStatus) {
				default:
				case MessageStatus.pending:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/sent.png");
					break;

				case MessageStatus.sent: 
				case MessageStatus.delivered:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/envelope.png");
					break;

				case MessageStatus.failed:
					messageStatusImageView.Image = ImageSetter.GetResourceImage ("chat/failed.png");
					break;
				case MessageStatus.ignored:
				case MessageStatus.read:
					messageStatusImageView.Image = null;
					break;
				}
			}

		}
	}
}