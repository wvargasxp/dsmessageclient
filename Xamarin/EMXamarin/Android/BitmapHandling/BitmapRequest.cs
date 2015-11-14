using System;
using Com.EM.Android;
using Android.Support.V7;
using Android.Support.V7.Widget;
using em;
using Android.Graphics;
using Android.Views;
using Android.Content.Res;

namespace Emdroid {
	public class BitmapRequest : Java.Lang.Object, IBitmapRequest {

		private const int SingleViewPosition = -7;
		public Resources Resources { get; set; }

		public CounterParty CounterParty { get; set; }

		// Media
		public RecyclerView.ViewHolder RecyclerHolder { get; set; }
		public EMBitmapViewHolder EMHolder { get; set; }
		public Media Media { get; set; }
		private int MaxHeightInPixels { get; set; }
		private Color Color { get; set; }

		// Thumbnails
		public bool ClipDefaultResource { get; set; }

		public static BitmapRequest From (RecyclerView.ViewHolder holder, Media media, View view, int defaultResource, int maxHeightInPixels, Color color, Resources resources) {
			BitmapRequest rq = new BitmapRequest ();
			rq.RecyclerHolder = holder;
			rq.Media = media;
			rq.View = view;
			rq.DefaultResource = defaultResource;
			rq.MaxHeightInPixels = maxHeightInPixels;
			rq.Color = color;
			rq.Resources = resources;
			return rq;
		}

		public static BitmapRequest FromMedia (RecyclerView.ViewHolder holder, Media media, View view, int defaultResource, int maxHeightInPixels, Resources resources) {
			BitmapRequest rq = new BitmapRequest ();
			rq.RecyclerHolder = holder;
			rq.Media = media;
			rq.View = view;
			rq.DefaultResource = defaultResource;
			rq.MaxHeightInPixels = maxHeightInPixels;
			rq.Resources = resources;
			return rq;
		}

		public static BitmapRequest From (RecyclerView.ViewHolder holder, CounterParty counterparty, View view, int defaultResource, int diameter, Resources resources) {
			BitmapRequest rq = new BitmapRequest ();
			rq.RecyclerHolder = holder;
			rq.Media = counterparty != null ? counterparty.media : null;
			rq.View = view;
			rq.DefaultResource = defaultResource;
			rq.Diameter = diameter;
			rq.Resources = resources;
			rq.CounterParty = counterparty;
			return rq;
		}

		public static BitmapRequest From (EMBitmapViewHolder holder, Resources resources, View view, int defaultResource, int maxHeight) {
			BitmapRequest rq = new BitmapRequest ();
			rq.EMHolder = holder;
			rq.Resources = resources;
			rq.View = view;
			rq.DefaultResource = defaultResource;
			rq.MaxHeightInPixels = maxHeight;
			return rq;
		}

		public bool SingleView {
			get {
				return this.HolderPosition == SingleViewPosition;
			}
		}

		public BitmapRequest ()
		{
		}

		public bool ResultStillMatchesRequest (int p0) {
			if (UsingRecycler ()) {
				return p0 == this.RecyclerHolder.AdapterPosition;
			} else {
				IViewHolder holder = this.EMHolder;
				if (holder != null) {
					return p0 == holder.Position;
				}
			}

			return false;
		}

		public bool ShouldClipDefaultResource () {
			return this.ClipDefaultResource;
		}

		public bool UsingRecycler () {
			return this.RecyclerHolder != null;
		}

		private int _defaultResource;
		public int DefaultResource {
			get { return this._defaultResource; }
			private set { this._defaultResource = value; }
		}

		private int _diameter;
		public int Diameter {
			get { return this._diameter; }
			private set { this._diameter = value;}
		}

		public int HolderPosition {
			get {
				if (UsingRecycler ()) {
					return this.RecyclerHolder.AdapterPosition;
				} else {
					IViewHolder holder = this.EMHolder;
					if (holder != null) {
						return holder.Position;
					}
				}

				return SingleViewPosition; // check BitmapWrapper, it's the constant to denote a single item.
			}
		}

		public int MaxHeight {
			get {
				return this.MaxHeightInPixels;
			}
		}

		private bool _onlyPreload = false;
		public bool OnlyPreload {
			get { return this._onlyPreload; }
			private set { this._onlyPreload = value; }
		}

		public int PreferredColor {
			get {
				return this.Color;
			}
		}

		private View _view = null;
		public Android.Views.View View {
			get { return this._view; }
			set { this._view = value; }
		}
	}
}

