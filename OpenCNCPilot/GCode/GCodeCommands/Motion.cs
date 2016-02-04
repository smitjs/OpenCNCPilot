using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	abstract class Motion : Command
	{
		public Vector3 Start;
		public Vector3 End;
		public double Feed;

		public Vector3 Delta
		{
			get
			{
				return End - Start;
			}
		}

		public abstract double Length { get; }
		public abstract Vector3 Interpolate(double ratio);
		public abstract IEnumerable<Motion> Split(double length);
	}
}
