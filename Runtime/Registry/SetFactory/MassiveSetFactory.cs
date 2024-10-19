﻿namespace Massive
{
	/// <summary>
	/// Factory for data structures with rollbacks.
	/// </summary>
	/// <remarks>
	/// Created massives have first frame saved so that you can rollback to it.
	/// </remarks>
	public class MassiveSetFactory : ISetFactory
	{
		private readonly int _setCapacity;
		private readonly int _framesCapacity;
		private readonly bool _storeEmptyTypesAsDataSets;
		private readonly int _pageSize;
		private readonly PackingMode _defaultPackingMode;

		public MassiveSetFactory(int setCapacity = Constants.DefaultCapacity, int framesCapacity = Constants.DefaultFramesCapacity,
			bool storeEmptyTypesAsDataSets = false, int pageSize = Constants.DefaultPageSize, PackingMode defaultPackingMode = PackingMode.Continuous)
		{
			_setCapacity = setCapacity;
			_framesCapacity = framesCapacity;
			_storeEmptyTypesAsDataSets = storeEmptyTypesAsDataSets;
			_pageSize = pageSize;
			_defaultPackingMode = defaultPackingMode;
		}

		public SparseSet CreateAppropriateSet<T>()
		{
			if (TypeInfo<T>.HasNoFields && !_storeEmptyTypesAsDataSets)
			{
				return CreateSparseSet<T>();
			}

			return CreateDataSet<T>();
		}

		private SparseSet CreateSparseSet<T>()
		{
			var massiveSparseSet = new MassiveSparseSet(_setCapacity, _framesCapacity,
				IStable.IsImplementedFor<T>() ? PackingMode.WithHoles : _defaultPackingMode);
			massiveSparseSet.SaveFrame();
			return massiveSparseSet;
		}

		private SparseSet CreateDataSet<T>()
		{
			var packingMode = IStable.IsImplementedFor<T>() ? PackingMode.WithHoles : _defaultPackingMode;
			var massiveDataSet = ManagedUtils.IsManaged<T>()
				? ManagedUtils.CreateMassiveManagedDataSet<T>(_setCapacity, _framesCapacity, _pageSize, packingMode)
				: new MassiveDataSet<T>(_setCapacity, _framesCapacity, _pageSize, packingMode);
			((IMassive)massiveDataSet).SaveFrame();
			return massiveDataSet;
		}
	}
}
