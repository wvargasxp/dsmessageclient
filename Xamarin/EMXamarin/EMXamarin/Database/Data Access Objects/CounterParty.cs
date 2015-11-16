using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace em {
	public class CounterParty : BaseDataR {

		~CounterParty() {
			NotificationCenter.DefaultCenter.RemoveObserver (this);
		}

		protected string dn;
		public string displayName { 
			get { return dn; }
			set {
				string previous = dn;
				dn = value;

				// 1. previous doesn't equal dn
				// 2. previous is null and is getting a value
				bool shouldCallDelegate = previous != null && !previous.Equals (dn) | previous == null && dn != null;
				if(shouldCallDelegate)
					EMTask.DispatchMain (() => DelegateDidChangeDisplayName (this));
			}
		}

		bool InnerSetThumbnail(string value) {
			bool shouldBroadcastChange = false;

			turl = value;
			Media oldMedia = media;
			media = value == null || value.Trim ().Equals ("") ?
				null :
				Media.FindOrCreateMedia (new Uri (turl));

			// using = as in pointer equality, if it's a different
			// object then we fire the delegate method
			if (oldMedia != media) {
				if (oldMedia != null) {
					oldMedia.BackgroundDelegateDidCompleteDownload -= MediaDidCompleteDownload;
					NotificationCenter.DefaultCenter.RemoveObserverAction (oldMedia, Media.MEDIA_DID_COMPLETE_DOWNLOAD, MediaDidCompleteDownload);
					NotificationCenter.DefaultCenter.RemoveObserverAction (oldMedia, Constants.Media_DownloadFailed, BackgroundMediaDidFailToDownload);
				}

				shouldBroadcastChange = true;
			}

			if (media != null) {
				media.ThrottleDownload = false;
				media.BackgroundDelegateDidCompleteDownload += MediaDidCompleteDownload;

				NotificationCenter.DefaultCenter.AddWeakObserver (media, Media.MEDIA_DID_COMPLETE_DOWNLOAD, MediaDidCompleteDownload);
				NotificationCenter.DefaultCenter.AddWeakObserver (media, Constants.Media_DownloadFailed, BackgroundMediaDidFailToDownload);
			}

			return shouldBroadcastChange;
		}

		public string thumbnailURLSilent {
			get { return turl; }
			set { InnerSetThumbnail (value); }
		}

		protected string turl;
		public string thumbnailURL {
			get { return turl; }
			set {
				bool shouldBroadcast = InnerSetThumbnail (value);
				if (shouldBroadcast) {
					DelegateDidChangeThumbnailMedia (this);

					var details = new Dictionary<string, CounterParty> ();
					details [Constants.Counterparty_CounterpartyKey] = this;
					NotificationCenter.DefaultCenter.PostNotification (this, Constants.Counterparty_ThumbnailChanged, details);
				}
			}
		}

		string ls = "A";
		public string lifecycleStringSilent { get { return ls; } set { ls = value; } }
		public string lifecycleString {
			get { return ls; }
			set {
				bool fireDelegate = !(new EqualsBuilder<string> (ls, value).Equals());
					
				ls = value;

				if ( fireDelegate )
					DelegateDidChangeLifecycle (this);
			}
		}

		public ContactLifecycle lifecycle {
			get { return ContactLifecycleHelper.FromDatabase (lifecycleString); }
			set {
				// DON'T CALL THIS, Set the string directly as newly introduced
				// lifecycles will come from the server.
			}
		}

		void MediaDidCompleteDownload (Notification notif) {
			var details = new Dictionary<string, CounterParty>();
			details [Constants.Counterparty_CounterpartyKey] = this;
			NotificationCenter.DefaultCenter.PostNotification (this, Constants.Counterparty_DownloadCompleted, details);
		}
			
		void MediaDidCompleteDownload(string path) {
			DelegateDidDownloadThumbnail (this);
		}

		private void BackgroundMediaDidFailToDownload (Notification notification) {
			NotificationCenter.DefaultCenter.PostNotification (this, Constants.Counterparty_DownloadFailed);
		}

		// The case where we already have a thumbnail locally.
		// So don't trigger a DelegateDidChangeThumbnailMedia callback.
		public void UpdateThumbnailUrlAfterMovingFromCache (string newUrl) {
			turl = newUrl;
			media = Media.FindOrCreateMedia (new Uri (turl));
			media.ClearMediaRefs ();
			media.MediaState = MediaState.Present;
			media.ThrottleDownload = false;

			// Probably don't need this as this is the case where we already have the file locally.
			if (media != null) {
				media.BackgroundDelegateDidCompleteDownload += path => DelegateDidDownloadThumbnail (this);
			}
		}

		Media currentMedia;
		Media priorMedia;
		public Media media { 
			get { return currentMedia; }
			set {
				Media oldMedia = currentMedia;
				currentMedia = value;

				// We're only keeping track of the prior media for purposes of keeping the thumbnail in place while we're loading in a new one.
				// So prior media is the last media object with media actually present.
				if (oldMedia != null) {
					if (appModel.mediaManager.MediaOnFileSystem (oldMedia))
						priorMedia = oldMedia;
				}
			}
		}

		public Media PriorMedia {
			get { return priorMedia; }
			set { priorMedia = value; }
		}

		protected JObject attr;
		public JObject attributes { 
			get { return attr; }
			set {
				JObject previous = attr;
				attr = value;

				// 1. previous and attr not the same as each other
				// 2. previous is null and is getting a value
				bool shouldCallDelegate = previous != null && (attr != null && !previous.ToString ().Equals (attr.ToString ())) | previous == null && attr != null;

				if(shouldCallDelegate)
					EMTask.DispatchMain (() => DelegateDidChangeColorTheme (this));
			}
		}

		public string attributesString {
			get { return attributes == null ? null : attributes.ToString(); }
			set { attributes = value == null ? null : JsonConvert.DeserializeObject<JObject> (value); }
		}

		public BackgroundColor colorTheme {
			get { return attributes == null ? BackgroundColor.Default : BackgroundColor.FromHexString ((string)attributes ["color"]); }
			set {
				BackgroundColor previous = colorTheme;
				attributes = attributes ?? new JObject ();

				JToken existing = attributes ["color"];
				if (existing == null)
					((JObject)attributes).Add ("color", value.ToHexString ());
				else
					attributes ["color"] = value.ToHexString ();

				if (!previous.ToHexString().Equals (value.ToHexString()))
					EMTask.DispatchMain (() => DelegateDidChangeColorTheme (this));
			}
		}

		public bool IsAKA {
			get {
				JObject attrs = this.attributes;
				if (attrs == null) return false;

				JToken isAka = attrs ["aka"];
				if (isAka == null) return false;

				bool result = isAka.ToObject<bool> ();
				return result;
			}
		}

		public delegate void DidChangeThumbnailMedia(CounterParty thisContact);
		public DidChangeThumbnailMedia DelegateDidChangeThumbnailMedia = (CounterParty c) => {};

		public delegate void DidDownloadThumbnail(CounterParty thisContact);
		public DidDownloadThumbnail DelegateDidDownloadThumbnail = (CounterParty c) => {};

		public delegate void DidChangeColorTheme(CounterParty thisContact);
		public DidChangeColorTheme DelegateDidChangeColorTheme = (CounterParty c) => {};

		public delegate void DidChangeDisplayName(CounterParty thisContact);
		public DidChangeDisplayName DelegateDidChangeDisplayName = (CounterParty c) => {};

		public delegate void DidChangeLifecycle(CounterParty thisContact);
		public DidChangeLifecycle DelegateDidChangeLifecycle = (CounterParty c) => {};
	}

	public abstract class CountepartyJsonConverter : JsonConverter {
		protected abstract CounterParty CreateReturnObject (Type t);

		protected abstract void WriteObjectToWriter(CounterParty counterparty, JsonWriter writer);

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
			var counterparty = value as CounterParty;
			writer.WriteStartObject ();
			writer.WritePropertyName ("displayName");
			writer.WriteValue (counterparty.displayName);
			writer.WritePropertyName ("thumbnailURL");
			if (counterparty.thumbnailURL != null)
				writer.WriteValue (counterparty.thumbnailURL);
			else
				writer.WriteNull ();
			writer.WritePropertyName ("attributes");
			if ( counterparty.attributes != null )
				writer.WriteRawValue (counterparty.attributesString);
			else
				writer.WriteNull ();
			writer.WritePropertyName ("lifecycle");
			writer.WriteValue (counterparty.lifecycleString);

			WriteObjectToWriter (counterparty, writer);

			writer.WriteEndObject ();
		}

		protected abstract void ReadFromJObject (CounterParty counterparty, JObject jsonObject);

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
			CounterParty retVal = CreateReturnObject (objectType);
			JObject jsonObject = JObject.Load(reader);
			JToken jtok;
			jtok = jsonObject ["displayName"];
			retVal.displayName = jtok == null ? null : jtok.Value<string>();
			jtok = jsonObject ["thumbnailURL"];
			retVal.thumbnailURLSilent = jtok == null ? null : jtok.Value<string>();
			retVal.attributes = (JObject) jsonObject ["attributes"];
			jtok = jsonObject ["lifecycle"];
			retVal.lifecycleStringSilent = jtok == null ? null : jtok.Value<string>();

			ReadFromJObject (retVal, jsonObject);

			return retVal;
		}

		public override bool CanConvert(Type objectType) {
			return objectType.Equals (typeof(CounterParty));
		}
	}
}