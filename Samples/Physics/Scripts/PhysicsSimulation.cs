﻿using System.Diagnostics;
using Massive.Samples.Shooter;
using UnityEngine;

namespace Massive.Samples.Physics
{
	public class PhysicsWorld
	{
		public MassiveDataSet<SphereCollider> Spheres { get; }
		public MassiveDataSet<BoxCollider> Boxes { get; }
		public MassiveDataSet<Rigidbody> Bodies { get; }
		public MassiveDataSet<Contact> Contacts { get; }

		public PhysicsWorld(int framesCapacity)
		{
			Spheres = new MassiveDataSet<SphereCollider>(framesCapacity);
			Boxes = new MassiveDataSet<BoxCollider>(framesCapacity);
			Bodies = new MassiveDataSet<Rigidbody>(framesCapacity);
			Contacts = new MassiveDataSet<Contact>(framesCapacity);
		}
	}
	
	public class PhysicsSimulation : MonoBehaviour
	{
		[SerializeField, Range(0.01f, 10f)] private float _simulationSpeed = 1f;
		[SerializeField] private int _simulationsPerFrame = 120;
		[SerializeField] private int _particlesCapacity = 1000;

		[Header("Physics")]
		[SerializeField] private EntityRoot<SphereCollider> _spherePrefab;
		[SerializeField] private EntityRoot<BoxCollider> _boxPrefab;
		[SerializeField] private int _substeps = 8;
		[SerializeField] private float _gravity = 10f;

		private MassiveDataSet<SphereCollider> _sphereColliders;
		private MassiveDataSet<BoxCollider> _boxColliders;
		private MassiveDataSet<Rigidbody> _bodies;
		private EntitySynchronisation<SphereCollider> _spheresSynchronisation;
		private EntitySynchronisation<BoxCollider> _boxesSynchronisation;

		private void Awake()
		{
			_sphereColliders = new MassiveDataSet<SphereCollider>(framesCapacity: _simulationsPerFrame, dataCapacity: _particlesCapacity);
			_boxColliders = new MassiveDataSet<BoxCollider>(framesCapacity: _simulationsPerFrame, dataCapacity: _particlesCapacity);
			_bodies = new MassiveDataSet<Rigidbody>(framesCapacity: _simulationsPerFrame, dataCapacity: _particlesCapacity);
			
			_spheresSynchronisation = new EntitySynchronisation<SphereCollider>(new EntityFactory<SphereCollider>(_spherePrefab));
			_boxesSynchronisation = new EntitySynchronisation<BoxCollider>(new EntityFactory<BoxCollider>(_boxPrefab));

			foreach (var massiveRigidbody in FindObjectsOfType<MassiveRigidbody>())
			{
				massiveRigidbody.Spawn(_bodies, _sphereColliders, _boxColliders);
			}
			
			Rigidbody.RecalculateAllInertia(_bodies, _boxColliders, _sphereColliders);
		}

		private int _currentFrame;

		private float _elapsedTime;

		private void Update()
		{
			Stopwatch stopwatch = Stopwatch.StartNew();

			if (_sphereColliders.CanRollbackFrames >= 0)
			{
				_currentFrame -= _sphereColliders.CanRollbackFrames;
				_sphereColliders.Rollback(_sphereColliders.CanRollbackFrames);
				_bodies.Rollback(_bodies.CanRollbackFrames);
			}

			_elapsedTime += Time.deltaTime * _simulationSpeed;

			int targetFrame = Mathf.RoundToInt(_elapsedTime * 60);

			while (_currentFrame < targetFrame)
			{
				const float simulationDeltaTime = 1f / 60f;
				float subStepDeltaTime = simulationDeltaTime / _substeps;
				
				for (int i = 0; i < _substeps; i++)
				{
					Rigidbody.UpdateAllWorldInertia(_bodies);
					SphereCollider.UpdateWorldPositions(_bodies, _sphereColliders);
					BoxCollider.UpdateWorldPositions(_bodies, _boxColliders);
					Collisions.Solve(_bodies, _sphereColliders, _boxColliders);
					Gravity.Apply(_bodies, _gravity);
					Rigidbody.IntegrateAll(_bodies, subStepDeltaTime);
				}

				_sphereColliders.SaveFrame();
				_boxColliders.SaveFrame();
				_bodies.SaveFrame();
				_currentFrame++;
			}

			// float systemEnergy = 0f;
			// foreach (var body in _bodies.AliveData)
			// {
			// 	systemEnergy += body.Mass * Mathf.Max(0, body.Position.y + 100f) * _gravity;
			// 	systemEnergy += body.Velocity.sqrMagnitude * body.Mass / 2f;
			// }
			//
			// Debug.Log(systemEnergy);
			
			_spheresSynchronisation.Synchronize(_sphereColliders);
			_boxesSynchronisation.Synchronize(_boxColliders);

			_debugTime = stopwatch.ElapsedMilliseconds;
		}

		private long _debugTime;

		private void OnGUI()
		{
			float fontScaling = Screen.height / (float)1080;
			
			GUILayout.TextField($"{_debugTime}ms Simulation", new GUIStyle() { fontSize = Mathf.RoundToInt(70 * fontScaling), normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_sphereColliders.CanRollbackFrames} Resimulations", new GUIStyle() { fontSize = Mathf.RoundToInt(50 * fontScaling), normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_sphereColliders.AliveCount} Spheres", new GUIStyle() { fontSize = Mathf.RoundToInt(50 * fontScaling), normal = new GUIStyleState() { textColor = Color.white } });
			GUILayout.TextField($"{_boxColliders.AliveCount} Boxes", new GUIStyle() { fontSize = Mathf.RoundToInt(50 * fontScaling), normal = new GUIStyleState() { textColor = Color.white } });
		}
	}
}