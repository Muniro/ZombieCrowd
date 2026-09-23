using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
public class CreatureAnimationDriver : MonoBehaviour
{
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float movingThreshold = 0.05f;

    private Animator animator;
    private static readonly int IsMoving =
        Animator.StringToHash("IsMoving");

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();
    }

    private void Update()
    {
        bool isMoving =
            agent != null &&
            agent.isOnNavMesh &&
            agent.velocity.sqrMagnitude >
            movingThreshold * movingThreshold;

        animator.SetBool(IsMoving, isMoving);
    }
}