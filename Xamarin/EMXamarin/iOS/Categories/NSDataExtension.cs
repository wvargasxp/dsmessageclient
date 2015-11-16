using System;
using Foundation;

namespace iOS {
	public static class NSDataExtension {
		public static byte[] ToByteArray (this NSData data) {
			nuint dataLength = data != null ? data.Length : 0;
			if (dataLength > 0) {
				var dataBytes = new byte [data.Length];
				System.Runtime.InteropServices.Marshal.Copy (data.Bytes, dataBytes, 0, Convert.ToInt32 (data.Length));
				return dataBytes;
			}

			return null;
		}
	}
}