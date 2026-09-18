using UnityEngine;

namespace ProjectGatefront.Gameplay.Squad
{
    [DisallowMultipleComponent]
    public sealed class SquadUnit : MonoBehaviour
    {
        private Transform formationRoot;
        private Vector3 targetLocalPosition;
        private float transitionSpeed;
        private float rotationSpeed;

        public int SlotIndex { get; private set; } = -1;

        public bool IsActive => gameObject.activeSelf;

        public Vector3 TargetLocalPosition => targetLocalPosition;

        private void LateUpdate()
        {
            if (formationRoot == null)
            {
                return;
            }

            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                targetLocalPosition,
                transitionSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation,
                Quaternion.identity,
                rotationSpeed * Time.deltaTime);
        }

        public void Activate(
            Transform root,
            Vector3 spawnLocalPosition,
            float movementSpeed,
            float facingSpeed)
        {
            formationRoot = root;
            transitionSpeed = movementSpeed;
            rotationSpeed = facingSpeed;
            transform.SetParent(root, false);
            transform.localPosition = spawnLocalPosition;
            transform.localRotation = Quaternion.identity;
            gameObject.SetActive(true);
        }

        public void SetFormationSlot(FormationSlot slot)
        {
            SlotIndex = slot.Index;
            targetLocalPosition = slot.LocalPosition;
        }

        public void Deactivate()
        {
            SlotIndex = -1;
            formationRoot = null;
            gameObject.SetActive(false);
        }
    }
}
