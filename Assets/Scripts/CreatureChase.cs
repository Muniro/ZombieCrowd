using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CreatureChase : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform target;
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float caughtDistance = 2.0f;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2.5f;
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

    [Header("Audio")]
    [SerializeField] private AudioSource roarSource;
    [SerializeField] private AudioClip roarClip;
    [SerializeField] private AudioSource footstepSource;

    [Header("Sniff")]
    [SerializeField] private Transform[] hidingPlaces;
    [SerializeField] private float sniffDistance = 5f;
    [SerializeField] private float sniffDuration = 1.5f;
    [SerializeField] private float sniffResetDistance = 7f;

    private readonly HashSet<Transform> sniffedThisVisit = new HashSet<Transform>();
    private NavMeshAgent agent;
    private bool playerCaught;
    private bool wasChasing;
    private bool movingToSearchPosition;
    private bool waitingAtPatrolPoint;
    private bool isSniffing;
    private bool hasSeenPlayer;
    private Vector3 lastKnownPosition;
    private int currentPatrolPoint;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (caughtOverlay == null) return;
        caughtOverlay.alpha = 0f;
        caughtOverlay.interactable = false;
        caughtOverlay.blocksRaycasts = false;
    }

    private void Start()
    {
        agent.acceleration = chaseAcceleration;
        MoveToCurrentPatrolPoint();
    }

    private void Update()
    {
        if (playerCaught || target == null || !agent.isOnNavMesh) return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        bool canSeePlayer = distanceToTarget <= detectionDistance && CanSeePlayer();

        if (canSeePlayer && distanceToTarget <= caughtDistance)
        {
            CatchPlayer();
            return;
        }

        if (canSeePlayer && !hasSeenPlayer && roarSource != null && roarClip != null)
            roarSource.PlayOneShot(roarClip);
        hasSeenPlayer = canSeePlayer;

        //if (isSniffing) return;

        if (!canSeePlayer && TryStartSniff()) return;

        if (canSeePlayer) ChasePlayer();
        else if (wasChasing) MoveToSearchPosition();
        else if (movingToSearchPosition) CheckSearchPosition();
        else Patrol();

        UpdateAnimation();
        UpdateFootsteps();
    }

    private bool TryStartSniff()
{
    if (hidingPlaces == null || waitingAtPatrolPoint)
        return false;

    foreach (Transform place in hidingPlaces)
    {
        if (place == null) continue;

        float distance = Vector3.Distance(
            transform.position, place.position);

        if (distance > Mathf.Max(
            sniffResetDistance, sniffDistance + 0.1f))
        {
            sniffedThisVisit.Remove(place);
        }

        if (distance > sniffDistance ||
            sniffedThisVisit.Contains(place))
            continue;

        sniffedThisVisit.Add(place);
        StartCoroutine(SniffAtHidingPlace(place));
        return true;
    }

    return false;
}

   private IEnumerator SniffAtHidingPlace(Transform place)
{
    isSniffing = true;
    agent.isStopped = true;

    if (footstepSource != null)
        footstepSource.Stop();

    Debug.Log($"Starting sniff at {place.name}");

    foreach (Animator creatureAnimator in creatureAnimators)
    {
        if (creatureAnimator == null) continue;

        creatureAnimator.speed = 1f;
        creatureAnimator.SetBool("isWalking", false);
        creatureAnimator.SetBool("isSniff", true);
    }

    yield return new WaitForSeconds(sniffDuration);

    foreach (Animator creatureAnimator in creatureAnimators)
    {
        if (creatureAnimator == null) continue;

        creatureAnimator.SetBool("isSniff", false);
        creatureAnimator.SetBool("isWalking", !playerCaught);
    }

    isSniffing = false;

    if (!playerCaught)
        agent.isStopped = false;
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

    private void UpdateFootsteps()
    {
        if (footstepSource == null || footstepSource.clip == null) return;
        bool moving = !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        if (!moving) footstepSource.Stop();
        else if (!footstepSource.isPlaying) footstepSource.Play();
    }

    private void MoveToSearchPosition()
    {
        Vector3 chaseDirection = lastKnownPosition - transform.position;
        chaseDirection = chaseDirection.sqrMagnitude > 0.01f
            ? chaseDirection.normalized : transform.forward;
        Vector3 intendedSearchPosition = lastKnownPosition +
            chaseDirection * searchOvershootDistance;

        agent.speed = chaseSpeed * 0.75f;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
        if (NavMesh.SamplePosition(intendedSearchPosition, out NavMeshHit hit,
                searchOvershootDistance, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(lastKnownPosition);

        movingToSearchPosition = true;
        wasChasing = false;
    }

    private void CheckSearchPosition()
    {
        if (!HasReachedDestination()) return;
        agent.ResetPath();
        movingToSearchPosition = false;
        MoveToCurrentPatrolPoint();
    }

    private void Patrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 || waitingAtPatrolPoint)
            return;
        if (HasReachedDestination()) StartCoroutine(WaitThenMoveToNextPatrolPoint());
    }

    private IEnumerator WaitThenMoveToNextPatrolPoint()
    {
        waitingAtPatrolPoint = true;
        agent.isStopped = true;
        agent.ResetPath();
        yield return new WaitForSeconds(patrolWaitTime);
        currentPatrolPoint = (currentPatrolPoint + 1) % patrolPoints.Length;
        waitingAtPatrolPoint = false;
        MoveToCurrentPatrolPoint();
    }

    private void MoveToCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0 ||
            patrolPoints[currentPatrolPoint] == null || !agent.isOnNavMesh)
            return;

        agent.speed = patrolSpeed;
        agent.stoppingDistance = stoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(patrolPoints[currentPatrolPoint].position);
    }

    private bool HasReachedDestination()
    {
        return !agent.pathPending && agent.hasPath &&
               agent.remainingDistance <= agent.stoppingDistance + 0.1f;
    }

    private void UpdateAnimation()
    {
        bool isWalking = !agent.isStopped && agent.velocity.sqrMagnitude > 0.05f;
        foreach (Animator creatureAnimator in creatureAnimators)
        {
            if (creatureAnimator == null) continue;
            creatureAnimator.SetBool("IsWalking", isWalking);
            creatureAnimator.speed = !isWalking ? 1f :
                wasChasing ? chaseAnimationSpeed : patrolAnimationSpeed;
        }
    }

    private bool CanSeePlayer()
    {
        Vector3 creatureEye = transform.position + Vector3.up * eyeHeight;
        Vector3 playerEye = target.position + Vector3.up * targetEyeHeight;
        Vector3 direction = playerEye - creatureEye;
        float distance = direction.magnitude;
        RaycastHit[] hits = Physics.RaycastAll(creatureEye, direction.normalized,
            distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;
            if (hit.transform == target || hit.transform.IsChildOf(target) ||
                target.IsChildOf(hit.transform))
            {
                Debug.DrawLine(creatureEye, playerEye, Color.green);
                return true;
            }
            Debug.DrawLine(creatureEye, hit.point, Color.red);
            return false;
        }
        return false;
    }

    private void CatchPlayer()
    {
        Debug.Log("CATCH TRIGGERED: creature can currently see the player.");
        playerCaught = true;
        agent.isStopped = true;
        agent.ResetPath();
        if (footstepSource != null) footstepSource.Stop();
        foreach (Animator creatureAnimator in creatureAnimators)
            if (creatureAnimator != null) creatureAnimator.SetBool("IsWalking", false);
        if (playerMovement != null) playerMovement.enabled = false;
        if (mouseLook != null) mouseLook.enabled = false;
        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        if (caughtOverlay == null) yield break;
        while (caughtOverlay.alpha < 1f)
        {
            caughtOverlay.alpha = Mathf.MoveTowards(caughtOverlay.alpha, 1f,
                Time.deltaTime / fadeDuration);
            yield return null;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
