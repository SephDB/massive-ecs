using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.IL2CPP.CompilerServices;

namespace Massive
{
	[Il2CppSetOption(Option.NullChecks | Option.ArrayBoundsChecks, false)]
	public class GroupRegistry
	{
		private readonly GenericLookup<IGroup> _groupLookup = new GenericLookup<IGroup>();
		private readonly Dictionary<SparseSet, IOwningGroup> _ownedBase = new Dictionary<SparseSet, IOwningGroup>();
		private readonly SetRegistry _setRegistry;
		private readonly IGroupFactory _groupFactory;

		public GroupRegistry(SetRegistry setRegistry, IGroupFactory groupFactory)
		{
			_setRegistry = setRegistry;
			_groupFactory = groupFactory;
		}

		public ReadOnlySpan<IGroup> All
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _groupLookup.All;
		}

		public IGroup Get<TInclude, TExclude, TOwn>()
			where TInclude : IIncludeSelector, new()
			where TExclude : IExcludeSelector, new()
			where TOwn : IOwnSelector, new()
		{
			var group = _groupLookup.Find<Tuple<TInclude, TExclude, TOwn>>();

			if (group != null)
			{
				group.EnsureSynced();
				return group;
			}

			var owned = new TOwn().Select(_setRegistry);
			var include = new TInclude().Select(_setRegistry);
			var exclude = new TExclude().Select(_setRegistry);

			if (Array.Exists(owned, set => set.PackingMode == PackingMode.WithHoles))
			{
				throw new Exception($"Sets with direct storage are not supported for owning: <{typeof(TOwn).GetFullGenericName()}>.");
			}

			// If non-owning, then just create new one
			if (owned.Length == 0)
			{
				var nonOwningGroup = _groupFactory.CreateNonOwningGroup(include, exclude);
				return RegisterAndSync<Tuple<TInclude, TExclude, TOwn>>(nonOwningGroup);
			}

			// Find base group for any owned set
			IOwningGroup baseGroup = null;
			foreach (var ownedSet in owned)
			{
				if (_ownedBase.TryGetValue(ownedSet, out baseGroup))
				{
					break;
				}
			}

			// If there is no base group, just create new owning group
			if (baseGroup == null)
			{
				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				foreach (var set in owned)
				{
					_ownedBase.Add(set, owningGroup);
				}
				return RegisterAndSync<Tuple<TInclude, TExclude, TOwn>>(owningGroup);
			}

			// Try to create new group as extension to the base group
			if (baseGroup.BaseForGroup(owned, include, exclude))
			{
				var baseGroupNode = baseGroup;

				// Find most nested group that is base for our
				while (baseGroupNode.Extended != null && baseGroupNode.Extended.BaseForGroup(owned, include, exclude))
				{
					baseGroupNode = baseGroupNode.Extended;
				}

				// Check if the next group can extend ours
				if (baseGroupNode.Extended != null && !baseGroupNode.Extended.ExtendsGroup(owned, include, exclude))
				{
					throw new Exception($"Conflicting group: <{typeof(TOwn).GetFullGenericName()}, " +
					                    $"{typeof(TInclude).GetFullGenericName()}, {typeof(TExclude).GetFullGenericName()}>.");
				}

				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				baseGroupNode.AddGroupAfterThis(owningGroup);
				return RegisterAndSync<Tuple<TInclude, TExclude, TOwn>>(owningGroup);
			}

			// Try to create group as a new base group
			if (baseGroup.ExtendsGroup(owned, include, exclude))
			{
				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				baseGroup.AddGroupBeforeThis(owningGroup);
				foreach (var set in owned)
				{
					_ownedBase[set] = owningGroup;
				}
				return RegisterAndSync<Tuple<TInclude, TExclude, TOwn>>(owningGroup);
			}

			throw new Exception($"Conflicting group: <{typeof(TOwn).GetFullGenericName()}, " +
			                    $"{typeof(TInclude).GetFullGenericName()}, {typeof(TExclude).GetFullGenericName()}>.");
		}

