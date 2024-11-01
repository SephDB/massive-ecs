using System.Collections.Generic;

namespace Massive
{
	public interface IGroupFactory
	{
		OwningGroup CreateOwningGroup(IReadOnlyList<SparseSet> owned, IReadOnlyList<SparseSet> include = null, IReadOnlyList<SparseSet> exclude = null);
		NonOwningGroup CreateNonOwningGroup(IReadOnlyList<SparseSet> include, IReadOnlyList<SparseSet> exclude = null, Entities entities = null);
	}
}
