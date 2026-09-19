using NUnit.Framework;
using System.Collections.Generic;
using ProjectGatefront.Gameplay.Combat;
using UnityEngine;

namespace ProjectGatefront.Tests
{
    public sealed class RegistryTestTarget : MonoBehaviour, ITargetable
    {
        public bool Targetable = true;
        public float Radius = 0.5f;

        public Transform TargetTransform => transform;

        public Vector3 AimPosition => transform.position;

        public float TargetRadius => Radius;

        public bool IsTargetable => Targetable;
    }

    public sealed class TargetRegistryTests
    {
        private GameObject registryObject;
        private TargetRegistry registry;

        [SetUp]
        public void SetUp()
        {
            registryObject = new GameObject("TargetRegistryTest");
            registry = registryObject.AddComponent<TargetRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(registryObject);
            foreach (RegistryTestTarget target in
                Object.FindObjectsByType<RegistryTestTarget>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(target.gameObject);
            }
        }

        [Test]
        public void ClosestForwardTarget_RejectsBehindDeadAndOutOfRangeTargets()
        {
            RegistryTestTarget behind = CreateTarget("Behind", new Vector3(0f, 0f, -2f));
            RegistryTestTarget dead = CreateTarget("Dead", new Vector3(0f, 0f, 2f));
            RegistryTestTarget valid = CreateTarget("Valid", new Vector3(0f, 0f, 6f));
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 20f));
            dead.Targetable = false;
            registry.Register(behind);
            registry.Register(dead);
            registry.Register(valid);
            registry.Register(far);

            ITargetable selected = registry.FindClosestForwardTarget(
                Vector3.zero,
                Vector3.forward,
                10f,
                0f);

            Assert.That(selected, Is.SameAs(valid));
        }

        [Test]
        public void RegisterAndUnregister_AreDuplicateSafe()
        {
            RegistryTestTarget target = CreateTarget("Target", Vector3.forward);
            Assert.That(registry.Register(target), Is.True);
            Assert.That(registry.Register(target), Is.False);
            Assert.That(registry.ActiveTargetCount, Is.EqualTo(1));
            Assert.That(registry.Unregister(target), Is.True);
            Assert.That(registry.Unregister(target), Is.False);
            Assert.That(registry.ActiveTargetCount, Is.Zero);
        }

        [Test]
        public void SegmentHit_SelectsFirstTargetWithoutTunneling()
        {
            RegistryTestTarget near = CreateTarget("Near", new Vector3(0f, 0f, 4f));
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 8f));
            registry.Register(far);
            registry.Register(near);

            bool hit = registry.TryFindFirstSegmentHit(
                Vector3.zero,
                Vector3.forward * 10f,
                0.05f,
                null,
                out ITargetable target);

            Assert.That(hit, Is.True);
            Assert.That(target, Is.SameAs(near));
        }

        [Test]
        public void CandidateCollection_ReturnsNearestTargetsInOrder()
        {
            RegistryTestTarget far = CreateTarget("Far", new Vector3(0f, 0f, 9f));
            RegistryTestTarget near = CreateTarget("Near", new Vector3(0f, 0f, 3f));
            RegistryTestTarget middle = CreateTarget("Middle", new Vector3(0f, 0f, 6f));
            registry.Register(far);
            registry.Register(near);
            registry.Register(middle);
            var results = new List<ITargetable>();

            int count = registry.CollectClosestForwardTargets(
                Vector3.zero,
                Vector3.forward,
                10f,
                0f,
                2,
                results);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(results[0], Is.SameAs(near));
            Assert.That(results[1], Is.SameAs(middle));
        }

        private static RegistryTestTarget CreateTarget(
            string name,
            Vector3 position)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            return gameObject.AddComponent<RegistryTestTarget>();
        }
    }
}
