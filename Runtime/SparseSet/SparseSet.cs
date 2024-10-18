﻿using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Massive
{
	public enum PackingMode
	{
		Continuous,
		WithHoles,
	}

	[Il2CppSetOption(Option.NullChecks | Option.ArrayBoundsChecks, false)]
	public class SparseSet : ISet
	{
		private const int MaxCount = int.MaxValue;

		private int[] _packed;
		private int[] _sparse;

		public PackingMode PackingMode { get; }
		public int Count { get; set; }
		public int NextHole { get; set; }

		public SparseSet(int setCapacity = Constants.DefaultCapacity, PackingMode packingMode = PackingMode.Continuous)
		{
			PackingMode = packingMode;
			_packed = new int[setCapacity];
			_sparse = new int[setCapacity];

			NextHole = MaxCount;
			Array.Fill(_sparse, Constants.InvalidId);
		}

		/// <summary>
		/// Checks whether a sparse set is fully packed.
		/// </summary>
		public bool IsContinuous
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => PackingMode == PackingMode.Continuous || NextHole == MaxCount;
		}

		/// <summary>
		/// Checks whether a packed array has any holes in it.
		/// </summary>
		public bool HasHoles
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => !IsContinuous;
		}

		public int[] Packed => _packed;

		public int[] Sparse => _sparse;

		public int[] Ids
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Packed;
		}

		public int PackedCapacity => Packed.Length;

		public int SparseCapacity => Sparse.Length;

		public event Action<int> AfterAssigned;

		public event Action<int> BeforeUnassigned;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Assign(int id)
		{
			// If ID is negative or element is alive, nothing to be done
			if (id < 0 || id < SparseCapacity && Sparse[id] != Constants.InvalidId)
			{
				return;
			}

			EnsureSparseForIndex(id);

			if (HasHoles)
			{
				int packed = NextHole;
				NextHole = ~Packed[packed];
				AssignIndex(id, packed);
			}
			else
			{
				EnsurePackedForIndex(Count);
				EnsureDataForIndex(Count);
				AssignIndex(id, Count);
				Count += 1;
			}

			AfterAssigned?.Invoke(id);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Unassign(int id)
		{
			// If ID is negative or element is not alive, nothing to be done
			if (id < 0 || id >= SparseCapacity || Sparse[id] == Constants.InvalidId)
			{
				return;
			}

			BeforeUnassigned?.Invoke(id);

			if (PackingMode == PackingMode.Continuous)
			{
				Count -= 1;
				CopyFromToPacked(Count, Sparse[id]);
			}
			else
			{
				int packed = Sparse[id];
				Packed[packed] = ~NextHole;
				NextHole = packed;
			}

			Sparse[id] = Constants.InvalidId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			if (IsContinuous)
			{
				for (int i = Count - 1; i >= 0; i--)
				{
					int id = Packed[i];
					BeforeUnassigned?.Invoke(id);
					Sparse[id] = Constants.InvalidId;
				}
			}
			else
			{
				for (int i = Count - 1; i >= 0; i--)
				{
					int id = Packed[i];
					if (id >= 0)
					{
						BeforeUnassigned?.Invoke(id);
						Sparse[id] = Constants.InvalidId;
					}
				}
			}
			Count = 0;
			NextHole = MaxCount;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetIndex(int id)
		{
			return Sparse[id];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetIndexOrInvalid(int id)
		{
			if (id < 0 || id >= SparseCapacity)
			{
				return Constants.InvalidId;
			}

			return Sparse[id];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetIndex(int id, out int index)
		{
			if (id < 0 || id >= SparseCapacity)
			{
				index = Constants.InvalidId;
				return false;
			}

			index = Sparse[id];

			return index != Constants.InvalidId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsAssigned(int id)
		{
			return id >= 0 && id < SparseCapacity && Sparse[id] != Constants.InvalidId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void SwapPacked(int packedA, int packedB)
		{
			int idA = Packed[packedA];
			int idB = Packed[packedB];
			AssignIndex(idA, packedB);
			AssignIndex(idB, packedA);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResizePacked(int capacity)
		{
			Array.Resize(ref _packed, capacity);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResizeSparse(int capacity)
		{
			int previousCapacity = SparseCapacity;
			Array.Resize(ref _sparse, capacity);
			if (capacity > previousCapacity)
			{
				Array.Fill(_sparse, Constants.InvalidId, previousCapacity, capacity - previousCapacity);
			}
		}

		/// <summary>
		/// Removes all holes from packed array.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Compact()
		{
			if (HasHoles)
			{
				for (; Count > 0 && Packed[Count - 1] < 0; Count--) {}

				while (NextHole != MaxCount)
				{
					int packedHole = NextHole;
					NextHole = ~Packed[NextHole];
					if (packedHole < Count)
					{
						Count -= 1;
						CopyFromToPacked(Count, packedHole);

						for (; Count > 0 && Packed[Count - 1] < 0; Count--) {}
					}
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual void CopyFromToPacked(int source, int destination)
		{
			int sourceId = Packed[source];
			AssignIndex(sourceId, destination);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected virtual void EnsureDataForIndex(int index)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AssignIndex(int id, int packed)
		{
			Sparse[id] = packed;
			Packed[packed] = id;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsurePackedForIndex(int index)
		{
			if (index >= PackedCapacity)
			{
				ResizePacked(MathHelpers.NextPowerOf2(index + 1));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void EnsureSparseForIndex(int index)
		{
			if (index >= SparseCapacity)
			{
				ResizeSparse(MathHelpers.NextPowerOf2(index + 1));
			}
		}
	}
}
