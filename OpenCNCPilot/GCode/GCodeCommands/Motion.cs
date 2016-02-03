using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	class Motion : Command
	{
		public Vector3 Start;
		public Vector3 End;
		public double Feed;
	}
}
