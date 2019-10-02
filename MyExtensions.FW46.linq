<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationCore.dll</Reference>
  <Reference>&lt;RuntimeDirectory&gt;\WPF\PresentationFramework.dll</Reference>
  <NuGetReference>MathNet.Numerics</NuGetReference>
  <NuGetReference>OxyPlot.Core</NuGetReference>
  <NuGetReference>OxyPlot.Wpf</NuGetReference>
  <Namespace>MathNet.Numerics</Namespace>
  <Namespace>OxyPlot</Namespace>
  <Namespace>OxyPlot.Annotations</Namespace>
  <Namespace>OxyPlot.Axes</Namespace>
  <Namespace>OxyPlot.Reporting</Namespace>
  <Namespace>OxyPlot.Series</Namespace>
  <Namespace>OxyPlot.Wpf</Namespace>
</Query>

void Main()
{
	var graph1 = new Graphing(pointCount: 10001);
	var s1 = new ExpressionSeries(x => x) { Name = "y=x" };
	var mk = new Marker((3,3));
	s1.Markers.Add(mk);
	graph1.AddExpressionSeries(s1);
	graph1.AddExpressionSeries(new ExpressionSeries(x => -x + 10) { Name = "y=-x+10" });
	graph1.RenderGraph(chartTitle: "Test Chart", yLabel: "ylabel", xLabel: "xlabel");
}

public static class MyExtensions
{
	// Write custom extension methods here. They will be available to all queries.

}

public class DataSeries : ISeries
{
	private IList<(double, double)> _data;
	public Range Range { get; set; }
	public string Name { get; set; }
	public IList<Marker> Markers { get; set; }


	public DataSeries(IList<(double, double)> data)
	{
		var sortData = data.ToList();
		sortData.Sort((x,y)=>x.Item1.CompareTo(y.Item1));
		this._data = sortData;
		var depSet = this._data.Select(x => x.Item1);
		this.Range = new Range() { Min = depSet.Min(), Max = depSet.Max() };
		this.Markers = new List<Marker>();
	}
	
	public (double, double) GetMax()
	{
		return this._data.Aggregate ((acc, curr) => curr.Item2 > acc.Item2 ? curr : acc);
	}

	public IList<DataPoint> GetPoints()
	{
		return this._data.Where(s => this.Range.InRangeBothEndsInclusive(s.Item1)).Select(s => new DataPoint(s.Item1, s.Item2)).ToList();
	}
}

public class ExpressionSeries : ISeries
{
	public Func<double, double> Expression
	{
		get {return this.exp;}
		set {throw new Exception("Cannot set");}
	}
	private Func<double, double> exp;
	public Range Range { get; set; }
	public string Name { get; set; }
	public IList<Marker> Markers { get; set; }
	
	public ExpressionSeries(Func<double, double> f)
	{
		this.exp = f;
		this.Markers = new List<Marker>();
	}

	public IList<DataPoint> GetPoints()
	{
		if (this.Range == null)
		{
			throw new Exception("Expression series range not set");
		}
		else
		{
			return Generate.LinearSpaced(10000, this.Range.Min, this.Range.Max).Select(g => new DataPoint(g, this.exp(g))).ToList();
		}
	}
}

public class LinearEquation
{
	public double k { get; set; }
	public double b { get; set; }
	public Range Range { get; set; }

	public LinearEquation(double grad, double intercept)
	{
		k = grad;
		b = intercept;
	}

	public LinearEquation((double, double) p1, (double, double) p2)
	{
		this.k = (p2.Item2 - p1.Item2) / (p2.Item1 - p1.Item1);
		this.b = p1.Item2 - this.k * p1.Item1;
	}

	public double GetDep(double indep)
	{
		return k * indep + b;
	}

	public double GetIndep(double dep)
	{
		return (dep - b) / k;
	}

	public double XIntersect()
	{
		return this.GetIndep(0);
	}

	public double YIntersect()
	{
		return this.GetDep(0);
	}

	public IList<(double, double)> GetDepSet(IEnumerable<double> indep)
	{
		return indep.Select(i => (i, this.GetDep(i))).ToList();
	}
}

public class Range
{
	public double Min { get; set; }
	public double Max { get; set; }

	public bool InRangeDownwardsInclusive(double val)
	{
		return val >= Min && val < Max;
	}
	public bool InRangeUpwardsInclusive(double val)
	{
		return val >= Min && val < Max;
	}
	public bool InRangeBothEndsInclusive(double val)
	{
		return val >= Min && val <= Max;
	}
	public bool InRangeBothEndsExclusive(double val)
	{
		return val > Min && val < Max;
	}

}

public class Marker
{
	public Marker((double, double) coordinate, string template = "X: {0}, Y: {1}")
	{
		this.Text = string.Format(template, coordinate.Item1, coordinate.Item2);
		this.Coordinate = coordinate;
	}
	public (double, double) Coordinate { get; set; }
	public string Text { get; set; }
}

public interface ISeries
{
	string Name { get; set; }
	Range Range { get; set; }
	IList<Marker> Markers {get; set;}
	IList<DataPoint> GetPoints();
}

public class Graphing
{
	public IList<ISeries> Plots { get; set; } = new List<ISeries>();
	public IList<ExpressionSeries> FunctionPlots { get; set; } = new List<ExpressionSeries>();
	public IList<DataSeries> DataPlots { get; set; } = new List<DataSeries>();
	
	private double pointCount;
	
