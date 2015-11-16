using System;

namespace em
{
	public class ModelStructureChange<T>
	{
		public ModelStructureChange() {
		}

		public ModelStructureChange(T model, ModelStructureChange changeType) {
			ModelObject = model;
			Change = changeType;
		}

		public ModelStructureChange(T model, ModelStructureChange changeType, int ind) {
			ModelObject = model;
			Change = changeType;
			Index = ind;
		}

		public T ModelObject { get; set; }
		public ModelStructureChange Change { get; set; }
		public int Index { get; set; }
	}

	public enum ModelStructureChange {
		added,
		deleted,
		moved
	}
}

