using Gtk;
using System;
using System.Collections.Generic;
using System.Resources;
using System.Drawing;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Utils;
using VkNet.Model.RequestParams;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;

namespace BanhammerGTK
{
	public class MainWindow
	{
		Builder b = new Builder ();
		CssProvider css = new CssProvider ();
		VkApi vk = new VkApi();

		//Скачиваем картинку в массив, если хотим отобразить потом оригинал
		public static byte[] DownloadImageToBytes(string url){
			byte[] imageBytes;
			using (var webClient = new WebClient ()) {
				imageBytes = webClient.DownloadData (url);
			}
			return imageBytes;

		}

		//Закругляем картинку
		public static System.Drawing.Image OvalImage(System.Drawing.Image img) {
			Bitmap bmp = new Bitmap(img.Width, img.Height);
			using (System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath()) {
				gp.AddEllipse(0, 0, img.Width, img.Height);
				using (System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(bmp)) {
					gr.SetClip(gp);
					gr.DrawImage(img, System.Drawing.Point.Empty);
				}
			}
			return bmp;
		}

		//Из картинки в массив 
		public static byte[] ImageToByte(System.Drawing.Image x){
			ImageConverter _imageConverter = new ImageConverter();
			byte[] xByte = (byte[])_imageConverter.ConvertTo(x, typeof(byte[]));
			return xByte;
		}

		//Из массива в картинку
		public static System.Drawing.Image FromBytesToImage(byte[] imageBytes){
			using (var ms = new MemoryStream(imageBytes))
			{
				return System.Drawing.Image.FromStream(ms);
			}
		}


		// РИСУЕМ КРУГЛУЮ ПИКЧУ СРЕДСТВАМИ СИШАРПА (Я ЗАЕБАЛСЯ ДЕЛАТЬ ЭТО НА CAIRO)
		public static byte[] roundedImage(byte[] imageBytes){
			System.Drawing.Image sysimage = FromBytesToImage(imageBytes);
			var roundedImage = OvalImage (sysimage);
			imageBytes = ImageToByte (roundedImage);
			return imageBytes;
		}

		//Сразу скачиваем и округляем картинку по ее url
		public static byte[] DownloadRoundedImage(string url){
			byte[] image = 	roundedImage (DownloadImageToBytes (url));
			return image;
		}

		//Проверяем подписки по паттерну
		static public bool CheckSubs(VkCollection<VkNet.Model.Group> subs, string pattern)
		{
			foreach (var v in subs)
			{
				if (v.Type == null)
				{
					continue;
				}
				if (Regex.IsMatch(v.Name, pattern))
				{
					Console.WriteLine(v.Name);
					return true;
				}
			}
			return false;
		}

		//Метод создания строк сообществ 
		public Gtk.ListBoxRow CommunityRow(VkNet.Model.Group community){
			
			Gtk.ListBoxRow newRow = new Gtk.ListBoxRow();
			Gtk.Grid grid = new Gtk.Grid();
			Gtk.Image img = new Gtk.Image();
			Gtk.VBox box = new Gtk.VBox();

			StyleContext context = newRow.StyleContext;
			context.AddProvider (css, StyleProviderPriority.Application);
			context.AddClass ("community-row");

			var communityHeader = new Gtk.Label ();
			var communityType = new Gtk.Label ();
			var communityPopulation = new Gtk.Label ();

			grid.Orientation = Orientation.Horizontal;

			box.Orientation = Orientation.Vertical;

			communityHeader.MarginTop = 6;
			communityHeader.MarginBottom = 12;
			communityHeader.MarginStart = 12;
			communityHeader.Halign = Align.Start;

			context = communityHeader.StyleContext;

			context.AddClass ("community-header");



			communityType.MarginBottom = 12;
			communityType.MarginStart = 12;
			communityType.Halign = Align.Start;

			context = communityType.StyleContext;
			context.AddClass ("community-text");

			communityPopulation.MarginStart = 12;
			communityPopulation.MarginBottom = 6;
			communityPopulation.Halign = Align.Start;

			context = communityPopulation.StyleContext;
			context.AddClass ("community-text");

			img.WidthRequest = 100;
			img.HeightRequest = 100;

			grid.Add (img);
			grid.Add (box);

			newRow.Add (grid);
			newRow.WidthRequest = 100;
			newRow.HeightRequest = 80;
			newRow.MarginTop = 12;
			newRow.MarginBottom = 12;
			newRow.MarginEnd = 12;

			box.Add (communityHeader);
			box.Add (communityType);
			box.Add (communityPopulation);


			communityHeader.Text = community.Name;

			if (community.Type == VkNet.Enums.SafetyEnums.GroupType.Group) {
				communityType.Text = "Группа";
			} else if (community.Type == VkNet.Enums.SafetyEnums.GroupType.Page) {
				communityType.Text = "Публичная страница";
			} else if (community.Type == VkNet.Enums.SafetyEnums.GroupType.Event) {
				communityType.Text = "Мероприятие";
			}

			var pop = community.MembersCount % 10;
			if (pop == 0 || (pop >= 5 && pop <= 9)) {
				communityPopulation.Text = community.MembersCount.ToString () + " участников";
			} else if (pop == 1) {
				communityPopulation.Text = community.MembersCount.ToString () + " участник";
			} else if (pop > 1 && pop <= 4) {
				communityPopulation.Text = community.MembersCount.ToString () + " участника";
			}

			newRow.TooltipText = community.Name;

			img.Pixbuf = new Gdk.Pixbuf (DownloadRoundedImage (community.Photo200.ToString ())).ScaleSimple (img.WidthRequest, img.HeightRequest, Gdk.InterpType.Bilinear);

			return newRow;

		}
		public void SelectRow( object obj, EventArgs args){
			var scrolledwindow = b.GetObject ("community-scrolled-window") as ScrolledWindow;
			GLib.Value name = (b.GetObject ("community-list") as ListBox).SelectedRow.GetProperty ("tooltip_text");
			(b.GetObject ("main-stack") as Stack).ChildSetProperty (scrolledwindow, "title", name);
		}


