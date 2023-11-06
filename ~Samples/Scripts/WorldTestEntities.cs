﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Massive.Samples
{
    public class WorldTestEntities : MonoBehaviour
    {
        [SerializeField] private float _simulationSpeed = 1f;
        [SerializeField] private int _resimulate = 0;

        private List<ITickable> _tickables;
        private WorldState<TestEntityState> _worldState;
        private WorldTime _worldTime;

        private void Start()
        {
            var entities = FindObjectsOfType<TestEntity>();
            _worldState = new WorldState<TestEntityState>(maxFrames: 2400, maxStatesPerFrame: 150);

            _worldTime = new WorldTime(60);

            foreach (TestEntity testEntity in entities)
            {
                testEntity.Register(
                    _worldState.Reserve(new TestEntityState(testEntity.transform.position, testEntity.transform.rotation)),
                    _worldTime);
            }

            _tickables = new List<ITickable>();
            _tickables.Add(_worldTime);
            _tickables.AddRange(entities);
        }

        [field: SerializeField] public float ElapsedTime { get; set; }

        public int TargetTick => Mathf.FloorToInt(ElapsedTime * _worldTime.TicksPerSecond);
        public int CurrentTick { get; set; }
        private int EarliestApprovedTick { get; set; }

        private void Update()
        {
            ElapsedTime += Time.deltaTime * _simulationSpeed;
            ElapsedTime = Mathf.Max(ElapsedTime, 0);

            EarliestApprovedTick = TargetTick - _resimulate;

            FastForward(TargetTick);

            void FastForward(int targetTick)
            {
                int earliestTick = Math.Min(targetTick, EarliestApprovedTick);
                int stepsToRollback = Math.Max(CurrentTick - earliestTick, 0);

                _worldState.Rollback(stepsToRollback);
                CurrentTick -= stepsToRollback;

                while (CurrentTick < targetTick)
                {
                    foreach (var tickable in _tickables)
                    {
                        tickable.Tick();
                    }
                    _worldState.SaveFrame();
                    CurrentTick += 1;
                }

                EarliestApprovedTick = CurrentTick;
            }
        }
    }
}
