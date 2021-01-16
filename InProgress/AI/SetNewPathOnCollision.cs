using UnityEngine;

public class SetNewPathOnCollision : MonoBehaviour
{
    public PatrolPath path;

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponentInParent<A_EnemyNavController>();
        if (!controller) return;
        controller.SetNewPath(path);
        controller.SetPathDestinationToClosestNode();
        controller.getRandomPathIndex = true;
    }
}
