using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenCNCPilot.GCode.GCodeCommands;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenCNCPilot.GCode
{
	enum ParseDistanceMode
	{
		Absolute,
		Incremental
	}

	enum ParseUnit
	{
		MM,
		In
	}

	class ParserState
	{
		public Vector3 Position;
		public ArcPlane Plane;
		public double Feed;
		public ParseDistanceMode DistanceMode;
		public ParseDistanceMode ArcDistanceMode;
		public ParseUnit Unit;
		public int LastMotionMode;

		public List<Command> Commands;
	}

	struct Word
	{
		public char Command;
		public float Parameter;
	}

	public class GCodeParser
	{
		ParserState State;

		private static Regex GCodeSplitter = new Regex(@"([A-Z])\s*(\-?\d+\.?\d*)", RegexOptions.Compiled);
		private static double[] MotionCommands = new double[] { 0, 1, 2, 3 };

		public GCodeParser()
		{
			State = new ParserState();

			State.Position = new Vector3();
			State.Plane = ArcPlane.XY;
			State.Feed = 100;
			State.DistanceMode = ParseDistanceMode.Absolute;
			State.ArcDistanceMode = ParseDistanceMode.Incremental;
			State.Unit = ParseUnit.MM;
			State.LastMotionMode = -1;

			State.Commands = new List<Command>();
		}

		public void ParseFile(string path)
		{
			string[] file = File.ReadAllLines(path);

			for (int i = 0; i < file.Length; i++)
			{
				string line = CleanupLine(file[i], i);

				if (string.IsNullOrWhiteSpace(line))
					continue;

				Parse(line, i);
			}
		}

		string CleanupLine(string line, int lineNumber)
		{
			int commentIndex = line.IndexOf(';');

			if (commentIndex > -1)
				line = line.Substring(0, commentIndex);

			while (line.Contains('('))
			{
				int start = line.IndexOf('(');
				int end = line.IndexOf(')');

				if (end < start)
					throw new ParseException("mismatched parentheses", lineNumber);

				line = line.Remove(start, end - start);
			}

			return line;
		}

		void Parse(string line, int lineNumber)
		{
			MatchCollection matches = GCodeSplitter.Matches(line);

			List<Word> Words = new List<Word>(matches.Count);

			foreach (Match match in matches)
			{
				Words.Add(new Word() { Command = match.Groups[1].Value[0], Parameter = int.Parse(match.Groups[2].Value) });
			}

			for (int i = 0; i < Words.Count; i++)
			{
				if (Words[i].Command != 'F')
					continue;
				State.Feed = Words.First().Parameter;
				Words.RemoveAt(i);
				continue;
			}

			while (Words.Count > 0)
			{
				if (Words.First().Command == 'M')
				{
					int param = (int)Words.First().Parameter;

					if (param != Words.First().Parameter || param < 0)
						throw new ParseException("MCode can only have integer parameters", lineNumber);

					State.Commands.Add(new MCode() { Code = param });

					Words.RemoveAt(0);
					continue;
				}

				if (Words.First().Command == 'G' && !MotionCommands.Contains(Words.First().Parameter))
				{
					#region UnitPlaneDistanceMode

					float param = Words.First().Parameter;

					if (param == 90)
					{
						State.DistanceMode = ParseDistanceMode.Absolute;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 91)
					{
						State.DistanceMode = ParseDistanceMode.Incremental;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 90.1)
					{
						State.ArcDistanceMode = ParseDistanceMode.Absolute;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 91.1)
					{
						State.ArcDistanceMode = ParseDistanceMode.Incremental;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 21)
					{
						State.Unit = ParseUnit.MM;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 20)
					{
						State.Unit = ParseUnit.In;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 17)
					{
						State.Plane = ArcPlane.XY;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 18)
					{
						State.Plane = ArcPlane.ZX;
						Words.RemoveAt(0);
						continue;
					}
					if (param == 19)
					{
						State.Plane = ArcPlane.YZ;
						Words.RemoveAt(0);
						continue;
					}

					Words.RemoveAt(0);  //unsupported G-Command
					continue;
					#endregion
				}

				if (Words.First().Command == 'T')
				{
					Words.RemoveAt(0);  //no toolchanges supported
					continue;
				}

				break;
			}

			if (Words.Count == 0)
				return;

			int MotionMode = State.LastMotionMode;

			if (Words.First().Command == 'G')
			{
				MotionMode = (int)Words.First().Parameter;
				State.LastMotionMode = MotionMode;
				Words.RemoveAt(0);
			}

			if (MotionMode < 0)
				throw new ParseException("No Motion Mode active", lineNumber);

			Vector3 EndPos = State.Position;

			#region FindEndPos
			{
				int Incremental = (State.DistanceMode == ParseDistanceMode.Incremental) ? 1 : 0;

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'X')
						continue;
					EndPos.X = Words[i].Parameter + Incremental * EndPos.X;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'Y')
						continue;
					EndPos.Y = Words[i].Parameter + Incremental * EndPos.Y;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'Z')
						continue;
					EndPos.Z = Words[i].Parameter + Incremental * EndPos.Z;
					Words.RemoveAt(i);
					break;
				}
			}
			#endregion

			if (MotionMode <= 1)
			{
				if (Words.Count > 0)
					throw new ParseException("Motion Command must be last in line (unused Words in Block)", lineNumber);

				Line motion = new Line();
				motion.Start = State.Position;
				motion.End = EndPos;
				motion.Feed = State.Feed;
				motion.Rapid = MotionMode == 0;

				State.Commands.Add(motion);
				State.Position = EndPos;
				return;
			}

			double U, V;

			bool IJKused = false;

			switch (State.Plane)
			{
				default:
					U = State.Position.X;
					V = State.Position.Y;
					break;
				case ArcPlane.YZ:
					U = State.Position.Y;
					V = State.Position.Z;
					break;
				case ArcPlane.ZX:
					U = State.Position.Z;
					V = State.Position.X;
					break;
			}

			#region FindIJK
			{
				int ArcIncremental = (State.DistanceMode == ParseDistanceMode.Incremental) ? 1 : 0;

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'I')
						continue;

					switch(State.Plane)
					{
						case ArcPlane.XY:
							U = Words[i].Parameter + ArcIncremental * State.Position.X;
                            break;
						case ArcPlane.YZ:
							throw new ParseException("Current Plane is YZ, I word is invalid", lineNumber);
						case ArcPlane.ZX:
							V = Words[i].Parameter + ArcIncremental * State.Position.X;
							break;
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'J')
						continue;

					switch (State.Plane)
					{
						case ArcPlane.XY:
							V = Words[i].Parameter + ArcIncremental * State.Position.Y;
							break;
						case ArcPlane.YZ:
							U = Words[i].Parameter + ArcIncremental * State.Position.Y;
							break;
						case ArcPlane.ZX:
							throw new ParseException("Current Plane is ZX, J word is invalid", lineNumber);
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}

				for (int i = 0; i < Words.Count; i++)
				{
					if (Words[i].Command != 'K')
						continue;

					switch (State.Plane)
					{
						case ArcPlane.XY:
							throw new ParseException("Current Plane is XY, K word is invalid", lineNumber);
						case ArcPlane.YZ:
							V = Words[i].Parameter + ArcIncremental * State.Position.Z;
							break;
						case ArcPlane.ZX:
							U = Words[i].Parameter + ArcIncremental * State.Position.Z;
							break;
					}

					IJKused = true;
					Words.RemoveAt(i);
					break;
				}
			}
			#endregion

			#region ResolveRadius
			for (int i = 0; i < Words.Count; i++)
			{
				if (Words[i].Command != 'R')
					continue;

				if (IJKused)
					throw new ParseException("Both IJK and R notation used", lineNumber);

				if (State.Position == EndPos)
					throw new ParseException("arcs in R-notation must have non-coincident start and end points", lineNumber);

				double Radius = Words[i].Parameter;

				if (Radius == 0)
					throw new ParseException("Radius can't be zero", lineNumber);

				double A, B;

				switch (State.Plane)
				{
					default:
						A = EndPos.X;
						B = EndPos.Y;
						break;
					case ArcPlane.YZ:
						A = EndPos.Y;
						B = EndPos.Z;
						break;
					case ArcPlane.ZX:
						A = EndPos.Z;
						B = EndPos.X;
						break;
				}

				A -= U;		//(AB) = vector from start to end of arc along the axes of the current plane
				B -= V;

				double C = -B;	//(UV) = vector perpendicular to (AB)
				double D = A;

				{	//normalize perpendicular vector
					double perpLength = Math.Sqrt(C * C + D * D);
					C /= perpLength;
					D /= perpLength;
				}

				double PerpLength = Math.Sqrt((Radius * Radius) - ((A * A + B * B) / 4));

				if (MotionMode == 3 ^ Radius < 0)
					PerpLength = -PerpLength;

				U += (A / 2) + C * (PerpLength);
				V += (B / 2) + (D * PerpLength);

				Words.RemoveAt(i);
				break;
			}
			#endregion

			Arc arc = new Arc();
			arc.Start = State.Position;
			arc.End = EndPos;
			arc.Feed = State.Feed;
			arc.Direction = (MotionMode == 2) ? ArcDirection.CW : ArcDirection.CCW;
			arc.U = U;
			arc.V = V;
			arc.Plane = State.Plane;

			State.Commands.Add(arc);
			State.Position = EndPos;
			return;
		}
	}
}
