using System;
using System.Collections.Generic;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Telephony;
using Android.Widget;
using em;

namespace Emdroid {
	public class InviteFriendsFragment : AddressBookFragment {

		private HiddenReference<ApplicationModel> _appModel;
		private ApplicationModel AppModel {
			get { return this._appModel != null ? this._appModel.Value : null; }
			set { this._appModel = new HiddenReference<ApplicationModel> (value); }
		}

		private HiddenReference<AbstractInviteFriendsController> _sharedInvite;
		public AbstractInviteFriendsController SharedInvite { 
			get { return this._sharedInvite != null ? this._sharedInvite.Value : null; }
			set { this._sharedInvite = new HiddenReference<AbstractInviteFriendsController> (value); }
		} 

		private bool OpenedSmsIntent { get; set; }

		private SmsManager SMS {
			get {
				return SmsManager.Default;
			}
		}

		public static InviteFriendsFragment NewInstance (ApplicationModel appModel, AddressBookArgs args) {
			InviteFriendsFragment f = new InviteFriendsFragment ();
			f.Args = args;
			f.AppModel = appModel;
			f.OpenedSmsIntent = false;
			return f;
		}

		public InviteFriendsFragment () {
			this.SharedInvite = new SharedInviteFriendsController (AppModel, this);
		}

		public override void OnActivityCreated (Bundle savedInstanceState) {
			base.OnActivityCreated (savedInstanceState);

			AnalyticsHelper.SendView ("Invite Friends View");
		}

		public override void OnResume () {
			base.OnResume ();

			if (this.OpenedSmsIntent) {
				this.OpenedSmsIntent = false;
				RunSendEmailFlow ();
			}
		}

		public override void OnPause () {
			base.OnPause ();
		}

