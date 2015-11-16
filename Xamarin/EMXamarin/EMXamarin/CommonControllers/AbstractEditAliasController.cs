using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace em {
	public abstract class AbstractEditAliasController {

		readonly ApplicationModel appModel;

		public BackgroundColor InitialBackgroundColor { get; set; }

		public AliasInfo Alias { get; set; }
		public bool IsNewAlias { get; private set; }

		JObject properties;
		public JToken Properties {
			set {
				properties = value as JObject;
				if ( properties != null ) {
					JToken tok;
					tok = properties ["akaName"];
					if (tok != null)
						Alias.displayName = tok.Value<string>();
					tok = properties ["akaPhotoURL"];
					if (tok != null)
						Alias.thumbnailURL = tok.Value<string>();
					tok = properties ["akaIconUrl"];
					if (tok != null)
						Alias.iconURL = tok.Value<string>();
					tok = properties ["akaAttributes"];
					if (tok != null)
						Alias.attributes = (JObject) tok;
					OriginalColorTheme = ColorTheme;
				}
			}
			get {
				return properties;
			}
		}

		public string ResponseDestination { set; get; }

		byte[] updatedThumbnail;
		public byte[] UpdatedThumbnail {
			get { return updatedThumbnail; }
			set { updatedThumbnail = value; }
		}

		byte[] updatedIcon;
		public byte[] UpdatedIcon {
			get { return updatedIcon; }
			set { updatedIcon = value; }
		}
			
		public BackgroundColor ColorTheme {
			get { return Alias.colorTheme; }
			set { Alias.colorTheme = value; } // this will trigger a delegate call in the colorTheme property setter if it's different
		}

		private BackgroundColor OriginalColorTheme { get; set; }

		// If the user made any edits on a new alias, we'd ask them if they wanted to confirm the exit.
		// This is a flag tracking their choice.
		public bool UserChoseToLeaveUponBeingAsked { get; set; }

		string textInDisplayNameField; // keep track of the text in the display name field when changing
		public string TextInDisplayNameField {
			get { return textInDisplayNameField; }
			set { textInDisplayNameField = value; }
		}

		public string DisplayName {
			get { return Alias.displayName; }
			set { Alias.displayName = value; } // this will trigger a delegate call in the colorTheme property setter if it's different
		}

		private bool EditsWereMade {
			get {
				if (this.Alias.media != null || this.Alias.iconMedia != null) {
					return true;
				}

				if (this.ColorTheme != this.OriginalColorTheme) {
					return true;
				}

				if (!string.IsNullOrWhiteSpace (this.TextInDisplayNameField)) {
					return true;
				}

				return false;
			}
		}

		public bool ShouldStopUserFromExiting {
			get {
				if (this.UserChoseToLeaveUponBeingAsked) {
					return false;
				}

				if (this.IsNewAlias && this.EditsWereMade) {
					return true;
				}

				return false;
			}
		}

		WeakDelegateProxy DidChangeAliasIconProxy;
		WeakDelegateProxy DidDownloadAliasIconProxy;

		protected AbstractEditAliasController (ApplicationModel applicationModel) {
			appModel = applicationModel;

			DidChangeAliasIconProxy = WeakDelegateProxy.CreateProxy<AliasInfo> (AliasDidChangeIcon);
			DidDownloadAliasIconProxy = WeakDelegateProxy.CreateProxy<AliasInfo> (AliasDidDownloadIcon);
		}

		public void Dispose() {
			if (Alias != null) {
				Alias.DelegateChangeAliasIcon -= DidChangeAliasIconProxy.HandleEvent<AliasInfo>;
				Alias.DelegateDownloadAliasIcon -= DidDownloadAliasIconProxy.HandleEvent<AliasInfo>;
			}
		}

		public abstract void DidChangeColorTheme ();
		public abstract void DidAliasActionFail (string message);
		public abstract void DidSaveAlias (bool saved);
		public abstract void DidDeleteAlias ();
		public abstract void ConfirmWithUserDelete (String serverID, Action<bool> onCompletion);
		public abstract void ThumbnailUpdated ();

		protected void DidChangeColorTheme(CounterParty alias) {
			DidChangeColorTheme ();
		}

		public void SetInitialAlias (string aliasServerID) {
			if (aliasServerID != null) {
				this.Alias = appModel.account.accountInfo.AliasFromServerID (aliasServerID);
				this.InitialBackgroundColor = this.Alias.colorTheme;
				this.IsNewAlias = false;
			} else {
				this.Alias = new AliasInfo ();
				this.Alias.appModel = appModel;
				this.Alias.isPersisted = false;
				this.Alias.colorTheme = BackgroundColor.Default;
				this.IsNewAlias = true;
			}

			this.OriginalColorTheme = this.Alias.colorTheme;
			this.UserChoseToLeaveUponBeingAsked = false;

			Alias.DelegateChangeAliasIcon += DidChangeAliasIconProxy.HandleEvent<AliasInfo>;
			Alias.DelegateDownloadAliasIcon += DidDownloadAliasIconProxy.HandleEvent<AliasInfo>;

			NotificationCenter.DefaultCenter.AddWeakObserver (this.Alias, Constants.Counterparty_DownloadFailed, BackgroundCounterpartyDownloadFailed);
			NotificationCenter.DefaultCenter.AddWeakObserver (this.Alias, Constants.Counterparty_DownloadCompleted, BackgroundCounterpartyDownloadCompleted);
			NotificationCenter.DefaultCenter.AddWeakObserver (this.Alias, Constants.Counterparty_ThumbnailChanged, BackgroundCounterpartyThumbnailChanged);
		}

		protected void AliasDidChangeIcon(AliasInfo alias) {
			BackgroundCounterpartyDownloadCompleted (null);
		}

		protected void AliasDidDownloadIcon(AliasInfo alias) {
			BackgroundCounterpartyDownloadCompleted (null);
		}

		protected void BackgroundCounterpartyDownloadCompleted (Notification n) {
			SetThumbnailOrIconByteArrayAfterDownloaded ();
			DidChangeThumbnail ();
		}

		protected void BackgroundCounterpartyThumbnailChanged (Notification n) {
			DidChangeThumbnail ();
		}

		protected void BackgroundCounterpartyDownloadFailed (Notification n) {
			DidChangeThumbnail ();
		}

		void DidChangeThumbnail () {
			ThumbnailUpdated ();
		}

		public void SaveOrUpdateAliasAsync () {
			EMTask.DispatchBackground (() => {
				if(ShouldUpdateAlias()) {
					Alias.appModel = appModel;
					Alias.displayName = TextInDisplayNameField;
					Alias.colorTheme = this.ColorTheme;

					if(DisplayName != null && (this.UpdatedThumbnail != null || this.UpdatedIcon != null))
						MoveAliasThumbnailAndIconToKnownLocation();

					BackgroundSaveOrUpdateAlias ();
				} else {
					DidSaveAlias(false);
				}
			});
		}

		void BackgroundSaveOrUpdateAlias () {
			try {
				var parms = new Dictionary<string, object> ();
				var attrs = new Dictionary<string, string> ();

				long lastUpdated = Convert.ToInt64(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);

				parms.Add ("defaultName", TextInDisplayNameField);
				attrs.Add ("color", this.Alias.colorTheme.ToHexString());
				parms.Add ("attributes", attrs);
				parms.Add ("alias", true);
				parms.Add ("lastUpdated", lastUpdated);
				string attributes = JsonConvert.SerializeObject (parms);

				appModel.account.accountInfo.lastUpdated = lastUpdated;

				appModel.account.UpdateAccountInfo (UpdatedThumbnail, UpdatedIcon, attributes, (ResultInput ri) => EMTask.DispatchMain (() => {
					if (ri == null || !ri.success) {
						DidAliasActionFail (ri == null ? appModel.platformFactory.GetTranslation ("ALIAS_GENERIC_ERROR") : ri.reason);
						MoveAliasThumbnailAndIconAfterFailure();
					} else {
						DidSaveAlias (true);

						appModel.RecordGAGoal(Preference.GA_CREATED_AKA, AnalyticsConstants.CATEGORY_GA_GOAL, 
							AnalyticsConstants.ACTION_CREATE_AKA, AnalyticsConstants.CREATED_AKA, AnalyticsConstants.VALUE_CREATE_AKA);

						if(ResponseDestination != null)
							appModel.liveServerConnection.ClientSendMessage(TextInDisplayNameField, ResponseDestination);
					}
				}));

			} catch (Exception e) {
				Debug.WriteLine(string.Format("Failed to Save Alias: {0}\n{1}", e.Message, e.StackTrace));
				DidAliasActionFail (appModel.platformFactory.GetTranslation ("ALIAS_GENERIC_ERROR"));
			}
		}

		public void DeleteAliasAsync(string serverID, bool confirmWithUser) {
			if (!confirmWithUser)
				EMTask.DispatchBackground (() => BackgroundDeleteAlias (serverID));
			else
				ConfirmWithUserDelete (serverID, (bool confirmed) => { if (confirmed) DeleteAliasAsync(serverID, false); });
		}

		void BackgroundDeleteAlias(string serverID) {
			try {
				appModel.account.DeleteAlias(serverID, (ResultInput ri) => EMTask.DispatchMain (() => {
					if(ri == null || !ri.success) 
						DidAliasActionFail(ri == null ? appModel.platformFactory.GetTranslation ("ALIAS_GENERIC_ERROR") : ri.reason);
					else {
						DeleteAliasThumbnailAndIconFromKnownLocation();
						DidDeleteAlias();
					}
						
				}));
			} catch (Exception e) {
				Debug.WriteLine(string.Format("Failed to Delete Alias: {0}\n{1}", e.Message, e.StackTrace));
			}
		}

		public bool ShouldUpdateAlias() {
			bool same = true;

			//if this is the create alias case, always save
			if (this.Alias.serverID == null)
				return same;

			if (this.Alias.colorTheme != this.InitialBackgroundColor)
				same = false;

			if (UpdatedThumbnail != null && UpdatedThumbnail.Length > 0)
				same = false;

			if (UpdatedIcon != null && UpdatedIcon.Length > 0)
				same = false;

			return !same;
		}

		public string GetRandomGeneratedAliasImagePath() {
			return Utils.GenerateRandomString (8);
		}

		bool UseStagingPath (AliasInfo alias) {
			if (alias != null && alias.serverID != null) {
				return false;
			}

			return true;
		}

		public string GetStagingFilePathForAliasThumbnail () {
			if (UseStagingPath (Alias)) {
				return appModel.uriGenerator.GetStagingPathForAliasThumbnailLocal ();
			}

			return appModel.uriGenerator.GetStagingPathForAliasThumbnailServer (Alias);
		}

		public string GetStagingFilePathForAliasIconThumbnail () {
			if (UseStagingPath (Alias)) {
				return appModel.uriGenerator.GetStagingPathForAliasIconThumbnailLocal ();
			}

			return appModel.uriGenerator.GetStagingPathForAliasIconThumbnailServer (Alias);
		}

		public void SetThumbnailOrIconByteArrayAfterDownloaded() {
			if (Properties != null && (UpdatedThumbnail == null || UpdatedIcon == null)) {
				IFileSystemManager fileSystemManager = appModel.platformFactory.GetFileSystemManager ();

				var thumbnailPath = Alias.media.GetPathForUri (appModel.platformFactory);
				var iconPath = Alias.iconMedia.GetPathForUri (appModel.platformFactory);

				if (thumbnailPath != null || iconPath != null) {
					
					byte[] thumbnailBytes = fileSystemManager.ContentsOfFileAtPath (thumbnailPath);
					byte[] iconBytes = fileSystemManager.ContentsOfFileAtPath (iconPath);

					if (UpdatedThumbnail == null && thumbnailBytes != null && thumbnailBytes.Length > 0) {
						this.Alias.UpdateThumbnailUrlAfterMovingFromCache (thumbnailPath);
						UpdatedThumbnail = thumbnailBytes;
					}

					if (UpdatedIcon == null && iconBytes != null && iconBytes.Length > 0) {
						this.Alias.UpdateIconUrlAfterMovingFromCache (iconPath);
						UpdatedIcon = iconBytes;
					}
				}
			}
		}

		public void MoveAliasThumbnailAndIconToKnownLocation() {
			EMTask.DispatchMain (() => {
				//copy from random generated location to known location
				if (!Alias.isPersisted) {
					IUriGenerator uriGenerator = this.appModel.uriGenerator;
					IFileSystemManager fileSystemManager = this.appModel.platformFactory.GetFileSystemManager ();

					if (UpdatedThumbnail != null) {
						EMTask.DispatchBackground(() => {
							//move thumbnail from generated path to displayName path
							string oldThumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailLocal ();
							string newThumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailServer (this.Alias);

							fileSystemManager.RemoveFileAtPath (newThumbnailPath);
							fileSystemManager.MoveFileAtPath (oldThumbnailPath, newThumbnailPath);
							fileSystemManager.RemoveFileAtPath (oldThumbnailPath);

							this.Alias.UpdateThumbnailUrlAfterMovingFromCache (newThumbnailPath);
						});
					}

					if (this.UpdatedIcon != null) {
						EMTask.DispatchBackground(() => {
							//move icon from generated path to displayName path
							string oldIconPath = uriGenerator.GetStagingPathForAliasIconThumbnailLocal ();
							string newIconPath = uriGenerator.GetStagingPathForAliasIconThumbnailServer (this.Alias);

							if (fileSystemManager.FileExistsAtPath (newIconPath)) {
								fileSystemManager.RemoveFileAtPath (newIconPath);
							}

							fileSystemManager.MoveFileAtPath (oldIconPath, newIconPath);
							fileSystemManager.RemoveFileAtPath (oldIconPath);
							this.Alias.UpdateIconUrlAfterMovingFromCache (newIconPath);
						});
					}
				}
			});
		}

		public void MoveAliasThumbnailAndIconAfterFailure() {
			EMTask.DispatchMain (() => {
				//copy from random generated location to known location
				IUriGenerator uriGenerator = this.appModel.uriGenerator;
				IFileSystemManager fileSystemManager = this.appModel.platformFactory.GetFileSystemManager ();
				if (!this.Alias.isPersisted) {
					if (this.UpdatedThumbnail != null) {
						EMTask.DispatchBackground (() => {
							string aliasThumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailLocal ();
							fileSystemManager.RemoveFileAtPath (aliasThumbnailPath);
							fileSystemManager.CopyBytesToPath (aliasThumbnailPath, this.UpdatedThumbnail, null);
						});
					}

					if (this.UpdatedIcon != null) {
						EMTask.DispatchBackground (() => {
							string aliasIconPath = uriGenerator.GetStagingPathForAliasIconThumbnailLocal ();
							fileSystemManager.RemoveFileAtPath (aliasIconPath);
							fileSystemManager.CopyBytesToPath (aliasIconPath, this.UpdatedIcon, null);
						});
					}
				}
			});
		}

		public void DeleteAliasThumbnailAndIconFromKnownLocation () {
			EMTask.DispatchBackground(() => {
				IUriGenerator uriGenerator = this.appModel.uriGenerator;
				IFileSystemManager fileSystemManager = this.appModel.platformFactory.GetFileSystemManager ();
				string thumbnailPath = uriGenerator.GetStagingPathForAliasThumbnailServer (this.Alias);
				fileSystemManager.RemoveFileAtPath (thumbnailPath);

				string iconPath = uriGenerator.GetStagingPathForAliasIconThumbnailServer (this.Alias);
				fileSystemManager.RemoveFileAtPath (iconPath);

				if (this.Alias.media != null && this.Alias.media.uri != null) {
					string aliasThumbnailCachedFilePath = uriGenerator.GetCachedFilePathForUri (this.Alias.media.uri);
					fileSystemManager.RemoveFileAtPath (aliasThumbnailCachedFilePath);
				}

				if (this.Alias.MediaForIcon != null && this.Alias.MediaForIcon.uri != null) {
					string aliasIconCachedFilePath = uriGenerator.GetCachedFilePathForUri (this.Alias.MediaForIcon.uri);
					fileSystemManager.RemoveFileAtPath (aliasIconCachedFilePath);
				}
			});
		}
	}
}