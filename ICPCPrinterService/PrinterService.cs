using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Printing;
using Mono.Net;

namespace ICPCPrinterService
{
	public class PrinterService
	{
		private HttpListener _listener = new HttpListener();
		private string _servicePath = "/";
		//private byte[] _printFormRawPage;
		private BlockingCollection<PrintTask> _printQueue = new BlockingCollection<PrintTask>();
		private Semaphore _stoppedSignal = new Semaphore(0, 1);

		public ushort Port { get; set; } = 80;

		public string ServicePath
		{
			get { return _servicePath; }
			set
			{
				if (value == null)
					_servicePath = "/";
				else
					_servicePath = value.StartsWith("/") ? value : "/" + value;
			}
		}
		public string RedirectPath { get; set; } = "/";

		public bool IsRunning => _listener.IsListening;

		public int QueueSize => _printQueue.Count;

		public Action<PrintTask> PrintHandler { get; set; }

		//private void LoadPrintFormPage()
		//{
		//	string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
		//	filePath = Path.Combine(Path.GetDirectoryName(filePath), "print.html");
		//	string content = File.OpenText(filePath).ReadToEnd();
		//	_printFormRawPage = Encoding.UTF8.GetBytes(content);
		//}

		private void HandleRequest(HttpListenerContext context)
		{
			var request = context.Request;
			var response = context.Response;

			if (request.HttpMethod == "POST")
			{
				try
				{
					//var buffer = new byte[request.ContentLength64 + 10];
					//int len;
					//len = request.InputStream.Read(buffer, 0, (int)request.ContentLength64);
					//var str = Encoding.UTF8.GetString(buffer, 0, len);
					var str = new StreamReader(request.InputStream).ReadToEnd();
					if (PrintTask.TryParseQueryString(str, out PrintTask printTask, true))
					{
						_printQueue.Add(printTask);
						response.Redirect(RedirectPath);
					}
					else
					{
						response.StatusCode = 400;
					}
				}
				catch (IOException)
				{ }
			}
			else
			{
				response.StatusCode = 405;
				response.StatusDescription = "Method Not Allowed";
			}

			response.Close();
		}

		private void GetContextCallback(IAsyncResult result)
		{
			HttpListenerContext context;

			try
			{
				context = _listener.EndGetContext(result);
			}
			catch (ObjectDisposedException)
			{
				return;
			}

			_listener.BeginGetContext(GetContextCallback, null);
			HandleRequest(context);
		}

		private void StartPrinterLoop()
		{
			new Thread(() =>
			{
				while (true)
				{
					var task = _printQueue.Take();
					if (task == null)
					{
						_stoppedSignal.Release();
						break;
					}

					PrintHandler?.Invoke(task);
				}
			}).Start();
		}

		private void StopPrinterLoop()
		{
			_printQueue.Add(null);
			_stoppedSignal.WaitOne();
		}

		public void Start()
		{
			//LoadPrintFormPage();
			_listener.Prefixes.Clear();
			_listener.Prefixes.Add($"http://+:{Port}{ServicePath}");
			_listener.Start();
			_listener.BeginGetContext(GetContextCallback, null);

			StartPrinterLoop();
		}

		public void Stop()
		{
			_listener.Stop();

			StopPrinterLoop();
		}
	}
}
