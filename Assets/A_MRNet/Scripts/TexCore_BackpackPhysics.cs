using UnityEngine;

/// <summary>
/// Makes a backpack-taken object safe to manipulate without simulation forces
/// changing its pose. Collider and MRTK interaction components stay intact.
/// </summary>
public static class TexCore_BackpackPhysics
{
    public static void Stabilize(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (Rigidbody body in root.GetComponentsInChildren<Rigidbody>(true))
        {
            if (body == null)
            {
                continue;
            }

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = false;
            body.isKinematic = true;
        }

        foreach (ConstantForce force in root.GetComponentsInChildren<ConstantForce>(true))
        {
            if (force != null)
            {
                force.enabled = false;
            }
        }
    }
}
