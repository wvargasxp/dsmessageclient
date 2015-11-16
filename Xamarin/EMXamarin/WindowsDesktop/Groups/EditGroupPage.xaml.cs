using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsDesktop.Chat;
using WindowsDesktop.Utility;
using em;

namespace WindowsDesktop.Groups {
	/// <summary>
	/// Interaction logic for EditGroupPage.xaml
	/// </summary>
	public partial class EditGroupPage : Page {

		private bool EditMode { get; set; }

		private SharedEditGroupController Shared { get; set; }

		public EditGroupPage (bool editing, Group group) {
			InitializeComponent ();
			this.EditMode = editing;
			this.Shared = new SharedEditGroupController (App.Instance.Model, group, this);
			SetInitialText (); 
			UpdateDatasource ();
			UpdateImage ();
		}

		private void SetInitialText () {
			this.LabelTextBlock.Text = this.Shared.Group.displayName;
		}

		public void UpdateImage () {
			BitmapImage img = ImageManager.Shared.GetImage (this.Shared.Group);
			this.ThumbnailImage.Source = img;
		}

		public void UpdateDatasource () {
			this.ListView.ItemsSource = EditGroupItemViewModel.From (this.Shared.Group.members).Items;
			this.ListView.Items.Refresh ();
		}

		#region xaml event handlers
		private void LeaveGroupButton_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Leave group button clicked.");
		}

		private void SendMessageButton_Click (object sender, RoutedEventArgs e) {
			MessageBox.Show ("Send message button clicked.");
			this.Shared.SendMessageToGroup ();
		}

		private void ListView_SelectionChanged (object sender, SelectionChangedEventArgs e) {

		}

		private void ListView_MouseDoubleClick (object sender, MouseButtonEventArgs e) {
			int index = this.ListView.SelectedIndex;
			MessageBox.Show ("Row double clicked");
			//BasicWindow window = new BasicWindow (page);
			//window.Show ();
		}
		#endregion

		#region SharedEditGroupController
		public void TransitionToChatController (ChatEntry chatEntry) {
			ChatPage page = new ChatPage (chatEntry);
			// todo
		}
		#endregion
	}

	class SharedEditGroupController : AbstractEditGroupController {
		private WeakReference _r = null;
		private EditGroupPage Self {
			get { return this._r != null ? this._r.Target as EditGroupPage : null; }
			set { this._r = new WeakReference (value); }
		}

		public SharedEditGroupController (ApplicationModel applicationModel, Group g, EditGroupPage p) : base (applicationModel, g) {
			this.Self = p;
		}

		public override void UpdateAliasText (string text) {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void ContactDidChangeThumbnail () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidLoadGroup () {
			EditGroupPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource (); // todo
		}

		protected override void DidLoadGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource (); // todo
		}

		protected override void DidSaveGroup () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidSaveGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidSaveOrUpdateGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidUpdateGroup () {
			EditGroupPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource (); // todo
		}

		protected override void DidUpdateGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidLeaveOrRejoinGroup () {
			EditGroupPage self = this.Self;
			if (self == null) return;
			self.UpdateDatasource (); // todo
		}

		protected override void DidLeaveGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		protected override void DidRejoinGroupFailed () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		public override void DidChangeColorTheme () {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		public override void TransitionToChatController (ChatEntry chatEntry) {
			EditGroupPage self = this.Self;
			if (self == null) return;
		}

		public override string TextInDisplayField {
			get { throw new NotImplementedException (); }
		}

		public override void ListOfMembersUpdated () {
			throw new NotImplementedException ();
		}
	}
}
