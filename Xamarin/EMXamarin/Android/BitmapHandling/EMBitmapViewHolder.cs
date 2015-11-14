using Com.EM.Android;

namespace Emdroid {
	public class EMBitmapViewHolder : Java.Lang.Object, IViewHolder {
		int position = -1;
			
		public int Position {
			get {
				return position;
			}

			set {
				position = value;
			}
		}
	}
}
