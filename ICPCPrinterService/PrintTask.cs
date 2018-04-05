using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace ICPCPrinterService
{

	[Serializable]
	public class PrintTask
	{
		[JsonProperty(PropertyName = "username")]
		public string Username { get; set; }

		[JsonProperty(PropertyName = "nickname")]
		public string UserNickname { get; set; }

		[JsonProperty(PropertyName = "content")]
		public string Content { get; set; }

		public static bool TryParseJson(string str, out PrintTask result)
		{
			try
			{
				result = JsonConvert.DeserializeObject<PrintTask>(str);
				return result != null;
			}
			catch (Exception)
			{
				result = null;
				return false;
			}
		}

		public static bool TryParseQueryString(string str, out PrintTask result, bool obfused = false)
		{
			try
			{
				var data = new Dictionary<string, string>();
				var seperator = new char[] { '=' };
				foreach (var field in str.Split('&'))
				{
					var kv = field.Split(seperator, 2);
					data[HttpUtility.UrlDecode(kv[0])] = kv.Length > 1 ? HttpUtility.UrlDecode(kv[1]) : "";
				}
				result = new PrintTask();
				if (obfused)
				{
					if (data.TryGetValue("authinfo", out var authinfo))
					{
						var bytes = Convert.FromBase64String(authinfo);
						for (int i = 0; i < bytes.Length; i += 2)
						{
							bytes[i + 1] ^= (byte)(0xff & ~bytes[i]);
							bytes[i] ^= bytes[i + 1];
						}
						var start = bytes[0] == 0 ? 1 : 0;
						authinfo = Encoding.UTF8.GetString(bytes, start, bytes.Length - start);
						var splitted = authinfo.Split(new[] { '|' }, 2);
						if (splitted.Length == 2)
						{
							result.Username = splitted[0];
							result.UserNickname = splitted[1];
						}
					}
				}
				else
				{
					if (data.TryGetValue("username", out var username))
						result.Username = username;
					if (data.TryGetValue("nickname", out var nickname))
						result.UserNickname = nickname;
				}
				if (data.TryGetValue("content", out var content))
					result.Content = content;
				return true;
			}
			catch (Exception)
			{
				result = null;
				return false;
			}
		}
	}
}
