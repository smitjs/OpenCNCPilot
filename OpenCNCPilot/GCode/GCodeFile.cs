using OpenCNCPilot.GCode.GCodeCommands;
using System.Collections.Generic;
using System.Linq;
using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;
using OpenCNCPilot.Properties;
using System.Globalization;
using System.IO;

namespace OpenCNCPilot.GCode
{
	class GCodeFile
	{
		public List<Command> Toolpath;
		public string FileName = string.Empty;

		public Vector3 Min = Vector3.MaxValue;
		public Vector3 Max = Vector3.MinValue;
		public Vector3 Size;


		private GCodeFile(List<Command> toolpath)
		{
			Toolpath = toolpath;

			foreach (Motion m in Enumerable.Concat<Motion>(Toolpath.OfType<Line>(), Toolpath.OfType<Arc>().SelectMany(a => a.Split(0.1))))
			{
				for (int i = 0; i < 3; i++)
				{
					if (m.End[i] > Max[i])
						Max[i] = m.End[i];

					if (m.End[i] < Min[i])
						Min[i] = m.End[i];
				}
			}

			Size = Max - Min;

			for (int i = 0; i < 3; i++)
			{
				if (Size[i] < 0)
					Size[i] = 0;
			}
		}

		public static GCodeFile Load(string path)
		{
			GCodeParser.Reset();
			GCodeParser.ParseFile(path);

			return new GCodeFile(GCodeParser.Commands) { FileName = path.Substring(path.LastIndexOf('\\') + 1)};
		}

		public static GCodeFile Empty()
		{
			return new GCodeFile(new List<Command>());
		}

		public void Save(string path)
		{
			File.WriteAllLines(path, GetGCode());
		}

		public void GetModel(LinesVisual3D line, LinesVisual3D rapid, LinesVisual3D arc)
		{
			Point3DCollection linePoints = new Point3DCollection();
			Point3DCollection rapidPoints = new Point3DCollection();
			Point3DCollection arcPoints = new Point3DCollection();

			foreach (Command c in Toolpath)
			{
				var l = c as Line;

				if (l != null)
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

				if (a != null)
				{
					foreach (Motion sub in a.Split(Settings.Default.ViewportArcSplit))
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

		public List<string> GetGCode()
		{
			List<string> GCode = new List<string>() { "G90 G91.1 G21 G17" };

			NumberFormatInfo nfi = new NumberFormatInfo();
			nfi.NumberDecimalSeparator = ".";   //prevent problems with international versions of windows (eg Germany would write 25.4 as 25,4 which is not compatible with standard GCode)
			nfi.NumberDecimalDigits = Settings.Default.OutputDecimalDigits;

			ParserState State = new ParserState();

			foreach (Command c in Toolpath)
			{
				if (c is Motion)
				{
					Motion m = c as Motion;

					if (m.Feed != State.Feed)
					{
						GCode.Add(string.Format(nfi, "F{0}", m.Feed));

						State.Feed = m.Feed;
					}
				}

				if (c is Line)
				{
					Line l = c as Line;

					string code = l.Rapid ? "G0" : "G1";

					if (State.Position.X != l.End.X)
						code += string.Format(nfi, "X{0}", l.End.X);
					if (State.Position.Y != l.End.Y)
						code += string.Format(nfi, "Y{0}", l.End.Y);
					if (State.Position.Z != l.End.Z)
						code += string.Format(nfi, "Z{0}", l.End.Z);

					GCode.Add(code);

					State.Position = l.End;

					continue;
				}

				if (c is Arc)
				{
					Arc a = c as Arc;

					if (State.Plane != a.Plane)
					{
						switch (a.Plane)
						{
							case ArcPlane.XY:
								GCode.Add("G17");
								break;
							case ArcPlane.YZ:
								GCode.Add("G19");
								break;
							case ArcPlane.ZX:
								GCode.Add("G18");
								break;
						}
						State.Plane = a.Plane;
					}

					string code = a.Direction == ArcDirection.CW ? "G2" : "G3";

					if (State.Position.X != a.End.X)
						code += string.Format(nfi, "X{0}", a.End.X);
					if (State.Position.Y != a.End.Y)
						code += string.Format(nfi, "Y{0}", a.End.Y);
					if (State.Position.Z != a.End.Z)
						code += string.Format(nfi, "Z{0}", a.End.Z);

					Vector3 Center = new Vector3(a.U, a.V, 0).RollComponents((int)a.Plane) - State.Position;

					if (Center.X != 0 && a.Plane != ArcPlane.YZ)
						code += string.Format(nfi, "I{0}", Center.X);
					if (Center.Y != 0 && a.Plane != ArcPlane.ZX)
						code += string.Format(nfi, "J{0}", Center.Y);
					if (Center.Z != 0 && a.Plane != ArcPlane.XY)
						code += string.Format(nfi, "K{0}", Center.Z);

					GCode.Add(code);
					State.Position = a.End;

					continue;
				}

				if (c is MCode)
				{
					GCode.Add($"M{((MCode)c).Code}");

					continue;
				}
			}

			return GCode;
		}
	}
}
