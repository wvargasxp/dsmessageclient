using System;
using System.IO;
using System.Collections.Generic;

namespace em {
	public interface ISecurityManager {
		void storeSecureField (String fieldName, String fieldValue);
		string retrieveSecureField (String fieldName);
		bool removeSecureField (String fieldName);

		void SaveSecureKeyValue (string key, string value);
		void SaveSecureKeyValue (Dictionary<string, string> keyValuePairs);
		string GetSecureKeyValue (string key);
		void RemoveSecureKeyValue (string key);
		string CalculateMD5Hash (string input);
		byte[] MD5StreamToBytes (Stream stream);
	}
}