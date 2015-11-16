using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;

namespace em {
	public class EMAccount {

		public ApplicationModel applicationModel { get; set; }
		public bool hasCredentials;
		public PlatformType platform;
		public IDeviceInfo deviceInfo;
		AccountInfo ai;
		public UserSettings UserSettings { get; set; }

		object conf_mutex = new object();
		JObject _conf;
		public JObject configuration {
			get {
				lock (conf_mutex) {
					return _conf;
				}
			}

			set {
				lock (conf_mutex) {
					_conf = value;
				}
			}
		}
		object ai_mutex = new object();
		public AccountInfo accountInfo {
			get {
				lock (ai_mutex) {
					return ai;
				}
			}

			set {
				lock (ai_mutex) {
					ai = value;
				}
			}
		}
		public string defaultName {
			get {
				return accountInfo != null && accountInfo.displayName != null ? accountInfo.displayName : applicationModel.platformFactory.GetTranslation ("DISPLAY_NAME");
			}
		}

		private bool _isLoggedIn = false;
		public bool IsLoggedIn {
			get { 
				return this._isLoggedIn; 
			}

			set { 
				Debug.Assert (this.applicationModel.platformFactory.OnMainThread, "We should only set IsLoggedIn on the main thread.");
				this._isLoggedIn = value; 
			}
		}

		public bool ConfigurationShouldTrackInstalledApps {
			get {
				bool shouldTrackInstalledApps = false;
				JObject jobject = this.configuration != null ? (JObject) this.configuration["track_installed_apps"] : null;
				JToken tok = null;
				if (jobject != null && jobject.TryGetValue ("ShouldTrackInstalledApps", out tok)) {
					shouldTrackInstalledApps = tok == null ? false : tok.ToObject<bool>();

					long nowInMilliseconds = Convert.ToInt64 (DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds);
					long lastTimeTrackedInMilliseconds = this.UserSettings.LastTimeTrackedInstalledApps;
					long difference = nowInMilliseconds - lastTimeTrackedInMilliseconds;

					if (difference < 604800000 /* 1 week in milliseconds */) {
						shouldTrackInstalledApps = false;
					} else {
						// Only store the last time we tracked apps if we're actually tracking apps this time around.
						if (shouldTrackInstalledApps) {
							this.UserSettings.LastTimeTrackedInstalledApps = nowInMilliseconds;
						}
					}
				}

				return shouldTrackInstalledApps;
			}
		}

		public bool ConfigurationShouldBindToWhosHere {
			get {
				JObject jobject = this.configuration != null ? (JObject) this.configuration["whoshere"] : null;
				JToken tok = null;

				bool bindToWhosHere = false;

				if (jobject != null && jobject.TryGetValue ("BindToWhosHere", out tok)) {
					bindToWhosHere = tok == null ? false : tok.ToObject<bool>();
				}

				return bindToWhosHere;
			}
		}
			
		public EMHttpClient httpClient;
		public EMHttpClient imageSearchClient;

		public EMAccount (ApplicationModel appModel, PlatformType platformType, IDeviceInfo info) {
			applicationModel = appModel;
			httpClient = new EMHttpClient (appModel);
			imageSearchClient = new EMHttpClient (appModel);
			platform = platformType;
			deviceInfo = info;
			this.UserSettings = new UserSettings ();
			this.UserSettings.AppModel = appModel;

			// parsing the JSON for the account seems slow (~1-1.5s) so 
			// we farm this off to a background thread.  We block 
			// the main thread to 'hand off' the mutex to the background
			// thread to make sure that no code down stream on the
			// main thread tries to grab the account info before the background thread
			// has had a chance to grab the lock.  The use of 'info' as the mutex to
			// do this is arbitrary.
			lock (info) {
				EMTask.DispatchBackground (() => {
					// we grab the mutex, at this point the main thread is waiting for
					// us.
					lock (ai_mutex) {
						// so now we notify the main thread that it can run because we
						// have the lock we need, which will block any future calls to
						// retrieve the account info if they occur while we are parsing.
						lock ( info ) {
							Monitor.PulseAll(info);
						}

						// load in the prior account info (if it exists)
						try {
							string path = applicationModel.platformFactory.GetUriGenerator ().GetFilePathForAccountInfo ();
							byte[] acctInfo = applicationModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath (path);
							string s = Encoding.UTF8.GetString (acctInfo, 0, acctInfo.Length);

							accountInfo = AccountInfo.FromJsonString (applicationModel, s);
						} catch (Exception e) {
							Debug.WriteLine ("Cannot load account info. Creating a new one. Message: " + e.Message);
							accountInfo = new AccountInfo (applicationModel);
						}
					}
				});

				// just stops the main thread long enough to let the background thread grab the lock we
				// want it to have.
				Monitor.Wait (info);
			}
		}

		public bool HasCredentials () {
			return true;  // stub
		}

		public void RegisterForPushNotifications () {
			string deviceJsonStr = deviceInfo.DeviceJSONString ();
			// Adding a null check here because there's a potential race condition where this function will be called again when liveServerConnection is null.
			// e.g. When opening up android's gallery and moving away from MainActivity and then returning to MainActivity after.
			// This causes MainActivity's OnPause, OnResume to jumble close together.
			if (applicationModel.liveServerConnection != null)
				applicationModel.liveServerConnection.SendDeviceDetailsAsync (deviceJsonStr);
		}

