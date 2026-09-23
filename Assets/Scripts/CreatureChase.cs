using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CreatureChase : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float caughtDistance = 1.4f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float patrolWaitTime = 1.5f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4.5f;
    [SerializeField] private float chaseAcceleration = 14f;

    [Header("Vision and searching")]
    [SerializeField] private float eyeHeight = 1.2f;
    [SerializeField] private float targetEyeHeight = 1f;
    [SerializeField] private float searchOvershootDistance = 5f;
    [SerializeField] private float stoppingDistance = 0.5f;

    [Header("Animation")]
    [SerializeField] private Animator[] creatureAnimators;
    [SerializeField] private float patrolAnimationSpeed = 0.9f;
    [SerializeField] private float chaseAnimationSpeed = 1.4f;

    [Header("Caught sequence")]
    [SerializeField] private SimplePlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private CanvasGroup caughtOverlay;
    [SerializeField] private float fadeDuration = 1f;

    private NavMeshAgent agent;

    private bool playerCaught;
    private bool wasChasing;
    private bool movingToSearchPosition;
    private bool waitingAtPatrolPoint;

    private Vector3 lastKnownPosition;
    private int currentPatrolPoint;

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

    private void Start()
    {
        agent.acceleration = chaseAcceleration;
        MoveToCurrentPatrolPoint();
    }

    private void Update()
    {
        if (playerCaught || target == null || !agent.isOnNavMesh)
            return;

        float distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        bool canSeePlayer =
            distanceToTarget <= detectionDistance &&
            CanSeePlayer();

        if (canSeePlayer && distanceToTarget <= caughtDistance)
        {
            CatchPlayer();
            return;
        }

        if (canSeePlayer)
        {
            ChasePlayer();
        }
        else if (wasChasing)
        {
            MoveToSearchPosition();
        }
        else if (movingToSearchPosition)
        {
            CheckSearchPosition();
        }
        else
        {
            Patrol();
        }

        UpdateAnimation();
    }

    private void ChasePlayer()
    {
        lastKnownPosition = target.position;

        wasChasing = true;
        movingToSearchPosition = false;
        waitingAtPatrolPoint = false;

        agent.speed = chaseSpeed;
        agent.acceleration = chaseAcceleration;
        agent.stoppingDistance = caughtDistance * 0.75f;
        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private void MoveToSearchPosition()
    {
        Vector3 chaseDirection =
            lastKnownPosition - transform.position;

        if (chaseDirection.sqrMagnitude > 0.01f)
            chaseDirection.Normalize();
        else
            chaseDirection = transform.forward;

        Vector3 intendedSearchPosition =
            lastKnownPosition +
            chaseDirection * searchOvershootDistance;

        agent.speed = chaseSpeed * 0.75f;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;

        if (NavMesh.SamplePosition(
                intendedSearchPosition,
                out NavMeshHit hit,
                searchOvershootDistance,
                NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            agent.SetDestination(lastKnownPosition);
        }

        movingToSearchPosition = true;
        wasChasing = false;
    }

    private void CheckSearchPosition()
    {
        if (!HasReachedDestination())
            return;

        agent.ResetPath();
        movingToSearchPosition = false;
        MoveToCurrentPatrolPoint();
    }

    private void Patrol()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0 ||
            waitingAtPatrolPoint)
        {
            return;
        }

        if (HasReachedDestination())
            StartCoroutine(WaitThenMoveToNextPatrolPoint());
    }

    private IEnumerator WaitThenMoveToNextPatrolPoint()
    {
        waitingAtPatrolPoint = true;

        agent.isStopped = true;
        agent.ResetPath();

        yield return new WaitForSeconds(patrolWaitTime);

        currentPatrolPoint =
            (currentPatrolPoint + 1) % patrolPoints.Length;

        waitingAtPatrolPoint = false;
        MoveToCurrentPatrolPoint();
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0 ||
            patrolPoints[currentPatrolPoint] == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(
            patrolPoints[currentPatrolPoint].position);
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending &&
               agent.hasPath &&
               agent.remainingDistance <=
               agent.stoppingDistance + 0.1f;
    }

    private void UpdateAnimation()
   {
    bool isWalking =
        !agent.isStopped &&
        agent.velocity.sqrMagnitude > 0.05f;

    foreach (Animator animator in creatureAnimators)
    {
        if (animator == null)
            continue;

        animator.SetBool("IsWalking", isWalking);

        animator.speed = !isWalking
            ? 1f
            : wasChasing
                ? chaseAnimationSpeed
                : patrolAnimationSpeed;
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
            if (hit.transform == transform ||
                hit.transform.IsChildOf(transform))
            {
                continue;
            }

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
        Debug.Log(
            "CATCH TRIGGERED: creature can currently see the player.");

        playerCaught = true;
        agent.isStopped = true;
        agent.ResetPath();

       foreach (Animator animator in creatureAnimators)
        {
            if (animator != null)
                animator.SetBool("IsWalking", false);
        }
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