using OpenCNCPilot.Util;
using System.Data;
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
using System.Windows.Shapes;
using System;
using OpenCNCPilot.GCode;

namespace OpenCNCPilot.UI
{
	/// <summary>
	/// Interaktionslogik für CreateHeightMapDialog.xaml
	/// </summary>
	public partial class CreateHeightMapDialog : Window
	{
		public CreateHeightMapDialog()
		{
			InitializeComponent();
		}

		public double MinX { get; set; } = 0.0;
		public double MinY { get; set; } = 0.0;
		public double MaxX { get; set; } = 100.0;
		public double MaxY { get; set; } = 50.0;
		public int SizeX { get; set; } = 11;
		public int SizeY { get; set; } = 6;

		public bool GenerateTestPattern { get; set; } = false;
		public string TestPattern { get; set; } = "(x * x + y * y) / 1000.0";

		public HeightMap OutputMap;

		private void buttonGenerate_Click(object sender, RoutedEventArgs e)
		{
			if(MinX > MaxX || MinY > MaxY)
			{
				MessageBox.Show("Coordinates of the lower left corner must be smaller than upper right ones");
				return;
			}

			if(SizeX < 1 || SizeY < 1)
			{
				MessageBox.Show("Array Size must be grater than zero");
				return;
			}

			OutputMap = new HeightMap(SizeX, SizeY, new Vector2(MinX, MinY), new Vector2(MaxX, MaxY));

			if(GenerateTestPattern)
			{
				DataTable t = new DataTable();
				try {

					for (int x = 0; x < SizeX; x++)
					{
						for (int y = 0; y < SizeY; y++)
						{
							double X = (x * (MaxX - MinX)) / (SizeX - 1) + MinX;
							double Y = (y * (MaxY - MinY)) / (SizeY - 1) + MinY;

							decimal d = (decimal)t.Compute(TestPattern.Replace("x", X.ToString()).Replace("y", Y.ToString()), "");
							OutputMap.AddPoint(x, y, (double)d);
						}
					}
				}
				catch(Exception ex)
				{
					MessageBox.Show("Error while evaluating expression:\n" + ex.Message);
					return;
				}
			}

			DialogResult = true;
			Close();
		}

		private void buttonCancel_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if(!DialogResult.HasValue)
				DialogResult = false;
		}
	}
}
