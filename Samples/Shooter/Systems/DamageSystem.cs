﻿namespace Massive.Samples.Shooter
{
	public static class DamageSystem
	{
		public static void Update(Registry registry, float deltaTime)
		{
			var characters = registry.DataSet<Character>();
			var bullets = registry.DataSet<Bullet>();
			var colliders = registry.DataSet<CircleCollider>();
			var positions = registry.DataSet<Position>();

			foreach (var characterId in registry.View().Filter<Include<Character>, Exclude<Dead>>())
			{
				ref var character = ref characters.Get(characterId);

				foreach (var bulletId in registry.View().Filter<Include<Bullet>, Exclude<Dead>>())
				{
					ref var bullet = ref bullets.Get(bulletId);

					// Don't collide a character with its own bullet.
					if (bullet.Owner == registry.GetEntity(characterId))
					{
						continue;
					}

					if (IsCollided(bulletId, characterId))
					{
						registry.Assign(bulletId, new Dead());

						character.Health -= bullet.Damage;
						if (character.Health <= 0)
						{
							registry.Assign(characterId, new Dead());
							DestroyCharacterBullets(characterId);
							break;
						}
					}
				}
			}

			void DestroyCharacterBullets(int characterId)
			{
				registry.View().Exclude<Dead>().ForEachExtra((characterId, registry),
					static (int bulletId, ref Bullet bullet, (int CharacterId, Registry Registry) args) =>
					{
						var (characterId, registry) = args;

						if (bullet.Owner == registry.GetEntity(characterId))
						{
							registry.Assign<Dead>(bulletId);
						}
					});
			}

			bool IsCollided(int firstId, int secondId)
			{
				ref var firstPosition = ref positions.Get(firstId);
				ref var firstCollider = ref colliders.Get(firstId);
				ref var secondPosition = ref positions.Get(secondId);
				ref var secondCollider = ref colliders.Get(secondId);

				return CircleCollider.IsCollided(
					firstPosition.Value, firstCollider.Radius,
					secondPosition.Value, secondCollider.Radius);
			}
		}
	}
}
