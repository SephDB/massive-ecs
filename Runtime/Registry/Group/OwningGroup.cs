﻿using System;
using System.Linq;

namespace Massive
{
	public class OwningGroup : IGroup
	{
		private IFilter Filter { get; }
		private ISet[] Owned { get; }
		private ISet[] Other { get; }
		private ISet[] All { get; }

		protected bool IsSynced { get; set; }

		public int Length { get; private set; }

		public OwningGroup(ISet[] owned, ISet[] other = null, IFilter filter = null)
		{
			Owned = owned;
			Other = other ?? Array.Empty<ISet>();
			Filter = filter ?? EmptyFilter.Instance;

			All = Owned.Concat(Other).ToArray();

			foreach (var set in All)
			{
				set.AfterAdded += OnAfterAdded;
				set.BeforeDeleted += OnBeforeDeleted;
			}
		}

		public void EnsureSynced()
		{
			if (IsSynced)
			{
				return;
			}

			SortOwned();
			IsSynced = true;
		}

		private void SortOwned()
		{
			Length = 0;
			var minimal = SetUtils.GetMinimalSet(All).AliveIds;
			foreach (var id in minimal)
			{
				OnAfterAdded(id);
			}
		}

		private void OnAfterAdded(int id)
		{
			if (IsSynced && SetUtils.AliveInAll(id, All) && Filter.Contains(id))
			{
				SwapEntry(id, Length);
				Length += 1;
			}
		}

		private void OnBeforeDeleted(int id)
		{
			if (IsSynced && Owned[0].TryGetDense(id, out var dense) && dense < Length)
			{
				Length -= 1;
				SwapEntry(id, Length);
			}
		}

		private void SwapEntry(int entryId, int swapDense)
		{
			foreach (var set in Owned)
			{
				set.SwapDense(set.GetDense(entryId), swapDense);
			}
		}
	}
}