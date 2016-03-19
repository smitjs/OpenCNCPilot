using HelixToolkit.Wpf;
using OpenCNCPilot.Util;
using System.Xml;
using System.Windows.Media.Media3D;
using System.Windows;
using System;

namespace OpenCNCPilot.GCode
{
	public class HeightMap
	{
		public double?[,] Points;
		public int SizeX;
		public int SizeY;

		public Vector2 Min;
		public Vector2 Max;

		public Vector2 Delta
		{
			get { return Max - Min; }
		}

		public double MinHeight = 0;
		public double MaxHeight = 0;

		public event Action MapUpdated;


		public HeightMap(int sizeX, int sizeY, Vector2 min, Vector2 max)
		{
			Points = new double?[sizeX, sizeY];
			Min = min;
			Max = max;

			if (Delta.X < 0)
				Max.X = Min.X;
			if (Delta.Y < 0)
				Max.Y = Min.Y;

			SizeX = sizeX;
			SizeY = sizeY;
		}

		private HeightMap()
		{

		}

		public void AddPoint(int x, int y, double height)
		{
			Points[x, y] = height;

			if (height > MaxHeight)
				MaxHeight = height;
			if (height < MinHeight)
				MinHeight = height;

			if (MapUpdated != null)
				MapUpdated();
		}

		public static HeightMap Load(string path)
		{
			HeightMap map = new HeightMap();

			XmlReader r = XmlReader.Create(path);

			while (r.Read())
			{
				if (!r.IsStartElement())
					continue;

				switch (r.Name)
				{
					case "heightmap":
						map.Min = new Vector2(double.Parse(r["MinX"], Constants.NFI), double.Parse(r["MinY"], Constants.NFI));
						map.Max = new Vector2(double.Parse(r["MaxX"], Constants.NFI), double.Parse(r["MaxY"], Constants.NFI));
						map.SizeX = int.Parse(r["SizeX"]);
						map.SizeY = int.Parse(r["SizeY"]);
						map.Points = new double?[map.SizeX, map.SizeY];
						break;
					case "point":
						int x = int.Parse(r["X"]), y = int.Parse(r["Y"]);
						double height = double.Parse(r.ReadInnerXml(), Constants.NFI);

						map.Points[x, y] = height;

						if (height > map.MaxHeight)
							map.MaxHeight = height;
						if (height < map.MinHeight)
							map.MinHeight = height;

						break;
				}
			}

			r.Dispose();
			return map;
		}

		public void Save(string path)
		{
			XmlWriterSettings set = new XmlWriterSettings();
			set.Indent = true;
			XmlWriter w = XmlWriter.Create(path, set);
			w.WriteStartDocument();
			w.WriteStartElement("heightmap");
			w.WriteAttributeString("MinX", Min.X.ToString(Constants.NFI));
			w.WriteAttributeString("MinY", Min.Y.ToString(Constants.NFI));
			w.WriteAttributeString("MaxX", Max.X.ToString(Constants.NFI));
			w.WriteAttributeString("MaxY", Max.Y.ToString(Constants.NFI));
			w.WriteAttributeString("SizeX", SizeX.ToString(Constants.NFI));
			w.WriteAttributeString("SizeY", SizeY.ToString(Constants.NFI));

			for (int x = 0; x < SizeX; x++)
			{
				for (int y = 0; y < SizeY; y++)
				{
					if (!Points[x, y].HasValue)
						continue;

					w.WriteStartElement("point");
					w.WriteAttributeString("X", x.ToString());
					w.WriteAttributeString("Y", y.ToString());
					w.WriteString(Points[x, y].Value.ToString(Constants.NFI));
					w.WriteEndElement();
				}
			}
			w.WriteEndElement();
			w.Close();
		}

		public void GetModel(MeshGeometryVisual3D mesh, LinesVisual3D boundary)
		{
			MeshBuilder mb = new MeshBuilder(false, true);

			double Hdelta = MaxHeight - MinHeight;

			for (int x = 0; x < SizeX - 1; x++)
			{
				for (int y = 0; y < SizeY - 1; y++)
				{
					if (!Points[x, y].HasValue || !Points[x, y + 1].HasValue || !Points[x + 1, y].HasValue || !Points[x + 1, y + 1].HasValue)
						continue;

					mb.AddQuad(
						new Point3D(Min.X + (x + 1) * Delta.X / (SizeX - 1), Min.Y + (y) * Delta.Y / (SizeY - 1), Points[x + 1, y].Value),
						new Point3D(Min.X + (x + 1) * Delta.X / (SizeX - 1), Min.Y + (y + 1) * Delta.Y / (SizeY - 1), Points[x + 1, y + 1].Value),
						new Point3D(Min.X + (x) * Delta.X / (SizeX - 1), Min.Y + (y + 1) * Delta.Y / (SizeY - 1), Points[x, y + 1].Value),
						new Point3D(Min.X + (x) * Delta.X / (SizeX - 1), Min.Y + (y) * Delta.Y / (SizeY - 1), Points[x, y].Value),
						new Point(0, (Points[x + 1, y].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x + 1, y + 1].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x, y + 1].Value - MinHeight) * Hdelta),
						new Point(0, (Points[x, y].Value - MinHeight) * Hdelta)
						);
				}
			}

			mesh.MeshGeometry = mb.ToMesh();

			Point3DCollection b = new Point3DCollection();
			b.Add(new Point3D(Min.X, Min.Y, 0));
			b.Add(new Point3D(Min.X, Max.Y, 0));
			b.Add(new Point3D(Min.X, Max.Y, 0));
			b.Add(new Point3D(Max.X, Max.Y, 0));
			b.Add(new Point3D(Max.X, Max.Y, 0));
			b.Add(new Point3D(Max.X, Min.Y, 0));
			b.Add(new Point3D(Max.X, Min.Y, 0));
			b.Add(new Point3D(Min.X, Min.Y, 0));

			boundary.Points = b;
		}
	}
}
