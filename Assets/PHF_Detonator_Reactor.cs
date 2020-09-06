using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PHF_Detonator_Reactor : MonoBehaviour
{
    [Header("Reaction Parameters")]
    [Tooltip("Default: 1x")]
    [Range(1, 5)] public float horizontalForceMultiplier = 1f;
    [Tooltip("Default: 1.05x")]
    [Range(1, 5)] public float upwardForceMultiplier = 1.05f;

    [Header("Debug")]
    public bool debugMessages = false;
    public GameObject debugSphere;

    private Ray ray;
    private RaycastHit theRayhit;

    //Call back from PHF_Detonator
    void StartReactor(OriginatorInfo origInfo)
    {
        if (debugMessages)
        {
            Debug.Log("******** StartReactor - IamProtected == True *********");
        }
        if (!IamProtected(origInfo.originator))
        {
            if (debugMessages)
            {
                Debug.Log("******** StartReactor - IamProtected == False *********");
            }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                float origForce = origInfo.originator.transform.GetComponent<PHF_Detonator>().explosionForce;
                float origRadius = origInfo.originator.transform.GetComponent<PHF_Detonator>().explosionRadius;
                float percentageDistanceToTarget = GetTargetDistance(origInfo.originator) / origRadius;
                float appliedForce = (1 - percentageDistanceToTarget) * origForce * horizontalForceMultiplier;
                if (debugMessages)
                {
                    Debug.Log("Percentage of DistanceToTarget: " + percentageDistanceToTarget.ToString());
                }

                float reactionRadius = origRadius;

                Vector3 forcePosition = transform.position + -transform.forward + GetTargetDirection(origInfo.originator);
                if (debugMessages)
                {
                    Debug.Log("forcePosition: " + forcePosition.ToString());
                }
                //rb.AddExplosionForce(appliedForce, new Vector3(transform.position.x + (Random.value * .25f), transform.position.y, transform.position.z + (Random.value * .25f)), reactionRadius, Random.Range(2.0f, 2.5f));
                rb.AddExplosionForce(appliedForce, forcePosition, reactionRadius, upwardForceMultiplier);
            }
        }
    }

    private Vector3 GetTargetDirection(GameObject originator)
    {
        if (originator?.GetComponent<PHF_Detonator>()?.XRPoserGrenade == true) ////////////////// HACK TO WORK WITH XRPoser Grenade /////////////////////////
        {
            var heading = originator.transform.position - transform.GetComponent<Renderer>().bounds.center;
            var distance = heading.magnitude;
            var direction = heading / distance; // This is now the normalized direction.
            return direction;
        }
        else
        {
            var heading = originator.transform.GetComponent<Renderer>().bounds.center - transform.GetComponent<Renderer>().bounds.center;
            var distance = heading.magnitude;
            var direction = heading / distance; // This is now the normalized direction.
            return direction;
        }
    }

    private float GetTargetDistance(GameObject originator)
    {
        return Vector3.Distance(transform.position, originator.transform.position);
    }

    private bool IamProtected(GameObject originator)
    {
        // if (originator?.GetComponent<PHF_SendDamageInfo>()) ////////////////// HACK TO WORK WITH XRPoser Rifle Bullet /////////////////////////
        // {
        //     return false;
        // }

        if (debugMessages)
        {
             Debug.Log("***************IamProtected********************");
            Debug.Log("IamProtected 01: Originator Name: " + originator.name + " My name: " + this.gameObject.name);
        }

        Vector3 rayOrigin = transform.GetComponent<Renderer>().bounds.center;
        ray = new Ray(rayOrigin, GetTargetDirection(originator));


        if (!this.gameObject.Equals(originator)) //Is this me or someone else?
        {
            //It's not me.
            if (Physics.Raycast(ray, out theRayhit, Mathf.Infinity)) // If a raycast hits SOMETHING with a collider
            {
                if (debugSphere)
                {
                    Instantiate(debugSphere, originator.transform.position, Quaternion.identity);
                }
                
                if (debugMessages)
                {
                    Debug.Log("IamProtected 02: Originator Name: " + originator.name + " My name: " + this.gameObject.name + " [Raycast Hit True]");
                }
                
                if (theRayhit.transform.gameObject.name == originator.name) // I can see the originator.
                {
                    if (debugMessages)
                    {
                        Debug.Log("IamProtected 03: Originator Name: " + originator.name + " My name: " + this.gameObject.name + " [Raycast Can see Originator]");
                        Debug.DrawLine(transform.position, theRayhit.point, Color.red);
                    }
                    return false;
                }
                else //I can see another object's collider, but it's not the originator so I'm safe.
                {
                    if (debugMessages)
                    {
                        Debug.Log("IamProtected 04: Originator Name: " + originator.name + " My name: " + this.gameObject.name + " [Raycast Can see " + theRayhit.transform.name + ". Since it's not the Originator, " + this.gameObject.name + " will NOT detonate!");
                    }
                    return true;
                }

            }
            else //I can't see another object with a collider, so I can't see the originator, so I'm protected.
            {
                if (debugMessages)
                {
                    Debug.Log("IamProtected 05: Originator Name: " + originator.name + " My name: " + this.gameObject.name + " [Raycast Can't see ANY object]");
                    Debug.DrawLine(transform.position, theRayhit.point, Color.green);
                }
                return true;
            }
        }
        else
        {
            if (debugMessages)
            {
                Debug.Log("IamProtected 06: Originator Name: " + originator.name + " My name: " + this.gameObject.name + " [Comparison: It's me.] " + this.gameObject.name + " will detonate!");
            }

            //Can't happen, I'm not an explodable. Return false or true works because this code is never evaluated
            return false;
        }
    }
}