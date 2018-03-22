using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ICPCPrinterService
{
	public class Configuration
	{
		[JsonProperty(PropertyName = "path")]
		public string ServicePath { get; set; } = "/print";

		[JsonProperty(PropertyName = "port")]
		public int Port { get; set; } = 80;

		[JsonProperty(PropertyName = "redirect")]
		public string RedirectPath { get; set; } = "/";

		[JsonProperty(PropertyName = "seatmap")]
		public Dictionary<string, string> SeatMap { get; set; }
	}
}