		public void RequestMissedNotificationsAsync (DateTime since) {
			RequestMissedMessagesOrNotificationsAsync ("missedNotifications", since);
		}

		public void RequestMissedMessagesAsync (DateTime since) {
			RequestMissedMessagesOrNotificationsAsync ("missedMessages", since);
		}

		public void RequestMissedMessagesOrNotificationsAsync (string url, DateTime since) {
			//show network indicator
			applicationModel.platformFactory.ShowNetworkIndicator ();

			long initialStart = DateTime.Now.Ticks;

			var mmo = new MissedMessagesOutbound ();
			mmo.setMessagesSince (since);

			httpClient.SendApiRequestAsync (url, mmo, HttpMethod.Post, "application/json", obj => {
				long restFinish = DateTime.Now.Ticks;
				Debug.WriteLine("Time to get missed messages from server: " + TimeSpan.FromTicks(restFinish - initialStart).Duration());

				Debug.WriteLine ("Done with missedMessages Http Request");
				if (obj.IsSuccess) {
                    string responseStr = obj.ResponseAsString;
					try {
						if (responseStr == null || responseStr.Trim ().Length == 0)
							Debug.WriteLine ("Missed messages response is null");
						else {
							JObject missedMessagesJson = JObject.Parse (responseStr);

							EMTask.Dispatch (() => {
								applicationModel.chatList.BackgroundStartingBulkUpdates();
								EMTask.PrepareToWaitForMainThreadCallbacks();
								try {
									ProcessMissedMessages (missedMessagesJson);
								}
								finally {
									EMTask.WaitForMainThreadCallbacks();
									applicationModel.chatList.BackgroundStoppingBulkUpdates();
								}

								long clientFinish = DateTime.Now.Ticks;
								Debug.WriteLine("Time to process account info and missed messages on client: " + TimeSpan.FromTicks(clientFinish - restFinish).Duration());
								Debug.WriteLine("Total time to process full login: " + TimeSpan.FromTicks(clientFinish - initialStart).Duration());

							}, EMTask.HANDLE_MESSAGE_QUEUE);
						}
					} catch (Exception e) {
						Debug.WriteLine ("Exception processing missed messages " + responseStr + " with " + e);
					} finally {
						//hide network indicator
						applicationModel.platformFactory.HideNetworkIndicator();
					}
				} else {
					Debug.WriteLine("Error getting missed messages. Obj: " + obj);

					//hide network indicator
					applicationModel.platformFactory.HideNetworkIndicator();
				}
			});
		}

		void ProcessMissedMessages (JObject json) {
			var postHandlingTasks = new List<Action>();
			var messages = (JArray)json ["messages"];

			applicationModel.IsHandlingMissedMessages = true;
			foreach (JObject message in messages)
				applicationModel.HandleMessage (message, postHandlingTasks);
			applicationModel.IsHandlingMissedMessages = false;

			foreach (Action task in postHandlingTasks)
				task();

			//need to do this to get the proper raw date string independant from culture
			//see: http://stackoverflow.com/questions/11856694/json-net-disable-the-deserialization-on-datetime
			JsonReader reader = new JsonTextReader(new StringReader(json.ToString()));
			reader.DateParseHandling = DateParseHandling.None;
			JObject rawJson = JObject.Load(reader);
			var updateSinceString = rawJson.GetValue ("sinceDate").ToString ();

			var errMsg = "Error parsing missed messages sinceDate: " + updateSinceString;
			if(updateSinceString != null) {
				DateTime updateSince;

				if(DateTime.TryParse(updateSinceString, Preference.usEnglishCulture, DateTimeStyles.AdjustToUniversal|DateTimeStyles.AssumeUniversal, out updateSince)) {
					Preference.UpdatePreference<DateTime> (applicationModel, Preference.LAST_MESSAGE_UPDATE, updateSince);
				} else {
					Debug.WriteLine (errMsg);
					applicationModel.platformFactory.ReportToXamarinInsights (errMsg);
				}
			}
		}

		public void RegisterEmailAddress (string emailAddress, string countryCode, string phonePrefix, Action<bool, string> completionHandler) {
			var parms = new JObject ();
			parms.Add ("emailAddress", emailAddress);
			parms.Add ("countryCode", countryCode);
			parms.Add ("phonePrefix", phonePrefix);

			var attrs = new JObject ();
			attrs.Add ("color", accountInfo.colorTheme.ToHexString());
			parms.Add ("attributes", attrs);
			httpClient.SendApiRequestAsync ("registerEmail", parms, HttpMethod.Post, "application/json", obj => {
				Debug.WriteLine ("Done with Http Request");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					Dictionary<string, string> json = JsonConvert.DeserializeObject<Dictionary<string, string>> (responseStr);
					string accountID = json ["accountID"];
					accountInfo.username = accountID;
					accountInfo.existingAccount = false;

					string existingStr = json ["existingAccount"];
					if(!string.IsNullOrEmpty(existingStr) && existingStr.ToLower().Equals("true"))
						accountInfo.existingAccount = true;

					completionHandler (true, accountID);
				} else {
					completionHandler (false, "");
				}
			});
		}

