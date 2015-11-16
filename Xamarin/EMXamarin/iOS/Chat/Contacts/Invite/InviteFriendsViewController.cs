using System;
using em;
using Foundation;
using MessageUI;
using UIKit;
using GoogleAnalytics.iOS;

namespace iOS {
	public class InviteFriendsViewController : AddressBookViewController {
		private MFMessageComposeViewController SMS { get; set; }
		private MFMailComposeViewController Email { get; set; }

		public AbstractInviteFriendsController SharedInvite { get; set; }

		public InviteFriendsViewController (ApplicationModel appModel, AddressBookArgs args) : base (args) {
			this.SharedInvite = new SharedInviteFriendsController (appModel, this);
		}

		public override void ViewDidLoad () {
			base.ViewDidLoad ();

			var leftButton = new UIBarButtonItem ("CLOSE".t (), UIBarButtonItemStyle.Done, WeakDelegateProxy.CreateProxy<object,EventArgs>( (sender, args) => DismissViewController (true, null)).HandleEvent<object,EventArgs>);
			leftButton.SetTitleTextAttributes (FontHelper.DefaultNavigationAttributes(), UIControlState.Normal);
			this.NavigationItem.SetLeftBarButtonItem(leftButton, true);
		}

		public override void ViewDidAppear (bool animated) {
			base.ViewDidAppear (animated);

			GAI.SharedInstance.DefaultTracker.Set (GAIConstants.ScreenName, "Invite Friends View");

			GAI.SharedInstance.DefaultTracker.Send (GAIDictionaryBuilder.CreateScreenView ().Build ());
		}

		public override void FinishSelectingContact (AddressBookSelectionResult result) {
			this.SharedInvite.Result = result;
			this.Clear ();

			if (!this.SharedInvite.HasSmsContacts) {
				RunSendEmailFlow ();
			} else {
				RunSendSmsFlow ();
			}
		}

		private void HandleNoSmsCallback () {
			RunSendEmailFlow ();
		}

		private void HandleNoEmailCallback () {
			this.DismissViewController (true, null);
		}

		private void RunSendSmsFlow () {
			AbstractInviteFriendsController inv = this.SharedInvite;
			if (!MFMessageComposeViewController.CanSendText) {
				UserPrompter.PromptUserWithActionNoNegative (inv.CantSendSMSTitle, inv.CantSendSMSBody, HandleNoSmsCallback);
				return;
			}

			string[] smsRecipients = this.SharedInvite.SMSRecipients;

			this.SMS = new MFMessageComposeViewController ();
			this.SMS.Finished += WeakDelegateProxy.CreateProxy<object, MFMessageComposeResultEventArgs> (SmsFinished).HandleEvent<object, MFMessageComposeResultEventArgs>;

			this.SMS.Recipients = smsRecipients;
			this.SMS.Body = inv.MessageBodySMS;

			this.PresentViewController (this.SMS, true, null);
		}

		private void RunSendEmailFlow () {
			AbstractInviteFriendsController inv = this.SharedInvite;

			if (!inv.HasEmailContact) {
				HandleNoEmailCallback ();
			} else {
				if (!MFMailComposeViewController.CanSendMail) {
					UserPrompter.PromptUserWithActionNoNegative (inv.CantSendEmailTitle, inv.CantSendEmailBody, HandleNoEmailCallback);
					return;
				}

				this.Email = new MFMailComposeViewController ();
				this.Email.Finished += WeakDelegateProxy.CreateProxy<object, MFComposeResultEventArgs> (EmailFinished).HandleEvent<object, MFComposeResultEventArgs>;
				this.Email.SetSubject (inv.EmailSubject);
				this.Email.SetMessageBody (inv.MessageBodyEmail, true);
				string[] emailRecipients = inv.EmailRecipients;
				this.Email.SetToRecipients (emailRecipients);
				this.PresentViewController (this.Email, true, null);
			}
		}

		public void SmsFinished (object sender, MFMessageComposeResultEventArgs e) {
			AbstractInviteFriendsController inv = this.SharedInvite;
			MessageComposeResult res = e.Result;
			switch (res) {
			case MessageComposeResult.Cancelled: 
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.CancelTitle, inv.CancelBody, AfterSmsResult);
					break;
				}
			case MessageComposeResult.Failed:
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.ErrorTitle, inv.ErrorBody, AfterSmsResult);
					break;
				}
			case MessageComposeResult.Sent: 
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.SentTitle, inv.SentBody, AfterSmsResult);
					break;
				}
			}
		}

		public void EmailFinished (object sender, MFComposeResultEventArgs e) {
			AbstractInviteFriendsController inv = this.SharedInvite;
			NSError err = e.Error;
			if (err != null) {
				UserPrompter.PromptUserWithActionNoNegative (inv.ErrorTitle, inv.ErrorBody, AfterEmailResult);
				return;
			}

			MFMailComposeResult res = e.Result;
			switch (res) {
			case MFMailComposeResult.Cancelled: 
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.CancelTitle, inv.CancelBody, AfterEmailResult);
					break;
				}
			case MFMailComposeResult.Failed:
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.ErrorTitle, inv.ErrorBody, AfterEmailResult);
					break;
				}
			case MFMailComposeResult.Saved: 
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.SavedTitle, inv.SavedBody, AfterEmailResult);
					break;
				}
			case MFMailComposeResult.Sent: 
				{
					UserPrompter.PromptUserWithActionNoNegative (inv.SentTitle, inv.SentBody, AfterEmailResult);
					break;
				}
			}
		}

		private void AfterSmsResult () {
			this.DismissViewController (true, () => {
				RunSendEmailFlow ();
			});
		}

		private void AfterEmailResult () {
			this.DismissViewController (true, null);
		}
	}

	class SharedInviteFriendsController : AbstractInviteFriendsController {
		private WeakReference _r = null;
		private InviteFriendsViewController Self {
			get { return this._r != null ? this._r.Target as InviteFriendsViewController : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedInviteFriendsController (ApplicationModel appModel, InviteFriendsViewController self) : base(appModel) {
			this.Self = self;
		}
	}
}