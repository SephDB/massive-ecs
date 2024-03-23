﻿using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Massive.PerformanceTests
{
	[TestFixture(1000)]
	public class ViewPerformanceTest
	{
		private const int MeasurementCount = 100;
		private const int IterationsPerMeasurement = 120;

		private readonly IRegistry _registry;

		public ViewPerformanceTest(int entitiesCount)
		{
			_registry = new Registry(entitiesCount).FillRegistryWith50Components();
		}

		[Test, Performance]
		public void View_Performance()
		{
			View view = new View(_registry);

			Measure.Method(() => view.ForEach((_) => { }))
				.MeasurementCount(MeasurementCount)
				.IterationsPerMeasurement(IterationsPerMeasurement)
				.Run();
		}

		[Test, Performance]
		public void ViewT_Performance()
		{
			View<TestState64> view = new View<TestState64>(_registry);

			Measure.Method(() => view.ForEach((int _, ref TestState64 _) => { }))
				.MeasurementCount(MeasurementCount)
				.IterationsPerMeasurement(IterationsPerMeasurement)
				.Run();
		}

		[Test, Performance]
		public void ViewTT_Performance()
		{
			View<TestState64, TestState64_2> view = new View<TestState64, TestState64_2>(_registry);

			Measure.Method(() => view.ForEach((int _, ref TestState64 _, ref TestState64_2 _) => { }))
				.MeasurementCount(MeasurementCount)
				.IterationsPerMeasurement(IterationsPerMeasurement)
				.Run();
		}

		[Test, Performance]
		public void ViewTTT_Performance()
		{
			View<TestState64, TestState64_2, TestState64_3> view = new View<TestState64, TestState64_2, TestState64_3>(_registry);

			Measure.Method(() => view.ForEach((int _, ref TestState64 _, ref TestState64_2 _, ref TestState64_3 _) => { }))
				.MeasurementCount(MeasurementCount)
				.IterationsPerMeasurement(IterationsPerMeasurement)
				.Run();
		}
	}
}