		public void RegisterMobileNumber (string mobileNumber, string countryCode, string phonePrefix, Action<bool, string> completionHandler) {
			var parms = new JObject ();
			parms.Add ("mobileNumber", mobileNumber);
			parms.Add ("countryCode", countryCode);
			parms.Add ("phonePrefix", phonePrefix);

			var attrs = new JObject ();
			attrs.Add ("color", accountInfo.colorTheme.ToHexString());
			parms.Add ("attributes", attrs);
			httpClient.SendApiRequestAsync ("registerMobile", parms, HttpMethod.Post, "application/json", obj => {
				Debug.WriteLine ("Done with Http Request");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					Dictionary<string, string> json = JsonConvert.DeserializeObject<Dictionary<string, string>> (responseStr);
					string accountID = json ["accountID"];
					accountInfo.username = accountID;
					accountInfo.existingAccount = false;

					string existingStr = json ["existingAccount"];
					if(!string.IsNullOrEmpty(existingStr) && existingStr.ToLower().Equals("true"))
						accountInfo.existingAccount = true;
					
					completionHandler (true, accountID);
				} else {
					completionHandler (false, "");
				}
			});
		}

		public void SearchForContact (string searchString, Action<SearchResponseInput> completionHandler) {
			var search = new SearchOutbound ();
			search.searchString = searchString;
			httpClient.SendApiRequestAsync ("search", search, HttpMethod.Post, "application/json", obj => {
				Debug.WriteLine ("Done with Search");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					SearchResponseInput searchResponse = JsonConvert.DeserializeObject<SearchResponseInput> (responseStr);
					EMTask.DispatchMain (() => completionHandler (searchResponse));
				}
			});
		}

		public void PossibleAccountInfoUpdate (JObject json) {
			AccountInfo updatedAccountInfo = json.ToObject<AccountInfo>();
			accountInfo.UpdateFrom (updatedAccountInfo);
			accountInfo.SaveAccountInfoOffline ();
		}

		public void UpdateAccountInfo (byte[] thumbnailJpg, byte[] iconJpg, string attributes, Action<ResultInput> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = "/uploadFiles/updateInfo";
			queueEntry.methodType = QueueRestMethodType.MultiPartPost;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			byte[] jsonBytes = Encoding.UTF8.GetBytes (attributes);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (applicationModel, jsonBytes, "application/json", "attributes.json", "attributes.json");
			queueEntry.contents.Add (messageContents);

			if (thumbnailJpg != null) {
				QueueEntryContents thumbnailContents = QueueEntryContents.CreateTemporaryContents (applicationModel, thumbnailJpg, "image/jpeg", "thumbnail", "thumbnail.jpg");
				queueEntry.contents.Add (thumbnailContents);
			}
				
			if (iconJpg != null) {
				QueueEntryContents iconContents = QueueEntryContents.CreateTemporaryContents (applicationModel, iconJpg, "image/jpeg", "icon", "icon.jpg");
				queueEntry.contents.Add (iconContents);
			}

			accountInfo.SaveAccountInfoOffline ();

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, obj => {
				Debug.WriteLine ("UpdateAccountInfo:DoneWithHttpRequest");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}

		public void AddContactToContactList(Contact contact, Action<ResultInput> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = "modifyContactList";
			queueEntry.methodType = QueueRestMethodType.Post;;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			var dict = new Dictionary<string,string> ();
			dict ["serverID"] = contact.serverID;
			dict ["type"] = contact.tempContact.Value ? "add" : "remove";

			string attributes = JsonConvert.SerializeObject (dict);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (attributes);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (applicationModel, jsonBytes, "application/json", "attributes.json", "attributes.json");
			queueEntry.contents.Add (messageContents);

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, obj => {
				Debug.WriteLine ("AddContactToAddressBook:DoneWithHttpRequest");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}

		public void BlockContact(Contact contact, Action<ResultInput> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = "block/" + contact.serverID;
			queueEntry.methodType = QueueRestMethodType.Post;;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, obj => {
				Debug.WriteLine ("BlockContact:DoneWithHttpRequest");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}

		public void UnblockContact(Contact contact, Action<ResultInput> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = "unblock/" + contact.serverID;
			queueEntry.methodType = QueueRestMethodType.Post;;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, obj => {
				Debug.WriteLine ("UnblockContact:DoneWithHttpRequest");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}

		public void DeleteAlias(string serverID, Action<ResultInput> completionHandler) {
			string lastUpdated = Convert.ToInt64 (DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds).ToString ();
			httpClient.SendApiRequestAsync (string.Format("alias/{0}?lastUpdated={1}", serverID, lastUpdated), null, HttpMethod.Delete, "application/json", obj => {
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}

		public void ReactivateAlias(string serverID, Action<ResultInput> completionHandler) {
			string lastUpdated = Convert.ToInt64 (DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1, 0, 0, 0, 0)).TotalMilliseconds).ToString ();
			httpClient.SendApiRequestAsync (string.Format("alias/{0}?lastUpdated={1}", serverID, lastUpdated), null, HttpMethod.Post, "application/json", obj => {
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					ResultInput result = JsonConvert.DeserializeObject<ResultInput> (responseStr);
					completionHandler (result);
				} else {
					completionHandler (null);
				}
			});
		}
		
		public void LookupGroup(string groupID, Action<GroupInput> completionHandler) {
			httpClient.SendApiRequestAsync (string.Format("group/{0}", groupID), null, HttpMethod.Get, "application/json", obj => {
				Debug.WriteLine ("Done with Group Lookup");
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					GroupInput rsp = JsonConvert.DeserializeObject<GroupInput> (responseStr);
					EMTask.DispatchMain (() => completionHandler (rsp));
				} else {
					EMTask.DispatchMain (() => completionHandler (null));
				}
			});
		}

		public void SaveGroup(GroupUpdateOutbound groupUpdate, byte[] thumbnailJpg, Action<Contact> completionHandler) {
			UpdateOrSaveGroup (groupUpdate, thumbnailJpg, "/group", completionHandler);
		}

		public void UpdateGroup(GroupUpdateOutbound groupUpdate, byte[] thumbnailJpg, Action<Contact> completionHandler) {
			UpdateOrSaveGroup (groupUpdate, thumbnailJpg, string.Format("/group/{0}", groupUpdate.serverID), completionHandler);
		}

		void UpdateOrSaveGroup (GroupUpdateOutbound groupUpdate, byte[] thumbnailJpg, string destination, Action<Contact> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = destination;
			queueEntry.methodType = QueueRestMethodType.MultiPartPost;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			string json = JsonConvert.SerializeObject (groupUpdate);
			byte[] jsonBytes = Encoding.UTF8.GetBytes (json);
			QueueEntryContents messageContents = QueueEntryContents.CreateTemporaryContents (applicationModel, jsonBytes, "application/json", "group.json", "group.json");
			queueEntry.contents.Add (messageContents);

			if (thumbnailJpg != null) {
				QueueEntryContents thumbnailContents = QueueEntryContents.CreateTemporaryContents (applicationModel, thumbnailJpg, "image/jpeg", "file", "file123");
				queueEntry.contents.Add (thumbnailContents);
			}

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, (obj => {
				Contact group = null;
				if (obj.IsSuccess) {
					string responseStr = obj.ResponseAsString;
					GroupInput gInput = JsonConvert.DeserializeObject<GroupInput> (responseStr);
					if (gInput != null) {
						group = Contact.FindContactByServerID (applicationModel, gInput.serverID);
						if (group == null) {
							group = Contact.FromContactInput (applicationModel, gInput);
							group.Save ();
						} else if (Contact.UpdateFromContactInput (group, gInput))
							group.Save ();

						// since we have the bytes, we save the thumbnail directly to our cached area.
						if (thumbnailJpg != null && thumbnailJpg.Length > 0 && gInput.thumbnailURL != null) {
							string localPath = applicationModel.uriGenerator.GetCachedFilePathForUri (new Uri (gInput.thumbnailURL));
							applicationModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (localPath, thumbnailJpg, null);
						}
					}
				}

				completionHandler (group);
			}));
		}

		public void DeleteGroup(string groupID, Action<bool> completionHandler) {
			httpClient.SendApiRequestAsync (string.Format("group/{0}", groupID), null, HttpMethod.Delete, "application/json", obj => {
				if(obj == null)
					completionHandler(false);
				else
					completionHandler(obj.IsSuccess);
			});
		}

		public void LeaveGroup(string groupID, Action<bool> completionHandler) {
			httpClient.SendApiRequestAsync (string.Format("group/{0}/member", groupID), null, HttpMethod.Delete, "application/json", obj => {
				if(obj == null)
					completionHandler(false);
				else
					completionHandler(obj.IsSuccess);
			});
		}

		public void RejoinGroup(string groupID, Action<bool> completionHandler) {
			httpClient.SendApiRequestAsync (string.Format("group/{0}/member", groupID), null, HttpMethod.Put, "application/json", obj => {
				if(obj == null)
					completionHandler(false);
				else
					completionHandler(obj.IsSuccess);
			});
		}

		public void LoginWithAccountIdentifier (string acctID, string verificationCode, bool getMissedMessages, Action<bool, bool> completionHandler) {
			//show network indicator
			applicationModel.platformFactory.ShowNetworkIndicator ();

			long initialStart = DateTime.Now.Ticks;

			DateTime messagesSince = Preference.GetPreference<DateTime>(applicationModel, Preference.LAST_MESSAGE_UPDATE );
			httpClient.DoLoginAsync (acctID, verificationCode, messagesSince, getMissedMessages, (obj => {
				if (obj != null && obj.ServerTime != DateTime.MinValue && obj.ClientTime != DateTime.MinValue) {
					double timeDiffInSeconds = (obj.ServerTime - obj.ClientTime).TotalSeconds;
					applicationModel.EMStandardTimeDiff = timeDiffInSeconds;
				}

				// we grab an outer lock that will block us until the background thread has acquired the
				// conf_mutex.  Once the conf mutex is grabbed any thing that needs the conf will block
				// until it's retrieved.
				object outer_lock = new object();
				lock(outer_lock) {
					// Push to the main thread since we need to set the IsLoggedIn flag on the main thread to avoid race conditions.
					EMTask.ExecuteNowIfMainOrDispatchMain (() => {
						if (obj.IsSuccess && obj.ResponseAsString != null && obj.ResponseAsString.Length > 0) {
							// If the app is in the foreground and we successfully logged in, then we set the flag to true.
							// Otherwise, we're in the background.
							if (applicationModel.AppInForeground) {
								this.IsLoggedIn = true;
							} else {
								this.IsLoggedIn = false;
							}
									
							EMTask.DispatchBackground (() => {
								long restFinish = DateTime.Now.Ticks;
								JObject json;
								lock(conf_mutex) {
									lock(outer_lock) {
										Monitor.PulseAll(outer_lock);
									}

									Debug.WriteLine("Time to login and return missed messages from server: " + TimeSpan.FromTicks(restFinish - initialStart).Duration());

									string responseStr = obj.ResponseAsString;

									json = JObject.Parse (responseStr);
									configuration = (JObject) json["configuration"]; // saving off account info
								}

								var accountInfoJson = (JObject) json ["accountInfo"];
								AccountInfo updatedAccountInfo = AccountInfo.FromJObject (applicationModel, accountInfoJson);

								accountInfo.UpdateFrom (updatedAccountInfo);
								accountInfo.SaveAccountInfoOffline ();

								if (getMissedMessages) {
									var missedMessagesJson = (JObject) json ["missedMessages"];
									EMTask.Dispatch (() => {
										applicationModel.chatList.BackgroundStartingBulkUpdates();
										EMTask.PrepareToWaitForMainThreadCallbacks();
										try {
											ProcessMissedMessages (missedMessagesJson);
										}
										finally {
											EMTask.WaitForMainThreadCallbacks();
											applicationModel.chatList.BackgroundStoppingBulkUpdates();
										}

										long clientFinish = DateTime.Now.Ticks;
										Debug.WriteLine("Time to process account info and missed messages on client: " + TimeSpan.FromTicks(clientFinish - restFinish).Duration());
									}, EMTask.HANDLE_MESSAGE_QUEUE);
								}

								completionHandler (true, updatedAccountInfo.existingAccount);

								//hide network indicator
								applicationModel.platformFactory.HideNetworkIndicator ();

								long loginFinish = DateTime.Now.Ticks;
								Debug.WriteLine("Total time to process login: " + TimeSpan.FromTicks(loginFinish - initialStart).Duration());

								EMTask.DispatchMain (() => {
									NotificationCenter.DefaultCenter.PostNotification (Constants.EMAccount_LoginAndHasConfigurationNotification);
								});
							});
						} else {
							// Failed login call. Set flag to false.
							this.IsLoggedIn = false;
							EMTask.DispatchBackground (() => {

								lock(outer_lock) {
									Monitor.PulseAll(outer_lock);
								}

								completionHandler (false, false);

								//hide network indicator
								applicationModel.platformFactory.HideNetworkIndicator ();
							});
						}
					});

					// this wait gets pulsed by the background task once it releases the 
					Monitor.Wait(outer_lock);
				}
			}));
		}

		public void RecordReferrer(string tracking, Action<bool> completionHandler) {
			var queueEntry = new QueueEntry ();
			queueEntry.destination = "android/track";
			queueEntry.methodType = QueueRestMethodType.Post;
			queueEntry.route = QueueRoute.Rest;
			queueEntry.sentDate = DateTime.Now.ToEMStandardTime(applicationModel);

			var parms = new Dictionary<string, string> ();
			parms.Add ("rawTrackingString", tracking);
			var json = JsonConvert.SerializeObject (parms);
			var jsonBytes = Encoding.UTF8.GetBytes (json);
			var messageContents = QueueEntryContents.CreateTemporaryContents (applicationModel, jsonBytes, "application/json", "referrer.json", "referrer.json");
			queueEntry.contents.Add (messageContents);

			applicationModel.outgoingQueue.EnqueueAndSend (queueEntry, (delegate (EMHttpResponse obj) {
				if (obj.IsSuccess) {
					completionHandler (true);
				} else {
					completionHandler (false);
				}
			}));
		}

		public void DisconnectLogOut () {
		}

		public void ClearKeyChain() {
		}

		public void PostMessage() {
		}

		public void SearchForImage (string url, Action<EMHttpResponse> callback) {
			imageSearchClient.ImageSearchRequest (url, callback);
		}

	}

	[JsonConverter(typeof(AccountInfoJsonConverter))]
	public class AccountInfo : CounterParty {
		public void SaveAccountInfoOffline () {
			string json = JsonConvert.SerializeObject (new {
				username = username,
				defaultName = defaultName,
				thumbnailURL = thumbnailURL,
				aliases = aliases,
				attributes = attributes,
				lifecycleString = lifecycleString,
				lastUpdated = lastUpdated,
			});

			if (json != null) {
				// save account info for the case when we are offline
				byte[] responseBytes = Encoding.UTF8.GetBytes (json);
				string path = appModel.uriGenerator.GetFilePathForAccountInfo ();
				appModel.platformFactory.GetFileSystemManager ().RemoveFileAtPath (path);
				appModel.platformFactory.GetFileSystemManager ().CopyBytesToPath (path, responseBytes, null);
			}
		}

		public static AccountInfo FromJsonString(ApplicationModel _appModel, string s) {
			Debug.Assert (s.Length > 0, "**AccountInfo: FromJsonString: s length is 0");
			JObject accountInfoJson = JObject.Parse (s);
			return FromJObject (_appModel, accountInfoJson);
		}

		public static AccountInfo FromJObject(ApplicationModel _appModel, JObject accountInfoJson) {
			AccountInfo retVal = accountInfoJson.ToObject<AccountInfo>();
			retVal.appModel = _appModel;
			return retVal;
		}

		public string username { get; set; }
		public string defaultName {
			get { return displayName; }
			set { displayName = value; }
		}

		public IList<AliasInfo> aliases { get; set; }
		public IList<AliasInfo> VisibleAliases {
			get {
				var retVal = new List<AliasInfo> ();
				foreach (AliasInfo aliasInfo in aliases) {
					if (aliasInfo.lifecycle == ContactLifecycle.Active || aliasInfo.lifecycle == ContactLifecycle.Orphaned)
						retVal.Add (aliasInfo);
				}
				return retVal;
			}
		}
		public IList<AliasInfo> ActiveAliases {
			get {
				List<AliasInfo> retVal = new List<AliasInfo> ();
				foreach (AliasInfo aliasInfo in aliases) {
					if (aliasInfo.lifecycle == ContactLifecycle.Active)
						retVal.Add (aliasInfo);
				}
				return retVal;
			}
		}
		public long lastUpdated { get; set; }
		public bool existingAccount { get; set; }

		public delegate void DidUpdateAliasList();
		public static DidUpdateAliasList DelegateDidUpdateAliasList = () => {};

		public AccountInfo(ApplicationModel _appModel) {
			appModel = _appModel;
			aliases = new List<AliasInfo> ();
		}

		public AliasInfo AliasFromServerID(string serverID) {
			if (serverID != null) {
				foreach (AliasInfo alias in aliases) {
					if (alias.serverID.Equals (serverID)) {
						alias.appModel = appModel;
						return alias;
					}
				}
			}

			return null;
		}

		public AliasInfo AliasFromName(string name) {
			foreach (AliasInfo alias in aliases) {
				if (alias.displayName.Equals (name)) {
					alias.appModel = appModel;
					return alias;
				}
			}

			return null;
		}

		public void UpdateFrom(AccountInfo updated) {
			if (updated == null)
				return;

			// the updated is older than our current profile, so return
			if (updated.lastUpdated < this.lastUpdated)
				return;

			bool notifyAliasListChanged = false;
			// updated is the same as the current profile
			// check if we need to move the thumbnail at our cached location to the new location specified by the new uri
			if (updated.lastUpdated == this.lastUpdated) {

				IUriGenerator uriGenerator = appModel.uriGenerator;
				IFileSystemManager fileMgr = appModel.platformFactory.GetFileSystemManager ();

				if (this.media != null && this.media.uri != null) {
					string stagingPath = uriGenerator.GetStagingPathForAccountInfoThumbnailLocal ();
					string currentMediaPath = uriGenerator.GetCachedFilePathForUri (this.media.uri);

					byte[] thumbnailAtPath = fileMgr.ContentsOfFileAtPath (currentMediaPath);

					if (thumbnailAtPath != null && updated.media != null) {
						if (stagingPath.Equals (currentMediaPath)) {
							string updatedAccountInfoMediaPath = uriGenerator.GetCachedFilePathForUri (updated.media.uri);

							fileMgr.RemoveFileAtPath (updatedAccountInfoMediaPath);
							fileMgr.CopyBytesToPath (updatedAccountInfoMediaPath, thumbnailAtPath, null);
							this.UpdateThumbnailUrlAfterMovingFromCache (updated.thumbnailURL);
						}
					}
				}
					
				//do the above for aliases (thumbnails and icons)
				for (int i = 0; i < this.aliases.Count; i++) {
					var currAlias = this.aliases [i];
					AliasInfo updatedAlias = null;

					//thumbnail
					if (currAlias.media != null && currAlias.media.uri != null) {
						string aliasThumbnailPath = appModel.uriGenerator.GetCachedFilePathForUri (currAlias.media.uri);
						byte[] thumbnailAtPath = appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath (aliasThumbnailPath);
						if (thumbnailAtPath != null) {
							foreach (AliasInfo ai in updated.aliases) {
								if (ai.serverID == currAlias.serverID) {
									updatedAlias = ai;
									break;
								}
							}

							if (updatedAlias != null && updatedAlias.media != null && updatedAlias.media.uri != null) {
								string stagingPath = uriGenerator.GetStagingPathForAliasThumbnailServer (currAlias);
								string currentMediaPath = appModel.uriGenerator.GetCachedFilePathForUri (currAlias.media.uri);

								if (stagingPath.Equals (currentMediaPath)) {
									string updatedAliasMediaPath = appModel.uriGenerator.GetCachedFilePathForUri (updatedAlias.media.uri);
									fileMgr.RemoveFileAtPath (updatedAliasMediaPath);
									fileMgr.CopyBytesToPath (updatedAliasMediaPath, thumbnailAtPath, null);
									currAlias.UpdateThumbnailUrlAfterMovingFromCache (updatedAlias.thumbnailURL);
								}
							}
						}
					}

					//icon
					if (currAlias.MediaForIcon != null && currAlias.MediaForIcon.uri != null) {
						string aliasIconPath = appModel.uriGenerator.GetCachedFilePathForUri (currAlias.MediaForIcon.uri);
						byte[] thumbnailAtPath = appModel.platformFactory.GetFileSystemManager ().ContentsOfFileAtPath (aliasIconPath);

						if (thumbnailAtPath != null && updatedAlias != null && updatedAlias.MediaForIcon != null && updatedAlias.MediaForIcon.uri != null) {
							string stagingPath = uriGenerator.GetStagingPathForAliasIconThumbnailServer (currAlias);
							string currentMediaPath = uriGenerator.GetCachedFilePathForUri (currAlias.MediaForIcon.uri);

							if (stagingPath.Equals (currentMediaPath)) {
								string updatedAliasIconPath = appModel.uriGenerator.GetCachedFilePathForUri (updatedAlias.MediaForIcon.uri);

								fileMgr.RemoveFileAtPath (updatedAliasIconPath);
								fileMgr.CopyBytesToPath (updatedAliasIconPath, thumbnailAtPath, null);
								currAlias.UpdateIconUrlAfterMovingFromCache (updatedAlias.iconURL);
							}
						}
					}
				}
			}
				
			this.displayName = updated.displayName;
			this.attributes = updated.attributes;
			this.lastUpdated = updated.lastUpdated;
			this.existingAccount = updated.existingAccount;
			this.thumbnailURL = updated.thumbnailURL;
			this.lifecycle = updated.lifecycle;

			// trigger thumbnail download if we don't already have it
			// optimization on account info's thumbnail so that it's downloaded and ready by the time we go into a chat conversation
			if(this.media != null) {
				if (!appModel.mediaManager.MediaOnFileSystem (this.media)) {
					this.media.DownloadMedia (appModel);
				}
			}

			JToken color = updated.attributes ["color"];
			if (color != null)
				this.colorTheme = BackgroundColor.FromHexString (color.ToString());

			if ( aliases.Count != updated.aliases.Count )
				notifyAliasListChanged = true;

			// we store pre existing AKA server IDs incase
			// the server is nolonger returning one.
			HashSet<string> preExisting = new HashSet<string> ();
			foreach (AliasInfo existingAlias in VisibleAliases)
				preExisting.Add (existingAlias.serverID);

			foreach (AliasInfo incomingAlias in updated.aliases) {
				incomingAlias.appModel = appModel;
				
				AliasInfo alias = AliasFromServerID (incomingAlias.serverID);
				if (alias != null) {
				    // so this was a match, remove it from our list
				    // so that only 'previous' but not currently
				    // return AKAs remain
					preExisting.Remove (incomingAlias.serverID);

					//existing alias - assign values and if they've changed, delegate methods should be called
					alias.appModel = appModel;
					alias.displayName = incomingAlias.displayName;
					alias.thumbnailURL = incomingAlias.thumbnailURL;
					alias.iconMedia = incomingAlias.iconMedia;
					alias.iconURL = incomingAlias.iconURL;
					alias.lifecycleString = incomingAlias.lifecycleString;

					alias.attributes = incomingAlias.attributes;
					if (alias.attributes != null) {
						JToken aliasColor = alias.attributes ["color"];
						if (aliasColor != null)
							alias.colorTheme = BackgroundColor.FromHexString (aliasColor.ToString());
					}
					
				} else {
					//add new alias
					if (this.aliases == null)
						this.aliases = new List<AliasInfo> ();
					
					this.aliases.Add (incomingAlias);

					notifyAliasListChanged = true;
				}
			}

			// these must be deleted as the server is nolonger returning them
			foreach (string serverID in preExisting) {
				AliasInfo aliasInfo = AliasFromServerID(serverID);
				aliasInfo.lifecycleStringSilent = "D"; // Deleted

				notifyAliasListChanged = true;
			}

			if ( notifyAliasListChanged ) {
				EMTask.DispatchMain (() => {
					AccountInfo.DelegateDidUpdateAliasList ();
				});
			}

			this.SaveAccountInfoOffline ();
		}
	}

	[JsonConverter(typeof(AliasInfoJsonConverter))]
	public class AliasInfo : CounterParty {

		public delegate void DidChangeAliasIcon (AliasInfo alias);
		public DidChangeAliasIcon DelegateChangeAliasIcon = (AliasInfo a) => {};

		public delegate void DidDownloadAliasIcon (AliasInfo alias);
		public DidDownloadAliasIcon DelegateDownloadAliasIcon = (AliasInfo a) => {};

		public string serverID { get; set; }

		string iurl;
		public string iconURL {
			get { 
				return iurl; 
			}
			set {
				iurl = value;
				Media oldMedia = iconMedia;
				iconMedia = value == null || value.Trim ().Equals ("") ? null : Media.FindOrCreateMedia (new Uri (iurl));

				// using = as in pointer equality, if it's a different object then we fire the delegate method
				if (oldMedia != iconMedia)
					DelegateChangeAliasIcon (this);

				if (oldMedia != null) {
					iconMedia.BackgroundDelegateDidCompleteDownload -= CallDelegateDownloadAliasIcon;
					iconMedia.BackgroundDelegateDidCompleteDownload -= IconMediaDidDownload;
					NotificationCenter.DefaultCenter.RemoveObserverAction (iconMedia, Constants.Media_DownloadFailed, BackgroundMediaDidFailToDownload);
				}

				if (iconMedia != null) {
					iconMedia.ThrottleDownload = false;
					iconMedia.BackgroundDelegateDidCompleteDownload += CallDelegateDownloadAliasIcon;
					iconMedia.BackgroundDelegateDidCompleteDownload += IconMediaDidDownload;

					NotificationCenter.DefaultCenter.AddWeakObserver (iconMedia, Constants.Media_DownloadFailed, BackgroundMediaDidFailToDownload);
				}
			}
		}

		void CallDelegateDownloadAliasIcon(string path) {
			DelegateDownloadAliasIcon (this);
		}

		public void UpdateIconUrlAfterMovingFromCache (string newUrl) {
			iurl = newUrl;
			iconMedia = Media.FindOrCreateMedia (new Uri (iurl));
			iconMedia.ClearMediaRefs ();
			iconMedia.MediaState = MediaState.Present;
			iconMedia.ThrottleDownload = false;

			// Probably don't need this as this is the case where we already have the file locally.
			if (iconMedia != null)
				iconMedia.BackgroundDelegateDidCompleteDownload += path => DelegateDownloadAliasIcon (this);
		}

		public Media iconMedia { get; set; }

		public Media MediaForIcon {
			get {
				return iconMedia;
			}
		}

		void IconMediaDidDownload(string localpath) {
			try {
				ChatList chatList = appModel.chatList;
				chatList.DidDownloadAliasIconMedia (this);
			} catch (Exception e) {
				// saw a NPE here :O
				Debug.WriteLine ("exception thrown IconMediaDidDownload {0}, ", e);
			}
		}

		private void BackgroundMediaDidFailToDownload (Notification notification) {
			NotificationCenter.DefaultCenter.PostNotification (this, Constants.Counterparty_DownloadFailed);
		}
					
	}
		
	public class AccountInfoJsonConverter : CountepartyJsonConverter {
		protected override CounterParty CreateReturnObject (Type t) {
			return new AccountInfo (null);
		}

		protected override void WriteObjectToWriter(CounterParty counterparty, JsonWriter writer) {
			AccountInfo accountInfo = counterparty as AccountInfo;
			writer.WritePropertyName ("username");
			writer.WriteValue (accountInfo.username);
			writer.WritePropertyName ("defaultName");
			if (accountInfo.defaultName != null)
				writer.WriteValue (accountInfo.defaultName);
			else
				writer.WriteNull ();
			writer.WritePropertyName("aliases");
			writer.WriteRawValue ( JsonConvert.SerializeObject (accountInfo.aliases));
			writer.WritePropertyName("lastUpdated");
			writer.WriteValue ( accountInfo.lastUpdated );
			writer.WritePropertyName("existingAccount");
			writer.WriteValue ( accountInfo.existingAccount );
		}

		protected override void ReadFromJObject (CounterParty counterparty, JObject jsonObject) {
			try {
				AccountInfo accountInfo = counterparty as AccountInfo;
				JToken jtok;
				JArray jarray;
				jtok = jsonObject ["username"];
				accountInfo.username = jtok == null ? null : jtok.Value<string>();
				jtok = jsonObject ["defaultName"];
				accountInfo.defaultName = jtok == null ? null : jtok.Value<string>();
				jarray =  jsonObject ["aliases"] as JArray;
				accountInfo.aliases = jtok == null ? new List<AliasInfo>() : jarray.ToObject<IList<AliasInfo>>();
				jtok = jsonObject ["lastUpdated"];
				accountInfo.lastUpdated = jtok == null ? 0 : jtok.Value<long>();
				jtok = jsonObject ["existingAccount"];
				accountInfo.existingAccount = jtok == null ? false : jtok.Value<bool>();
			}
			catch (Exception e) {
				Debug.WriteLine ("Failed to parse: " + jsonObject + ".  " + e);
			}
		}

		public override bool CanConvert(Type objectType) {
			return objectType.Equals (typeof(AccountInfo));
		}
	}

	public class AliasInfoJsonConverter : CountepartyJsonConverter {
		protected override CounterParty CreateReturnObject (Type t) {
			return new AliasInfo ();
		}

		protected override void WriteObjectToWriter(CounterParty counterparty, JsonWriter writer) {
			AliasInfo aliasInfo = counterparty as AliasInfo;
			writer.WritePropertyName ("serverID");
			writer.WriteValue (aliasInfo.serverID);
			writer.WritePropertyName ("iconURL");
			if (aliasInfo.iconURL != null)
				writer.WriteValue (aliasInfo.iconURL);
			else
				writer.WriteNull ();
		}

		protected override void ReadFromJObject (CounterParty counterparty, JObject jsonObject) {
			AliasInfo aliasInfo = counterparty as AliasInfo;
			JToken jtok;
			jtok = jsonObject ["serverID"];
			aliasInfo.serverID = jtok == null ? null : jtok.Value<string>();
			jtok = jsonObject ["iconURL"];
			aliasInfo.iconURL = jtok == null ? null : jtok.Value<string>();
		}

		public override bool CanConvert(Type objectType) {
			return objectType.Equals (typeof(AliasInfo));
		}
	}
}
