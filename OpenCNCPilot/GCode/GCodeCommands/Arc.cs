using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	public enum ArcPlane
	{
		XY = 0,
		ZX = 1,
		YZ = 2
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
		public double V;    //absolute position of center in second axis of plane

		public override double Length
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public double StartAngle
		{
			get
			{
				Vector3 StartInPlane = Start.RollComponents(-(int)Plane);
				double X = StartInPlane.X - U;
				double Y = StartInPlane.Y - V;
				return Math.Atan2(Y, X);
			}
		}

		public double EndAngle
		{
			get
			{
				Vector3 EndInPlane = End.RollComponents(-(int)Plane);
				double X = EndInPlane.X - U;
				double Y = EndInPlane.Y - V;
				return Math.Atan2(Y, X);
			}
		}

		public double AngleSpan
		{
			get
			{
				double span = EndAngle - StartAngle;

				if (Direction == ArcDirection.CW)
				{
					if (span >= 0)
						span -= 2 * Math.PI;
				}
				else
				{
					if (span <= 0)
						span += 2 * Math.PI;
				}

				return span;
			}
		}

		public double Radius
		{
			get // get average between both radii
			{
				Vector3 startplane = Start.RollComponents(-(int)Plane);
				Vector3 endplane = End.RollComponents(-(int)Plane);

				return (
					Math.Sqrt(Math.Pow(startplane.X - U, 2) + Math.Pow(startplane.Y - V, 2)) +
					Math.Sqrt(Math.Pow(endplane.X - U, 2) + Math.Pow(endplane.Y - V, 2))
					) / 2;
			}
		}

		public override Vector3 Interpolate(double ratio)
		{
			double angle = StartAngle + AngleSpan * ratio * ((Direction == ArcDirection.CCW) ? 1 : -1);

			Vector3 onPlane = new Vector3(U + (Radius * Math.Cos(angle)), V + (Radius * Math.Sin(angle)), 0);

			double helix = (Start + (ratio * Delta)).RollComponents(-(int)Plane).Z;

			onPlane.Z = helix;

			return onPlane.RollComponents((int)Plane);
		}

		public override IEnumerable<Motion> Split(double length)
		{
			throw new NotImplementedException();
		}
	}
}
