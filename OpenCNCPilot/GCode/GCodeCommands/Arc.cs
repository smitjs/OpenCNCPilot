using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	public enum ArcPlane
	{
		XY,
		ZX,
		YZ
	}

	public enum ArcDirection
	{
		CW,
		CCW
	}

	class Arc : Motion
	{
		public ArcPlane Plane;
		public ArcDirection Direction;
		public double U;	//absolute position of center in first axis of plane
		public double V;	//absolute position of center in second axis of plane
	}
}
