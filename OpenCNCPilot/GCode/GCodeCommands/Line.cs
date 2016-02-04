using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	class Line : Motion
	{
		public bool Rapid;

		public override double Length
		{
			get
			{
				return Delta.Magnitude;
			}
		}

		public override Vector3 Interpolate(double ratio)
		{
			return Start + Delta * ratio;
		}

		public override IEnumerable<Motion> Split(double length)
		{
			throw new NotImplementedException();
		}
	}
}
