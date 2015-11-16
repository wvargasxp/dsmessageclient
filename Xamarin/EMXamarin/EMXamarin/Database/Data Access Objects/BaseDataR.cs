namespace em {
	public class BaseDataR {
		public ApplicationModel appModel { get; set;}
		public bool isPersisted { get; set; }

		public BaseDataR () {
			isPersisted = true;
		}
	}
}