		//Разделитель, на всякий случай
		public Gtk.Label Separator(){
			var label = new Gtk.Label ();

			label.MarginEnd = 21;
			label.HeightRequest = 1;

			StyleContext context = label.StyleContext;
			context.AddProvider (css, StyleProviderPriority.Application);
			context.AddClass ("line");

			label.Sensitive = false;

			return label;
		}

		public MainWindow(){
			Application.Init ();


			b.AddFromFile ("../../Resources/UI/login_new.xml");
			b.Autoconnect (this);


			css.LoadFromPath("../../Resources/css/ui_common.css");

			StyleContext.AddProviderForScreen (Gdk.Screen.Default, css, StyleProviderPriority.Application);

			var loginBtn = b.GetObject ("login-btn") as Button;
			loginBtn.Clicked += onLoginClick;

			Application.Run ();

		}

		public void SecondWindow(){

			var user = vk.Users.Get ((long)vk.UserId, ProfileFields.Photo50);

			(b.GetObject("window") as Window).Close();
			b.AddFromFile("../../Resources/UI/main_window.xml");
			b.Autoconnect(this);

			var userpic = b.GetObject ("userpic") as Gtk.Image;

			userpic.Pixbuf = new Gdk.Pixbuf(DownloadRoundedImage(user.Photo50.ToString())).ScaleSimple(userpic.WidthRequest, userpic.HeightRequest, Gdk.InterpType.Bilinear);

			var username = b.GetObject ("username") as Gtk.Label;
			username.Text = user.FirstName;

			var stackswitcher = b.GetObject ("stack-switcher") as StackSwitcher;
			stackswitcher.Padding = 0;

			//Собираем все группы, которые может модерировать пользователь
			var communities = vk.Groups.Get (new GroupsGetParams () {
				UserId = user.Id,
				Extended = true,
				Filter = GroupsFilters.Administrator | GroupsFilters.Editor | GroupsFilters.Moderator,
				Fields = GroupsFields.MembersCount
			});


			var list = b.GetObject ("community-list") as ListBox;


			int pos = 0;
			List<Gtk.ListBoxRow> rows = new List<Gtk.ListBoxRow>();
			foreach (var v in communities) {
				rows.Add (CommunityRow (v));
			}
			foreach (var v in rows) {
				list.Add (rows[pos]);
				pos++;
			}

			list.ShowAll ();
			list.RowSelected += SelectRow;
		}

		public void onLoginClick( object obj, EventArgs args){
			var email = b.GetObject ("email") as Entry;
			var password = b.GetObject ("password") as Entry;
			var label = b.GetObject ("login-txt") as Label;

			VkNet.Enums.Filters.Settings scope = VkNet.Enums.Filters.Settings.Groups;
			try{
				vk.Authorize(new ApiAuthParams
				{
					ApplicationId = 4379189,
					Login = email.Text,
					Password = password.Text,
					Settings = scope
				});
				SecondWindow();
			}
			catch {
				label.Text = "Неверный логин/пароль";
			}
			

		}

		public void OnRightButtonClick(object obj, EventArgs args){
			
		}

		static void onDeleteEvent(object o, DeleteEventArgs args){
			Application.Quit();
		}

		public static void Main(){
			new MainWindow ();
		}
	}
}
	