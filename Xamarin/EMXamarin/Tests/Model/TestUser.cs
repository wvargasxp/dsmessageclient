using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;
using EMXamarin;
using em;

namespace Tests
{
	public class TestUser
	{
		public string identifier { get; set; }
		public string identifierType { get; set; }

		public ApplicationModel appModel { get; set; }
		public TestsManager testManager { get; set; }
		//device info
		//platform is automatically iOS, system name automatically iPhone OS, version is automatically 6.01
		public string deviceName { get; set; }
		public string deviceModel { get; set; } //iPod touch, iPhone, iPhone Simulator, iPad, iPad Simulator
		public string identifierForVendor { get; set; } //com.example.app.app1
		public string colorTheme { get; set; }

		public List<AddressBookContact> listOfContacts { get; set; }

		public TestUser (Dictionary<string, string> properties) 
		{
		}
	}

	public class TestUserDB
	{
		private static string usersFilename = "model/users.json";
		private static string abFilename = "model/addressBooks.json";
		private static List<TestUser> users;

		public static TestUser GetUserAtIndex(int index) 
		{
			if (users != null && index < users.Count) {
				return users[index];
			}
			return null;
		}

		public static void LoadUsers(){
			//load users from file
			string json = System.IO.File.ReadAllText(usersFilename);
			users = JsonConvert.DeserializeObject<List<TestUser>> (json);

			string addressJson = System.IO.File.ReadAllText (abFilename);
			List<List<AddressBookContact>> addressBooks = JsonConvert.DeserializeObject<List<List<AddressBookContact>>> (addressJson);
			for (int i = 0; i < users.Count; i++) {
				users [i].listOfContacts = addressBooks [i];
				users [i].appModel = new ApplicationModel (new TestPlatformFactory (i));
				users [i].testManager = new TestsManager (users [i].appModel);
			}
			Debug.WriteLine ("just for breakpoint");
		}
	}
}

