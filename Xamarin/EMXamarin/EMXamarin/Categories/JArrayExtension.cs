using System;
using Newtonsoft.Json.Linq;

namespace emExtension
{
	public static class JArrayExtension
	{
		public static T[] convertToNativeArray<T>(this JArray jarray) {
			T[] retVal = new T[ jarray.Count ];
			int i=0;
			foreach ( JValue value in jarray )
				retVal[i++] = value.Value<T>();

			return retVal;
		}
	}
}

