using Unity.IL2CPP.CompilerServices;

namespace Massive
{
	/// <summary>
	/// Rollback extension for <see cref="Massive.DataSet{T}"/> with managed data support.
	/// </summary>
	[Il2CppSetOption(Option.NullChecks, false)]
	[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
	[Il2CppSetOption(Option.DivideByZeroChecks, false)]
	public class MassiveManagedDataSet<T> : MassiveDataSet<T> where T : IManaged<T>
	{
		public MassiveManagedDataSet(int framesCapacity = Constants.DefaultFramesCapacity,
			int pageSize = Constants.DefaultPageSize, Packing packing = Packing.Continuous)
			: base(framesCapacity, pageSize, packing)
		{
		}

		protected override void CopyData(PagedArray<T> source, PagedArray<T> destination, int count)
		{
			foreach (var (pageIndex, pageLength, _) in new PageSequence(source.PageSize, count))
			{
				if (!source.HasPage(pageIndex))
				{
					continue;
				}

				destination.EnsurePage(pageIndex);

				var sourcePage = source.Pages[pageIndex];
				var destinationPage = destination.Pages[pageIndex];

				for (var i = 0; i < pageLength; i++)
				{
					sourcePage[i].CopyTo(ref destinationPage[i]);
				}
			}
		}
	}
}