		public IGroup Get(Type includeSelector, Type excludeSelector, Type ownSelector)
		{
			var groupKey = typeof(Tuple<,,>).MakeGenericType(includeSelector, excludeSelector, typeof(None));

			var group = _groupLookup.Find(groupKey);

			if (group != null)
			{
				group.EnsureSynced();
				return group;
			}

			var include = ((IIncludeSelector)Activator.CreateInstance(includeSelector)).Select(_setRegistry);
			var exclude = ((IExcludeSelector)Activator.CreateInstance(excludeSelector)).Select(_setRegistry);
			var owned = ((IOwnSelector)Activator.CreateInstance(ownSelector)).Select(_setRegistry);

			if (Array.Exists(owned, set => set.PackingMode == PackingMode.WithHoles))
			{
				throw new Exception($"Sets with direct storage are not supported for owning: <{ownSelector.GetFullGenericName()}>.");
			}

			// If non-owning, then just create new one
			if (owned.Length == 0)
			{
				var nonOwningGroup = _groupFactory.CreateNonOwningGroup(include, exclude);
				return RegisterAndSync(groupKey, nonOwningGroup);
			}

			// Find base group for any owned set
			IOwningGroup baseGroup = null;
			foreach (var ownedSet in owned)
			{
				if (_ownedBase.TryGetValue(ownedSet, out baseGroup))
				{
					break;
				}
			}

			// If there is no base group, just create new owning group
			if (baseGroup == null)
			{
				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				foreach (var set in owned)
				{
					_ownedBase.Add(set, owningGroup);
				}
				return RegisterAndSync(groupKey, owningGroup);
			}

			// Try to create new group as extension to the base group
			if (baseGroup.BaseForGroup(owned, include, exclude))
			{
				var baseGroupNode = baseGroup;

				// Find most nested group that is base for our
				while (baseGroupNode.Extended != null && baseGroupNode.Extended.BaseForGroup(owned, include, exclude))
				{
					baseGroupNode = baseGroupNode.Extended;
				}

				// Check if the next group can extend ours
				if (baseGroupNode.Extended != null && !baseGroupNode.Extended.ExtendsGroup(owned, include, exclude))
				{
					throw new Exception($"Conflicting group: <{ownSelector.GetFullGenericName()}, " +
					                    $"{includeSelector.GetFullGenericName()}, {excludeSelector.GetFullGenericName()}>.");
				}

				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				baseGroupNode.AddGroupAfterThis(owningGroup);
				return RegisterAndSync(groupKey, owningGroup);
			}

			// Try to create group as a new base group
			if (baseGroup.ExtendsGroup(owned, include, exclude))
			{
				var owningGroup = _groupFactory.CreateOwningGroup(owned, include, exclude);
				baseGroup.AddGroupBeforeThis(owningGroup);
				foreach (var set in owned)
				{
					_ownedBase[set] = owningGroup;
				}
				return RegisterAndSync(groupKey, owningGroup);
			}

			throw new Exception($"Conflicting group: <{ownSelector.GetFullGenericName()}, " +
			                    $"{includeSelector.GetFullGenericName()}, {excludeSelector.GetFullGenericName()}>.");
		}

		public (Type IncludeSelector, Type ExcludeSelector, Type OwnSelector) GetSelectorsOfGroup(IGroup group)
		{
			var groupType = _groupLookup.TypeOf(group);
			var genericArguments = groupType.GetGenericArguments();
			return (genericArguments[0], genericArguments[1], genericArguments[2]);
		}

		private IGroup RegisterAndSync<TGroupKey>(IGroup group)
		{
			_groupLookup.Assign<TGroupKey>(group);
			group.EnsureSynced();
			return group;
		}

		private IGroup RegisterAndSync(Type groupKey, IGroup group)
		{
			_groupLookup.Assign(groupKey, group);
			group.EnsureSynced();
			return group;
		}
	}
}