		public override void OnCreate (Bundle savedInstanceState) {
			base.OnCreate (savedInstanceState);
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

		private void RunSendSmsFlow () {
			AbstractInviteFriendsController inv = this.SharedInvite;

			bool hasSms = this.Activity.PackageManager.HasSystemFeature (PackageManager.FeatureTelephony);
			if (!hasSms) {
				UserPrompter.PromptUserWithActionNoNegative (inv.CantSendSMSTitle, inv.CantSendSMSBody, HandleNoSmsCallback, this.Activity);
				return;
			}

			UserPrompter.PromptUserWithAction ("INVITE_FRIENDS_TITLE".t (), "INVITE_FRIENDS_VIA_SMS_MESSAGE".t (), "YES".t (), HandleSmsAction, HandleNoSmsCallback, this.Activity);
		}

		private void RunSendEmailFlow () {
			AbstractInviteFriendsController inv = this.SharedInvite;
			if (!inv.HasEmailContact) {
				HandleNoEmailCallback ();
			} else {
				UserPrompter.PromptUserWithAction ("INVITE_FRIENDS_TITLE".t (), "INVITE_FRIENDS_VIA_EMAIL_MESSAGE".t (), "YES".t (), HandleEmailAction, HandleNoEmailCallback, this.Activity);
			}
		}

		private void HandleEmailAction () {
			AbstractInviteFriendsController inv = this.SharedInvite;
			string[] emailRecipients = inv.EmailRecipients;
			Intent i = new Intent (Intent.ActionSend);
			i.SetType ("message/rfc822");
			i.PutExtra (Intent.ExtraEmail, emailRecipients);
			i.PutExtra (Intent.ExtraSubject, inv.EmailSubject);
			i.PutExtra (Intent.ExtraText, inv.MessageBodySMS);
			i.PutExtra (Intent.ExtraHtmlText, inv.MessageBodyEmail);

			try {
				this.Activity.StartActivity (CreateEmailOnlyChooserIntent (i, "INVITE_FRIENDS_SEND_EMAIL_LABEL".t ()));
			} catch (ActivityNotFoundException ex) {
				System.Diagnostics.Debug.WriteLine ("Exception when sending email invite! " + ex.Message);
				UserPrompter.PromptUserWithActionNoNegative (inv.ErrorTitle, "INVITE_FRIENDS_ERROR_MESSAGE".t (), HandleNoEmailCallback, this.Activity);
			}
		}

		private void HandleSmsAction () {

			EMTask.DispatchBackground (() => {
				AbstractInviteFriendsController inv = this.SharedInvite;
				string[] recipients = inv.SMSRecipients;

				int count = recipients.Length;
				string body = this.SharedInvite.MessageBodySMS;

				for (int i = 0; i< count; i++) {
					int g = i;
					EMTask.DispatchMain (() => {
						Toast toast = Toast.MakeText (this.Activity, string.Format ("INVITE_FRIENDS_SMS_SENDING_TOAST_MESSAGE".t (), g+1, count), ToastLength.Short);
						toast.Show ();
					});

					string description = recipients [i];
					this.SMS.SendTextMessage (description, null, body, null, null);
				}

				EMTask.DispatchMain (() => {
					UserPrompter.PromptUserWithActionNoNegative (inv.SentTitle, inv.SentBody, RunSendEmailFlow, this.Activity);	
				});
			});

//			StringBuilder builder = new StringBuilder ();
//			builder.Append ("sms:");
//			for (int i = 0; i<count; i++) {
//				string description = recipients [i];
//				builder.Append (description);
//
//				if (count > 1 && i != count - 1) {
//					builder.Append (", ");
//				}
//			}
//
//			string recipientString = builder.ToString ();
//			Android.Net.Uri data = Android.Net.Uri.Parse (recipientString);
//
//			Intent smsIntent = new Intent (Intent.ActionView);
//			smsIntent.SetType ("vnd.android-dir/mms-sms");
//			smsIntent.SetData (data);
//			smsIntent.PutExtra ("sms_body", body);
//
//
//			// Set this flag so we know to try to send out emails when we come back to the app.
//			this.OpenedSmsIntent = true;
//
//			try {
//				this.Activity.StartActivity (smsIntent);
//			} catch (ActivityNotFoundException ex) {
			//	UserPrompter.PromptUserWithActionNoNegative (inv.FailTitle, "INVITE_FRIENDS_ERROR_MESSAGE".t (), RunSendEmailFlow, this.Activity);
//			}
		}

		private void HandleNoSmsCallback () {
			RunSendEmailFlow ();
		}

		private void HandleNoEmailCallback () {
			this.FragmentManager.PopBackStack ();
		}

		private Intent CreateEmailOnlyChooserIntent (Intent source, string chooserTitle) {
			Stack<Intent> intents = new Stack<Intent> ();
			Intent i = new Intent (Intent.ActionSendto, Android.Net.Uri.FromParts ("mailto", "info@domain.com", null));
			IList<ResolveInfo> activities = this.Activity.PackageManager.QueryIntentActivities (i, 0);

			foreach (ResolveInfo ri in activities) {
				Intent target = new Intent (source);
				target.SetPackage (ri.ActivityInfo.PackageName);
				intents.Push (target);
			}

			if (intents.Count != 0) {
				Intent chooserIntent = Intent.CreateChooser (intents.Pop (), chooserTitle);
				chooserIntent.PutExtra (Intent.ExtraInitialIntents, intents.ToArray ());
				return chooserIntent;
			} else {
				return Intent.CreateChooser (source, chooserTitle);
			}
		}
	}

	class SharedInviteFriendsController : AbstractInviteFriendsController {
		private WeakReference _r = null;
		private InviteFriendsFragment Self {
			get { return this._r != null ? this._r.Target as InviteFriendsFragment : null; }
			set { this._r = new WeakReference (value); }
		}
		public SharedInviteFriendsController (ApplicationModel appModel, InviteFriendsFragment self) : base(appModel) {
			this.Self = self;
		}
	}
}