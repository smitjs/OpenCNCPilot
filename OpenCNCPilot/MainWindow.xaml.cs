using Microsoft.Win32;
using OpenCNCPilot.GCode;
using System;
using System.Windows;
using System.ComponentModel;

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
				UpdateViewport();
			}
		}

		void UpdateViewport()
		{
			Toolpath.GetModel(ModelLine, ModelRapid, ModelArc);
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
			ofd.FileOk += GCode_OpenFileOk;
			ofd.ShowDialog();
		}

		private void MenuSaveGCode_Click(object sender, RoutedEventArgs e)
		{
			SaveFileDialog sfd = new SaveFileDialog();

			sfd.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
			sfd.Filter = "GCode|*.tap;*.nc;*.ngc|All Files|*.*";
			sfd.FileName = toolpath.FileName;
			sfd.FileOk += GCode_SaveFileOk;
			sfd.ShowDialog();
		}

		private void GCode_SaveFileOk(object sender, CancelEventArgs e)
		{
			try
			{
				toolpath.Save(((SaveFileDialog)sender).FileName);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void GCode_OpenFileOk(object sender, System.ComponentModel.CancelEventArgs e)
		{
			try
			{
				Toolpath = GCodeFile.Load(((OpenFileDialog)sender).FileName);
			}
			catch(ParseException ex)
			{
				MessageBox.Show(ex.Error + " in Line " + ex.Line + 1);
			}
			catch(Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
