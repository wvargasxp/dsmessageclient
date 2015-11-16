using em;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace WindowsDesktop.Utility {
	class BasicEmRowTemplate {
		public CounterParty Counterparty { get; set; }

		public BitmapImage Image {
			get {
				BitmapImage bm = ImageManager.Shared.GetImage (this.Counterparty);
				return bm;
			}
		}

		public string Text {
			get {
				return this.Counterparty.displayName;
			}
		}

		public BasicEmRowTemplate (CounterParty counterparty) {
			this.Counterparty = counterparty;
		}
	}
}
