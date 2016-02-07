using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot
{
	class HeightMap
	{
		double[,] Points;

		public HeightMap(int sizeX, int sizeY, double offsetX, double offsetY)
		{
			Points = new double[sizeX, sizeY];
		}
	}
}
