using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [SerializeField] private Transform leftDoor;
    [SerializeField] private Transform rightDoor;

    [SerializeField] private float openDistance = 2.5f;
    [SerializeField] private float movementSpeed = 3f;

    private Vector3 leftClosedPosition;
    private Vector3 rightClosedPosition;

    private Vector3 leftOpenPosition;
    private Vector3 rightOpenPosition;

    private bool shouldOpen;

    private void Awake()
    {
        leftClosedPosition = leftDoor.localPosition;
        rightClosedPosition = rightDoor.localPosition;

        leftOpenPosition =
            leftClosedPosition + Vector3.left * openDistance;

        rightOpenPosition =
            rightClosedPosition + Vector3.right * openDistance;
    }

    private void Update()
    {
        Vector3 leftTarget =
            shouldOpen ? leftOpenPosition : leftClosedPosition;

        Vector3 rightTarget =
            shouldOpen ? rightOpenPosition : rightClosedPosition;

        leftDoor.localPosition = Vector3.MoveTowards(
            leftDoor.localPosition,
            leftTarget,
            movementSpeed * Time.deltaTime);

        rightDoor.localPosition = Vector3.MoveTowards(
            rightDoor.localPosition,
            rightTarget,
            movementSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            shouldOpen = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            shouldOpen = false;
    }
}