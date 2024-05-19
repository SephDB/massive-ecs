﻿using NUnit.Framework;

namespace Massive.Tests
{
	[TestFixture]
	public class MassiveSparseSetTests
	{
		[Test]
		public void Ensure_ShouldMakeAlive()
		{
			var massive = new MassiveSparseSet(setCapacity: 4, framesCapacity: 2);

			var id = 2;

			Assert.IsFalse(massive.IsAssigned(id));

			massive.Assign(id);

			Assert.IsTrue(massive.IsAssigned(id));
		}

		[Test]
		public void Delete_ShouldMakeNotAlive()
		{
			var massive = new MassiveSparseSet(setCapacity: 4, framesCapacity: 2);

			massive.Assign(0);
			massive.Assign(1);
			massive.Assign(2);

			massive.Unassign(1);

			Assert.IsTrue(massive.IsAssigned(0));
			Assert.IsFalse(massive.IsAssigned(1));
			Assert.IsTrue(massive.IsAssigned(2));
		}

		[Test]
		public void Ensure_ShouldMakeStatesAlive()
		{
			var massive = new MassiveSparseSet(setCapacity: 4, framesCapacity: 2);

			Assert.IsFalse(massive.IsAssigned(0));
			Assert.IsFalse(massive.IsAssigned(1));
			Assert.IsFalse(massive.IsAssigned(2));

			massive.Assign(0);
			massive.Assign(1);
			massive.Assign(2);

			Assert.IsTrue(massive.IsAssigned(0));
			Assert.IsTrue(massive.IsAssigned(1));
			Assert.IsTrue(massive.IsAssigned(2));
		}

		[Test]
		public void SaveFrame_ShouldPreserveLiveliness()
		{
			var massive = new MassiveSparseSet(setCapacity: 4, framesCapacity: 2);

			massive.Assign(0);
			massive.Assign(1);
			massive.Assign(2);

			massive.SaveFrame();

			Assert.IsTrue(massive.IsAssigned(0));
			Assert.IsTrue(massive.IsAssigned(1));
			Assert.IsTrue(massive.IsAssigned(2));
		}

		[Test]
		public void RollbackZero_ShouldResetCurrentFrameChanges()
		{
			var massive = new MassiveSparseSet(setCapacity: 2, framesCapacity: 2);

			massive.Assign(0);

			Assert.IsTrue(massive.IsAssigned(0));

			massive.SaveFrame();

			massive.Unassign(0);

			Assert.IsFalse(massive.IsAssigned(0));

			massive.Rollback(0);

			Assert.IsTrue(massive.IsAssigned(0));
		}

		[Test]
		public void IsAlive_ShouldWorkCorrectWithRollback()
		{
			var massive = new MassiveSparseSet(setCapacity: 2, framesCapacity: 2);

			massive.SaveFrame();

			massive.Assign(0);
			massive.Assign(1);
			massive.Unassign(1);

			Assert.IsTrue(massive.IsAssigned(0));
			Assert.IsFalse(massive.IsAssigned(1));

			massive.SaveFrame();

			Assert.IsTrue(massive.IsAssigned(0));
			Assert.IsFalse(massive.IsAssigned(1));

			massive.Rollback(1);

			Assert.IsFalse(massive.IsAssigned(0));
			Assert.IsFalse(massive.IsAssigned(1));
		}
	}
}
