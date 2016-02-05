using OpenCNCPilot.GCode.GCodeCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using OpenCNCPilot.Properties;

namespace OpenCNCPilot.GCode
{
	class GCodeFile
	{
		public List<Command> Toolpath;

		public Vector3 Min = Vector3.MaxValue;
		public Vector3 Max = Vector3.MinValue;
		public Vector3 Size;


		private GCodeFile(List<Command> toolpath)
		{
			Toolpath = toolpath;

			foreach (Motion m in Enumerable.Concat<Motion>(Toolpath.OfType<Line>(), Toolpath.OfType<Arc>().SelectMany(a => a.Split(0.1))))
			{
				for(int i = 0; i < 3; i++)
				{
					if (m.End[i] > Max[i])
						Max[i] = m.End[i];

					if (m.End[i] < Min[i])
						Min[i] = m.End[i];
				}
			}

			Size = Max - Min;

			for(int i = 0; i < 3; i++)
			{
				if (Size[i] < 0)
					Size[i] = 0;
			}
		}

		public static GCodeFile Load(string path)
		{
			GCodeParser.Reset();
			GCodeParser.ParseFile(path);

			return new GCodeFile(GCodeParser.State.Commands);
		}

		public static GCodeFile Empty()
		{
			return new GCodeFile(new List<Command>());
		}

		public void GetModel(LinesVisual3D line, LinesVisual3D rapid, LinesVisual3D arc)
		{
			Point3DCollection linePoints = new Point3DCollection();
			Point3DCollection rapidPoints = new Point3DCollection();
			Point3DCollection arcPoints = new Point3DCollection();

			foreach (Command c in Toolpath)
			{
				var l = c as Line;

				if(l != null)
				{
					if (l.Rapid)
					{
						rapidPoints.Add(l.Start.ToPoint3D());
						rapidPoints.Add(l.End.ToPoint3D());
					}
					else
					{
						linePoints.Add(l.Start.ToPoint3D());
						linePoints.Add(l.End.ToPoint3D());
					}
					continue;
				}

				var a = c as Arc;

				if(a != null)
				{
					foreach(Motion sub in a.Split(Settings.Default.ViewportArcSplit))
					{
						arcPoints.Add(sub.Start.ToPoint3D());
						arcPoints.Add(sub.End.ToPoint3D());
					}
				}
			}

			line.Points = linePoints;
			rapid.Points = rapidPoints;
			arc.Points = arcPoints;
		}
	}
}
