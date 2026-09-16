using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CreatureChase : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float detectionDistance = 30f;
    [SerializeField] private float caughtDistance = 1.4f;

    [Header("Caught sequence")]
    [SerializeField] private SimplePlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private CanvasGroup caughtOverlay;
    [SerializeField] private float fadeDuration = 1f;

    private NavMeshAgent agent;
    private bool playerCaught;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (playerCaught || target == null || !agent.isOnNavMesh)
            return;

        float distanceToTarget =
            Vector3.Distance(transform.position, target.position);

        if (distanceToTarget <= caughtDistance)
        {
            CatchPlayer();
            return;
        }

        if (distanceToTarget <= detectionDistance)
            agent.SetDestination(target.position);
        else
            agent.ResetPath();
    }

    private void CatchPlayer()
    {
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