#define testing
using Microsoft.Win32;
using OpenCNCPilot.GCode;
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

namespace OpenCNCPilot
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		SettingsWindow SettingsWindow = new SettingsWindow();
		GCodeFile toolpath = GCodeFile.Empty();
		GCodeFile Toolpath
		{
			get { return toolpath; }
			set
			{
				toolpath = value;
				Console.WriteLine("starting visualization");
				Toolpath.GetModel(ModelLine, ModelRapid, ModelArc);
			}	//add 3d update
		}

		public MainWindow()
		{
			InitializeComponent();
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
			SettingsWindow.Close();
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow.Show();
		}

		private void MenuOpenGCode_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog();
			ofd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			ofd.Filter = "GCode|*.tap;*.nc;*.ngc|All Files|*.*";
			ofd.FileOk += GCode_FileOk;
			ofd.ShowDialog();
		}

		private void GCode_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
#if !testing
			try
#endif
			{
				Toolpath = GCodeFile.Load(((OpenFileDialog)sender).FileName);
			}
#if !testing
			catch(ParseException ex)
			{
				MessageBox.Show(ex.Error + " in Line " + ex.Line + 1);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
#endif
		}
	}
}