	public Graphing(int pointCount = 10001) 
	{
		this.pointCount = pointCount;
	}

	public void AddSeries(ISeries series)
	{
		this.Plots.Add(series);
	}
	
	public void AddDataSeries(DataSeries series)
	{
		this.DataPlots.Add(series);
	}
	
	public void AddExpressionSeries(ExpressionSeries series)
	{
		this.FunctionPlots.Add(series);
	}

	public void RenderGraph(string xLabel = null, string yLabel = null, string chartTitle = null)
	{
		var nonNullRanges = this.Plots.Where(p => p.Range != null);
		var r = nonNullRanges.Count() == 0 ? new Range() { Min = 0, Max = 10 }
			: new Range()
			{
				Min = nonNullRanges.Select(nr => nr.Range.Min).Min(),
				Max = nonNullRanges.Select(nr => nr.Range.Max).Max()
			};

		var pm = new PlotModel();

		if (FunctionPlots.Count() == 0 && DataPlots.Count() == 0)
		{
			foreach (var plot in this.Plots)
			{
				plot.Range = r;
				var s = new OxyPlot.Series.LineSeries();
				if (plot.Name != null)
				{
					s.Title = plot.Name;
				}
				s.Points.AddRange(plot.GetPoints().Select(p => new DataPoint(p.X, p.Y)));
				pm.Series.Add(s);
			}
		}
		else
		{
			foreach (var functionPlot in FunctionPlots)
			{
				var fn = functionPlot.Expression;
				var s = new FunctionSeries(fn, r.Min, r.Max, ((r.Max-r.Min)/(pointCount-1)));
				if (functionPlot.Name != null)
				{
					s.Title = functionPlot.Name;
				}
				pm.Series.Add(s);
				if (functionPlot.Markers.Count() > 0)
				{
					foreach (var marker in functionPlot.Markers)
					{
						pm.Annotations.Add(new OxyPlot.Annotations.PointAnnotation()
							{
								X = marker.Coordinate.Item1,
								Y = marker.Coordinate.Item2,
								Text = marker.Text
							});
					}
				}
			}
			foreach (var dataPlot in DataPlots)
			{
				var s = new OxyPlot.Series.LineSeries();
				if (dataPlot.Name != null)
				{
					s.Title = dataPlot.Name;
				}
				s.Points.AddRange(dataPlot.GetPoints().Select(p => new DataPoint(p.X, p.Y)));
				pm.Series.Add(s);
				if (dataPlot.Markers.Count() > 0)
				{
					foreach (var marker in dataPlot.Markers)
					{
						pm.Annotations.Add(new OxyPlot.Annotations.PointAnnotation()
						{
							X = marker.Coordinate.Item1,
							Y = marker.Coordinate.Item2,
							Text = marker.Text
						});
					}
				}

			}
		}

		if (xLabel != null)
		{
			pm.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = AxisPosition.Bottom, Title = xLabel });
		}
		if (yLabel != null)
		{
			pm.Axes.Add(new OxyPlot.Axes.LinearAxis { Position = AxisPosition.Left, Title = yLabel });
		}
		if (chartTitle != null)
		{
			pm.Title = chartTitle;
		}

		var view = new PlotView();
		view.Model = pm;
		PanelManager.DisplayWpfElement(view);
	}
}

public static class PhysicalConstantsFull
{
	static public double SpeedOfLight = 299792458; 
	static public double GravitationalConstant = 6.67408e-11;
	static public double PlanckConstant = 6.62607015e-34;
	static public double PlanckConstantInEv = 4.13566770e-15;
	static public double DiracConstant = 1.05457182e-34;
	static public double DiracCOnstantInEv = 6.58211957e-16;
	static public double ElementaryCharge = 1.602176634e-19;
	static public double VacuumPermittivity = 8.8541878128e-12; 
	static public double FreeSpacePermeability = Math.PI * 4e-7;
	static public double AvogadroConstant = 6.02214076e23;
	static public double BoltzmannConstant = 1.380649e-23;
	static public double BoltzmannConstantInEv = 8.617333262145e-5;
	static public double StefanBoltzmannConstant = 5.670374419e-8;
	static public double GasConstant = 8.314462618;
	static public double AtomicMassConstant = 1.660e-27; 
	static public double ElectronMass = 9.10938356e-31;
	static public double ProtonMass = 1.6726219e-27; 
	static public double NeutronMass = 1.674927471e-27;
	static public double EarthGravitationalAcceleration = 9.80665;
	static public double StandardAtmosphere = 101325;
}

public static class PhysicalConstantsCommon
{
	static public double c = PhysicalConstantsFull.SpeedOfLight;
	static public double G = PhysicalConstantsFull.GravitationalConstant;
	static public double g = PhysicalConstantsFull.EarthGravitationalAcceleration;
	static public double h = PhysicalConstantsFull.PlanckConstant;
	static public double hBar = PhysicalConstantsFull.DiracConstant;
	static public double e = PhysicalConstantsFull.ElementaryCharge;
	static public double R = PhysicalConstantsFull.GasConstant;
	static public double kB = PhysicalConstantsFull.BoltzmannConstant;
	static public double atm = PhysicalConstantsFull.StandardAtmosphere;
	static public double NA = PhysicalConstantsFull.AvogadroConstant;
	static public double epsilonNaught = PhysicalConstantsFull.VacuumPermittivity;
	static public double muNaught = PhysicalConstantsFull.FreeSpacePermeability;
}

// You can also define non-static classes, enums, etc.