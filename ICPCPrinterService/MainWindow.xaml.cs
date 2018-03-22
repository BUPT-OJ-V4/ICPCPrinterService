using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Xps;
using Newtonsoft.Json;
using Microsoft.Win32;

namespace ICPCPrinterService
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private PrinterService _service = new PrinterService();

		private PrintDialog _printDialog = new PrintDialog();

		private bool _isPrinterConfigured = false;

		private double _printableAreaWidth;

		private Configuration _configuration = new Configuration();

		public MainWindow()
		{
			InitializeComponent();
		}


		private void startButton_Click(object sender, RoutedEventArgs e)
		{
			if (!_isPrinterConfigured)
			{
				MessageBox.Show("Please setup printing configurations first", "Cannot Start",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (!ushort.TryParse(portBox.Text, out ushort port))
			{
				MessageBox.Show("Port should be a integer between 1~65535", "Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				_service.Port = port;
				_service.ServicePath = pathBox.Text;
				_service.RedirectPath = redirectBox.Text;
				_service.PrintHandler = PrintHandler;
				_service.Start();

				pathBox.IsEnabled = false;
				portBox.IsEnabled = false;
				startButton.IsEnabled = false;
				configButton.IsEnabled = false;
				stopButton.IsEnabled = true;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void stopButton_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				_service.Stop();

				pathBox.IsEnabled = true;
				portBox.IsEnabled = true;
				startButton.IsEnabled = true;
				configButton.IsEnabled = true;
				stopButton.IsEnabled = false;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message + "\n\n" + ex.StackTrace, "Error",
					MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void configButton_Click(object sender, RoutedEventArgs e)
		{
			if (_printDialog.ShowDialog() == true)
			{
				_printableAreaWidth = _printDialog.PrintableAreaWidth;
				_isPrinterConfigured = true;
			}
		}

		private void PrintHandler(PrintTask printTask)
		{
			Dispatcher.Invoke(() =>
			{
				var doc = new FlowDocument();
				doc.ColumnWidth = _printableAreaWidth;
				doc.PagePadding = new Thickness(25);

				var header = new Run($"{printTask.UserName} ({printTask.UserNickname})")
				{
					FontSize = 11
				};
				if (_configuration.SeatMap.TryGetValue(printTask.UserName, out var seat))
				{
					header.Text += "  " + seat;
				}

				var content = new Run(printTask.Content.Replace("\t", "    "))
				{
					FontFamily = new FontFamily("Consolas"),
					FontSize = 11
				};

				doc.Blocks.Add(new Paragraph(header));
				doc.Blocks.Add(new BlockUIContainer(new Separator()));
				doc.Blocks.Add(new Paragraph(content));

				var writer = System.Printing.PrintQueue.CreateXpsDocumentWriter(_printDialog.PrintQueue);
				writer.Write((doc as IDocumentPaginatorSource).DocumentPaginator);
			});
		}

		private void loadConfigButton_Click(object sender, RoutedEventArgs e)
		{
			var fileDialog = new OpenFileDialog();
			fileDialog.Multiselect = false;
			fileDialog.Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*";
			if (fileDialog.ShowDialog() != true)
				return;

			string data;
			try
			{
				data = File.OpenText(fileDialog.FileName).ReadToEnd();
			}
			catch (IOException ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			try
			{
				_configuration = JsonConvert.DeserializeObject<Configuration>(data);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			pathBox.Text = _configuration.ServicePath ?? "/print";
			portBox.Text = _configuration.Port.ToString();
			redirectBox.Text = _configuration.RedirectPath ?? "/";
		}
	}
}
