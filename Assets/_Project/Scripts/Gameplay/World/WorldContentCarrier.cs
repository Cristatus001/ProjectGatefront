using System;
using UnityEngine;

namespace ProjectGatefront.Gameplay.World
{
    // A reusable moving-world path for future gates, pickups, and encounters.
    // Selection/effects belong to the content component, not this carrier.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(ScrollItem))]
    public sealed class WorldContentCarrier : MonoBehaviour
    {
        private ScrollItem scrollItem;
        private WorldScroller worldScroller;
        private float passWorldZ;

        public event Action<WorldContentCarrier> Passed;

        public bool IsActive { get; private set; }

        public float HorizontalPosition => transform.position.x;

        private void Awake()
        {
            scrollItem = GetComponent<ScrollItem>();
        }

        private void Update()
        {
            Tick();
        }

        public void Activate(
            WorldScroller scroller,
            Vector3 spawnPosition,
            float passThresholdZ)
        {
            if (scroller == null)
            {
                throw new ArgumentNullException(nameof(scroller));
            }

            Deactivate();
            worldScroller = scroller;
            scrollItem = scrollItem != null ? scrollItem : GetComponent<ScrollItem>();
            scrollItem.Configure(false, 0f);
            passWorldZ = passThresholdZ;
            transform.position = spawnPosition;
            gameObject.SetActive(true);
            worldScroller.Register(scrollItem);
            IsActive = true;
        }

        public void Tick()
        {
            if (!IsActive
                || worldScroller == null
                || !worldScroller.ScrollingEnabled
                || transform.position.z > passWorldZ)
            {
                return;
            }

            Deactivate();
            Passed?.Invoke(this);
        }

        public void Deactivate()
        {
            if (worldScroller != null && scrollItem != null)
            {
                worldScroller.Unregister(scrollItem);
            }

            IsActive = false;
            worldScroller = null;
            if (gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (worldScroller != null && scrollItem != null)
            {
                worldScroller.Unregister(scrollItem);
            }

            IsActive = false;
            worldScroller = null;
        }
    }
}
