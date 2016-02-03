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

			foreach(Match match in matches)
			{
				Words.Add(new Word() { Command = match.Groups[1].Value[0], Parameter = int.Parse(match.Groups[2].Value) });
			}
			
		}
	}
}
