using Microsoft.Win32;
using OpenCNCPilot.GCode;
using System;
using System.Windows;
using OpenCNCPilot.Util;
using System.IO;

namespace OpenCNCPilot.UI
{
	public partial class MainWindow : Window
	{
		OpenFileDialog openFileDialog = new OpenFileDialog() { InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) };
		SaveFileDialog saveFileDialog = new SaveFileDialog() { InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) };

		GCodeFile Toolpath = GCodeFile.Empty();
		HeightMap HMap = new HeightMap(0, 0, new Vector2(0, 0), new Vector2(0, 0));

		public MainWindow()
		{
			InitializeComponent();
		}

		void UpdateToolpath()
		{
			Toolpath.GetModel(ModelLine, ModelRapid, ModelArc);
		}

		void UpdateHeightMap()
		{
			HMap.GetModel(ModelHeightMap, ModelHeightMapBoundary);
		}

		private void Window_Closed(object sender, EventArgs e)
		{
			Properties.Settings.Default.Save();
			Application.Current.Shutdown();
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			SettingsWindow sw = new SettingsWindow();
			sw.ShowDialog();
		}

		#region FileIO

		private void MenuOpenGCode_Click(object sender, RoutedEventArgs e)
		{
			openFileDialog.Filter = Constants.FileFilterGCode;
			openFileDialog.FileName = "";

			if(openFileDialog.ShowDialog().Value)
			{
				try
				{
					Toolpath = GCodeFile.Load(openFileDialog.FileName);

					UpdateToolpath();
				}
				catch (ParseException ex)
				{
					MessageBox.Show(ex.Error + " in Line " + (ex.Line + 1));
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		private void MenuSaveGCode_Click(object sender, RoutedEventArgs e)
		{ 
			saveFileDialog.Filter = Constants.FileFilterGCode;
			saveFileDialog.FileName = Toolpath.FileName;

			if(saveFileDialog.ShowDialog().Value)
			{
				try
				{
					Toolpath.Save(saveFileDialog.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		private void MenuOpenHeightMap_Click(object sender, RoutedEventArgs e)
		{
			openFileDialog.Filter = Constants.FileFilterHeightMap;
			openFileDialog.FileName = "";
			
			if(openFileDialog.ShowDialog().Value)
			{
				try
				{
					HMap = HeightMap.Load(openFileDialog.FileName);
					HMap.MapUpdated += UpdateHeightMap;

					UpdateHeightMap();
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		private void MenuSaveHeightMap_Click(object sender, RoutedEventArgs e)
		{
			saveFileDialog.Filter = Constants.FileFilterHeightMap;
			saveFileDialog.FileName = "";

			if (saveFileDialog.ShowDialog().Value)
			{
				try
				{
					HMap.Save(saveFileDialog.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show(ex.Message);
				}
			}
		}

		private void MenuViewGCode_Click(object sender, RoutedEventArgs e)
		{
			string path = Path.GetTempFileName();

			try
			{
				Toolpath.Save(path);
                System.Diagnostics.Process.Start(Properties.Settings.Default.ExternalEditor, path);
            }
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
		#endregion

		private void MenuCreateHeightMap_Click(object sender, RoutedEventArgs e)
		{
			CreateHeightMapDialog chmd = new CreateHeightMapDialog();

			if (chmd.ShowDialog().Value)
			{
				HMap = chmd.OutputMap;
				HMap.MapUpdated += UpdateHeightMap;
				UpdateHeightMap();
			}
		}

		private void MenuItemConnect_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
