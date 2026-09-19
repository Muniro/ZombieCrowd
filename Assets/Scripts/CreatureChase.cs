using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CreatureChase : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float caughtDistance = 1.4f;

    [Header("Vision")]
    [SerializeField] private LayerMask sightBlockingLayers;
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private float targetEyeHeight = 1f;
    [SerializeField] private float searchOvershootDistance = 5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("Caught sequence")]
    [SerializeField] private SimplePlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private CanvasGroup caughtOverlay;
    [SerializeField] private float fadeDuration = 1f;

    private NavMeshAgent agent;
    private bool playerCaught;
    private bool wasChasing;
    private bool movingToSearchPosition;
    private Vector3 lastKnownPosition;

   private void Awake()
{
    agent = GetComponent<NavMeshAgent>();

    if (caughtOverlay != null)
    {
        caughtOverlay.alpha = 0f;
        caughtOverlay.interactable = false;
        caughtOverlay.blocksRaycasts = false;
    }
}

    private void Update()
    {
        if (playerCaught || target == null || !agent.isOnNavMesh)
            return;

        float distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        bool canSeePlayer =
            distanceToTarget <= detectionDistance && CanSeePlayer();

        // The creature must be close AND able to see the player.
        if (canSeePlayer && distanceToTarget <= caughtDistance)
        {
            CatchPlayer();
            return;
        }

        if (canSeePlayer)
        {
            lastKnownPosition = target.position;
            wasChasing = true;
            movingToSearchPosition = false;

            agent.isStopped = false;
            agent.SetDestination(target.position);
        }
        else if (wasChasing)
        {
            // Continue beyond the last place where the player was seen.
            Vector3 chaseDirection =
                (lastKnownPosition - transform.position).normalized;

            Vector3 intendedSearchPosition =
                lastKnownPosition +
                chaseDirection * searchOvershootDistance;

            if (NavMesh.SamplePosition(
                    intendedSearchPosition,
                    out NavMeshHit hit,
                    searchOvershootDistance,
                    NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                movingToSearchPosition = true;
            }
            else
            {
                agent.SetDestination(lastKnownPosition);
                movingToSearchPosition = true;
            }

            wasChasing = false;
        }
        else if (movingToSearchPosition &&
                 !agent.pathPending &&
                 agent.remainingDistance <= stoppingDistance)
        {
            agent.ResetPath();
            movingToSearchPosition = false;
        }
    }

    private bool CanSeePlayer()
{
    Vector3 creatureEye =
        transform.position + Vector3.up * eyeHeight;

    Vector3 playerEye =
        target.position + Vector3.up * targetEyeHeight;

    Vector3 direction = playerEye - creatureEye;
    float distance = direction.magnitude;

    RaycastHit[] hits = Physics.RaycastAll(
        creatureEye,
        direction.normalized,
        distance,
        Physics.AllLayers,
        QueryTriggerInteraction.Ignore);

    System.Array.Sort(
        hits,
        (first, second) =>
            first.distance.CompareTo(second.distance));

    foreach (RaycastHit hit in hits)
    {
        // Ignore the creature's own colliders.
        if (hit.transform == transform ||
            hit.transform.IsChildOf(transform))
        {
            continue;
        }

        // If the first relevant object is the player, it is visible.
        if (hit.transform == target ||
            hit.transform.IsChildOf(target) ||
            target.IsChildOf(hit.transform))
        {
            Debug.DrawLine(
                creatureEye,
                playerEye,
                Color.green);

            return true;
        }

        // Anything else, such as the wall, blocks sight.
        Debug.DrawLine(
            creatureEye,
            hit.point,
            Color.red);

        return false;
    }

    return false;
}

    private void CatchPlayer()
    {
        Debug.Log("CATCH TRIGGERED: creature can currently see the player.");
        playerCaught = true;
        agent.isStopped = true;

        if (playerMovement != null)
            playerMovement.enabled = false;

        if (mouseLook != null)
            mouseLook.enabled = false;

        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        if (caughtOverlay == null)
            yield break;

        while (caughtOverlay.alpha < 1f)
        {
            caughtOverlay.alpha = Mathf.MoveTowards(
                caughtOverlay.alpha,
                1f,
                Time.deltaTime / fadeDuration);

            yield return null;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}