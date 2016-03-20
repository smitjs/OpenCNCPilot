using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.Util
{
	public class Constants
	{
		public static NumberFormatInfo NFI = new NumberFormatInfo() { NumberDecimalSeparator = "." };

		public static string FileFilterGCode = "GCode|*.tap;*.nc;*.ngc|All Files|*.*";
		public static string FileFilterHeightMap = "Height Maps|*.hmap|All Files|*.*";

		static Constants()
		{
			
		}
	}
}
