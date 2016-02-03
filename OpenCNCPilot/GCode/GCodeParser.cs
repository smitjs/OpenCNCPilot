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

		public GCodeParser()
		{
			State = new ParserState();

			State.Position = new Vector3();
			State.Plane = ArcPlane.XY;
			State.Feed = 100;
			State.DistanceMode = ParseDistanceMode.Absolute;
			State.ArcDistanceMode = ParseDistanceMode.Incremental;
			State.Unit = ParseUnit.MM;

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

			while (Words.Count > 0)
			{
				if (Words.First().Command == 'M')
				{
					int param = (int)Words.First().Parameter;

					if (param != Words.First().Parameter)
						throw new ParseException("MCode can only have integer parameters", lineNumber);

					State.Commands.Add(new MCode() { Code = param });

					Words.RemoveAt(0);
					continue;
				}

				if (Words.First().Command == 'G')
				{
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
					if(param == 0 || param == 1 || param == 2 || param == 3)
					{
						Words.RemoveAt(0);

						for(int i = 0; i < Words.Count; i++)
						{
							if (Words[i].Command != 'F')
								continue;
							State.Feed = Words[i].Parameter;
							Words.RemoveAt(i);
						}

						Vector3 EndPos = State.Position;

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

						if(param  <= 1)
						{
							Line l = new Line();
							l.Start = State.Position;
							l.End = EndPos;
							l.Rapid = param == 0;
							l.Feed = State.Feed;

							State.Commands.Add(l);
						}
						else
						{
							Vector3 ArcCenter = State.Position;

							int ArcIncremental = (State.DistanceMode == ParseDistanceMode.Incremental) ? 1 : 0;

							if (State.Plane != ArcPlane.YZ)
							{
								for (int i = 0; i < Words.Count; i++)
								{
									if (Words[i].Command != 'I')
										continue;
									EndPos.X = Words[i].Parameter + ArcIncremental * ArcCenter.X;
									Words.RemoveAt(i);
									break;
								}
							}

							if (State.Plane != ArcPlane.ZX)
							{
								for (int i = 0; i < Words.Count; i++)
								{
									if (Words[i].Command != 'J')
										continue;
									EndPos.Y = Words[i].Parameter + ArcIncremental * ArcCenter.Y;
									Words.RemoveAt(i);
									break;
								}
							}

							if (State.Plane != ArcPlane.XY)
							{
								for (int i = 0; i < Words.Count; i++)
								{
									if (Words[i].Command != 'K')
										continue;
									EndPos.Z = Words[i].Parameter + ArcIncremental * ArcCenter.Z;
									Words.RemoveAt(i);
									break;
								}
							}

							for (int i = 0; i < Words.Count; i++)
							{
								if (Words[i].Command != 'R')
									continue;

								if (ArcCenter == State.Position)
									throw new ParseException("both IJK and R centers defined", lineNumber);

								double radius = Words[i].Parameter;







								Words.RemoveAt(i);
								break;
							}
						}

						if (Words.Count > 0)
							throw new ParseException("unused words in block", lineNumber);
					}

				}

				if (Words.First().Command == 'F')
				{
					State.Feed = Words.First().Parameter;
					Words.RemoveAt(0);
					continue;
				}

				Words.RemoveAt(0);
			}
		}
	}
}
