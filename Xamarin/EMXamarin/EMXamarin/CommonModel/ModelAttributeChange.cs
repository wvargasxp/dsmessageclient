using System;

namespace em
{
	public class ModelAttributeChange<T,V>
	{
		public T ModelObject { get; set; }
		public string AttributeName { get; set; }
		public V OldValue { get; set; }
		public V NewValue { get; set; }

		public ModelAttributeChange(T mo, string name, V val) {
			ModelObject = mo;
			AttributeName = name;
			NewValue = val;
		}
	}
}

