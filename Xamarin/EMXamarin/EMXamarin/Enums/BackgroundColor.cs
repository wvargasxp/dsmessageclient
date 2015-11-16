using System;

namespace em {
	public class BackgroundColor {

		public static BackgroundColor Blue = FromHexString("#1381D0");
		public static BackgroundColor Green = FromHexString("#23C303");
		public static BackgroundColor Pink = FromHexString("#BD187B");
		public static BackgroundColor Orange = FromHexString("FC9938");
		public static BackgroundColor Purple = FromHexString("#632F6D");
		public static BackgroundColor Gray = FromHexString("#3C3C3C");
		public static BackgroundColor Default = FromHexString("#3C3C3C");

		public static readonly BackgroundColor[] AllColors = { BackgroundColor.Gray, BackgroundColor.Pink, BackgroundColor.Orange, BackgroundColor.Green, BackgroundColor.Blue, BackgroundColor.Purple };

		private string hexString;

		public string HexString {
			get {
				return this.hexString;
			}
			set {
				if (value == null)
					return;
				this.hexString = value;
				if (!value.Substring (0, 1).Equals ("#")) {
					this.hexString = "#" + this.hexString;
				}
			}
		}

		public int[] GetRGB () {
			int[] rgbArray = new int[3];
			string hex = this.HexString;
			for (int i = 1; i <= 3; i++) {
				string component = HexString.Substring (2 * i - 1, 2);
				int value = Convert.ToInt32 (component, 16);
				rgbArray [i - 1] = value;
			}
			return rgbArray;
		}

		public static BackgroundColor FromHexString(string name) {
			BackgroundColor color = new BackgroundColor ();
			color.HexString = name;
			return color;
		}

		public string ToHexString() {
			return this.HexString;
		}

		public override bool Equals (object obj) {
			if (obj == null || !(obj.GetType ().Equals (this.GetType ())))
				return false;
			BackgroundColor other = (BackgroundColor)obj;
			return other.HexString.Equals (this.HexString);
		}

		public static bool operator ==(BackgroundColor left, BackgroundColor right) {
			return left.Equals (right);
		}

		public static bool operator !=(BackgroundColor left, BackgroundColor right) {
			return !left.Equals (right);
		}
	}
}

