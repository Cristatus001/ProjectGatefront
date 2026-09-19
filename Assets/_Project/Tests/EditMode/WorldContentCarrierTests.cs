using NUnit.Framework;
using ProjectGatefront.Data;
using ProjectGatefront.Gameplay.World;
using UnityEngine;

namespace ProjectGatefront.Tests.EditMode
{
    public sealed class WorldContentCarrierTests
    {
        [Test]
        public void MovingContent_PassesAndUnregistersForPooling()
        {
            var worldObject = new GameObject("World");
            var contentObject = new GameObject("FutureGateContent");
            var balance = ScriptableObject.CreateInstance<GameBalanceConfig>();
            try
            {
                WorldScroller world = worldObject.AddComponent<WorldScroller>();
                world.Configure(balance, null, -28f);
                world.Initialize();
                world.SetScrollSpeed(8f);
                contentObject.AddComponent<ScrollItem>();
                WorldContentCarrier content =
                    contentObject.AddComponent<WorldContentCarrier>();
                int passedEvents = 0;
                content.Passed += _ => passedEvents++;
                content.Activate(world, new Vector3(2f, 0f, 10f), 0f);

                Assert.That(content.IsActive, Is.True);
                Assert.That(content.HorizontalPosition, Is.EqualTo(2f));
                Assert.That(world.RegisteredItemCount, Is.EqualTo(1));
                world.SetScrollingEnabled(false);
                world.Tick(2f);
                content.Tick();
                Assert.That(content.IsActive, Is.True);

                world.SetScrollingEnabled(true);
                world.Tick(2f);
                content.Tick();
                content.Tick();
                Assert.That(passedEvents, Is.EqualTo(1));
                Assert.That(content.IsActive, Is.False);
                Assert.That(world.RegisteredItemCount, Is.Zero);
                Assert.That(contentObject.activeSelf, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(contentObject);
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(balance);
            }
        }
    }
}
