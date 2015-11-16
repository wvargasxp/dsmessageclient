namespace em {
	public class ChangeInstruction<T> {
		public T Entry { get; set; }
		public bool PhotoChanged { get; set; }
		public bool NameChanged { get; set; }
		public bool PreviewChanged { get; set; }
		public bool ColorThemeChanged { get; set; }

		public ChangeInstruction (T c, bool photo, bool name, bool preview, bool theme) {
			this.Entry = c;
			this.PhotoChanged = photo;
			this.NameChanged = name;
			this.PreviewChanged = preview;
			this.ColorThemeChanged = theme;
		}

		public override string ToString () {
			return string.Format ("[ChangeInstruction: Entry={0}, PhotoChanged={1}, NameChanged={2}, PreviewChanged={3}, ColorThemeChanged={4}]", Entry, PhotoChanged, NameChanged, PreviewChanged, ColorThemeChanged);
		}

	}
}