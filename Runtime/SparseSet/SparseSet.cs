﻿using System;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Massive
{
	[Il2CppSetOption(Option.NullChecks, false)]
	[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
	[Il2CppSetOption(Option.DivideByZeroChecks, false)]
	public class SparseSet : PackedSet
	{
		private const int EndHole = int.MaxValue;

		public int[] Sparse { get; private set; } = Array.Empty<int>();

		public int PackedCapacity { get; private set; }
		public int SparseCapacity { get; private set; }

		private int NextHole { get; set; } = EndHole;

		public SparseSet(Packing packing = Packing.Continuous)
		{
			Packing = packing;
		}

		/// <summary>
		/// Checks whether a packed array has no holes in it.
		/// </summary>
		public bool IsContinuous
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Packing == Packing.Continuous || NextHole == EndHole;
		}

		/// <summary>
		/// Checks whether a packed array has any holes in it.
		/// </summary>
		public bool HasHoles
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Packing != Packing.Continuous && NextHole != EndHole;
		}

		/// <summary>
		/// Shoots only after <see cref="Assign"/> call, when the id was not alive and therefore was created.
		/// </summary>
		public event Action<int> AfterAssigned;

		/// <summary>
		/// Shoots before each <see cref="Unassign"/> call, when the id was alive.
		/// </summary>
		public event Action<int> BeforeUnassigned;

		/// <summary>
		/// For serialization and rollbacks only.
		/// </summary>
		public State CurrentState
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new State(Count, NextHole, Packing);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				Count = value.Count;
				NextHole = value.NextHole;
				Packing = value.Packing;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Assign(int id)
		{
			// If ID is negative or already assigned, nothing to be done
			if (id < 0 || id < SparseCapacity && Sparse[id] != Constants.InvalidId)
			{
				return;
			}

			EnsureSparseAt(id);

			if (Packing == Packing.WithHoles && NextHole != EndHole)
			{
				var index = NextHole;
				NextHole = ~Packed[index];
				Pair(id, index);
			}
			else
			{
				EnsurePackedAt(Count);
				EnsureDataAt(Count);
				Pair(id, Count);
				Count += 1;
			}

			AfterAssigned?.Invoke(id);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Unassign(int id)
		{
			// If ID is negative or not assigned, nothing to be done
			if (id < 0 || id >= SparseCapacity || Sparse[id] == Constants.InvalidId)
			{
				return;
			}

			BeforeUnassigned?.Invoke(id);

			if (Packing == Packing.Continuous)
			{
				Count -= 1;
				MoveAt(Count, Sparse[id]);
			}
			else
			{
				var index = Sparse[id];
				Packed[index] = ~NextHole;
				NextHole = index;
			}

			Sparse[id] = Constants.InvalidId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Clear()
		{
			if (IsContinuous)
			{
				for (var i = Count - 1; i >= 0; i--)
				{
					var id = Packed[i];
					BeforeUnassigned?.Invoke(id);
					Sparse[id] = Constants.InvalidId;
				}
			}
			else
			{
				for (var i = Count - 1; i >= 0; i--)
				{
					var id = Packed[i];
					if (id >= 0)
					{
						BeforeUnassigned?.Invoke(id);
						Sparse[id] = Constants.InvalidId;
					}
				}
			}
			Count = 0;
			NextHole = EndHole;
		}

		/// <summary>
		/// For deserialization only.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClearWithoutNotify()
		{
			Array.Fill(Sparse, Constants.InvalidId);
			Count = 0;
			NextHole = EndHole;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int GetIndexOrNegative(int id)
		{
			if (id < 0 || id >= SparseCapacity)
			{
				return Constants.InvalidId;
			}

			return Sparse[id];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IsAssigned(int id)
		{
			return id >= 0 && id < SparseCapacity && Sparse[id] != Constants.InvalidId;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void EnsurePackedAt(int index)
		{
			if (index >= PackedCapacity)
			{
				ResizePacked(MathUtils.NextPowerOf2(index + 1));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void EnsureSparseAt(int index)
		{
			if (index >= SparseCapacity)
			{
				ResizeSparse(MathUtils.NextPowerOf2(index + 1));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResizePacked(int capacity)
		{
			Packed = Packed.Resize(capacity);
			PackedCapacity = capacity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResizeSparse(int capacity)
		{
			Sparse = Sparse.Resize(capacity);
			if (capacity > SparseCapacity)
			{
				Array.Fill(Sparse, Constants.InvalidId, SparseCapacity, capacity - SparseCapacity);
			}
			SparseCapacity = capacity;
		}

		/// <summary>
		/// Removes all holes from the packed array.
		/// </summary>
		public override void Compact()
		{
			if (HasHoles)
			{
				var count = Count;
				var nextHole = NextHole;

				for (; count > 0 && Packed[count - 1] < 0; count--) { }

				while (nextHole != EndHole)
				{
					var holeIndex = nextHole;
					nextHole = ~Packed[nextHole];
					if (holeIndex < count)
					{
						count -= 1;
						MoveAt(count, holeIndex);

						for (; count > 0 && Packed[count - 1] < 0; count--) { }
					}
				}

				Count = count;
				NextHole = EndHole;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PackedEnumerator GetEnumerator()
		{
			return new PackedEnumerator(this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SwapAt(int first, int second)
		{
			var firstId = Packed[first];
			var secondId = Packed[second];

			ThrowIfNegative(firstId, $"Can't swap negative id.");
			ThrowIfNegative(secondId, $"Can't swap negative id.");

			Pair(firstId, second);
			Pair(secondId, first);

			SwapDataAt(first, second);
		}

		public virtual void MoveDataAt(int source, int destination)
		{
		}

		public virtual void SwapDataAt(int first, int second)
		{
		}

		public virtual void CopyDataAt(int source, int destination)
		{
		}

		public virtual void EnsureDataAt(int index)
		{
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MoveAt(int source, int destination)
		{
			var sourceId = Packed[source];
			Pair(sourceId, destination);
			MoveDataAt(source, destination);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void Pair(int id, int index)
		{
			Sparse[id] = index;
			Packed[index] = id;
		}

		private void ThrowIfNegative(int id, string errorMessage)
		{
			if (id < 0)
			{
				throw new Exception(errorMessage);
			}
		}

		public readonly struct State
		{
			public readonly int Count;
			public readonly int NextHole;
			public readonly Packing Packing;

			public State(int count, int nextHole, Packing packing)
			{
				Count = count;
				NextHole = nextHole;
				Packing = packing;
			}
		}
	}
}
