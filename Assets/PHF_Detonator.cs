using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PHF_Detonator : MonoBehaviour
{
    [Header("Explosion Parameters")]
    [Tooltip("Default: 1500")]
    [Range(0, 3000)] public float explosionForce = 1500.0f;
    [Tooltip("Default: 5")]
    [Range(0, 10)] public float explosionRadius = 5.0f;
    [Tooltip("Default: 100 - Higher numbers equate to faster chain reactions.")]
    [Range(0, 500)] public float DetonationVelocity = 100.0f;
    [Range(.2f, 1f)] public float SelfDestructDelay = .2f;

    [Header("Settings")]
    public bool ActivateOnStart = false;
    public int integrity = 100;
    [Tooltip("True ONLY if attached to XRPoser Grenade: Default is False.")]
    public bool XRPoserGrenade = false;
    public bool useEnemyAI = false;
    public bool affectPlayer = false;
    GameObject XRRigAimTarget;

    [Header("Effects - IGNORE for XRPoser Grenades")]
    public AudioClip explosionAudio;
    public GameObject ParticleFXPrefab;
    public GameObject[] pieces;

    [Header("Debug")]
    public bool debugMessages = false;

    private bool hasExploded = false;
    private Ray ray;
    private RaycastHit theRayhit;

    private void Awake()
    {
        XRRigAimTarget = GameObject.FindGameObjectWithTag("Player").transform.Find("AimTarget").gameObject;
    }

    public void Start()
    {
        if (ActivateOnStart)
        {
            OriginatorInfo activateInfo = new OriginatorInfo(100, this.gameObject, false);
            Detonate(activateInfo);
        }
    }

    //Callback from this script or Send_DamageInfo
    public IEnumerator StartDetonator(OriginatorInfo oInfo)
    {
        yield return new WaitForSeconds(explosionRadius / DetonationVelocity);
        Detonate(oInfo);
    }

    void Detonate(OriginatorInfo origInfo)
    {
        if (hasExploded)
        {
            return;
        }

        if (!ActivateOnStart)
        {
            if (debugMessages)
            {
                Debug.Log(this.gameObject.name + ": Received message to Detonate");
            }
        }
        else
        {
            if (debugMessages)
            {
                Debug.Log(this.gameObject.name + "Detonated via ActivateOnStart");
            }
        }

        //Set explosion force for initiating object (THIS ONE)
        //The NPC, Explodable or Player is inside the explosion radius. The individual destroyable object will check to see if they are protected.

        GameObject.FindGameObjectWithTag("GameController")?.SendMessage("RootAlertNearby", transform.position, SendMessageOptions.DontRequireReceiver);

        //Should this gameobject be protected from the blast due to a wall or other blocking object
        if (IamProtected(origInfo.originator, origInfo.isGunShot) == false)
        {
            //Rifle bullet prefabs deliver stepped damage
            integrity -= origInfo.integrityHit;

            if (integrity <= 0)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, new Vector3(transform.position.x + (Random.value * .25f), transform.position.y, transform.position.z + (Random.value * .25f)), explosionRadius, Random.Range(2.0f, 2.5f));
                }

                hasExploded = true;

                //Check to see if an NPC, Explodable or the player inside the explosion radius
                Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
                if (debugMessages)
                {
                    Debug.Log("Number of items within the blast radius: " + colliders.Length.ToString());
                    foreach (var item in colliders)
                    {
                        Debug.Log("Item within the blast radius: " + item.name);
                    }
                }
                if (colliders.Length > 0)
                {
                    PHF_Detonator[] detonators;
                    PHF_Detonator_Reactor[] reactors;
                    detonators = (PHF_Detonator[])GameObject.FindObjectsOfType(typeof(PHF_Detonator));
                    reactors = (PHF_Detonator_Reactor[])GameObject.FindObjectsOfType(typeof(PHF_Detonator_Reactor));

                    if (debugMessages)
                    {
                        Debug.Log("Number of items with a PHF_Detonator component: " + detonators.Length.ToString());
                        Debug.Log("Number of items with a PHF_Detonator_Reactor component: " + reactors.Length.ToString());
                    }

                    if (detonators.Length > 1 || reactors.Length > 0)
                    {
                        foreach (var colHit in colliders)
                        {
                            if (useEnemyAI)
                            {
                                ////Important: ROOT cannot be an empty game object in the scene hierarchy where like-items are grouped or this won't work.
                                //if (colHit.transform.root.GetComponent<EnemyAI.PHF_EnemyHealth>() || colHit.GetComponent<PHF_Detonator>() || colHit.GetComponent<PHF_Detonator_Reactor>() || colHit.transform.root.GetComponent<PHF_PlayerHealth>())
                                //{
                                //    if (debugMessages)
                                //    {
                                //        Debug.Log("There are other objects in the blast radius that can detonate or react." + this.gameObject.name);
                                //    }
                                //    SendMessages(colHit, this.gameObject);
                                //}
                            }
                            else if (affectPlayer)
                            {
                                //    //Important: ROOT cannot be an empty game object in the scene hierarchy where like-items are grouped or this won't work.
                                //    if (colHit.GetComponent<PHF_Detonator>() || colHit.GetComponent<PHF_Detonator_Reactor>() || colHit.transform.root.GetComponent<PHF_PlayerHealth>())
                                //    {
                                //        if (debugMessages)
                                //        {
                                //            Debug.Log("There are other objects in the blast radius that can detonate or react." + this.gameObject.name);
                                //        }
                                //        SendMessages(colHit, this.gameObject);
                                //    }
                            }
                            else
                            {
                                                                   //Important: ROOT cannot be an empty game object in the scene hierarchy where like-items are grouped or this won't work.
                                   if (colHit.GetComponent<PHF_Detonator_Reactor>())
                                   {
                                       if (debugMessages)
                                       {
                                           Debug.Log("There are other objects in the blast radius that can detonate or react." + this.gameObject.name);
                                       }
                                       SendMessages(colHit, this.gameObject);
                                   }
                            }
                        }
                    }
                    else
                    {
                        if (debugMessages)
                        {
                            Debug.Log("There are no other effected objects in the blast radius. " + this.gameObject.name);
                            foreach (var item in colliders)
                            {
                                Debug.Log("Debug_A: This item is in the blast radius with colliders: " + item.name);
                            }
                        }
                    }
                }
                else
                {
                    //This is close to impossible because there will ALWAYS be anotehr collider in the scene near the exploding object.
                    if (debugMessages)
                    {
                        Debug.Log("There are no other effected objects in the blast radius." + this.gameObject.name);
                        foreach (var item in colliders)
                        {
                            Debug.Log("Debug_A: This item is in the blast radius with colliders: " + item.name);
                        }
                    }
                }


                // Show shatter objects (if any)
                if (pieces.Length > 0)
                {
                    for (int i = 0; i < pieces.Length; i++)
                    {
                        Instantiate(pieces[i], transform.position, transform.rotation);
                    }
                }

                // Show particle VFX (if any)
                if (ParticleFXPrefab)
                {
                    GameObject expFX = Instantiate(ParticleFXPrefab, transform.position, transform.rotation);
                }

                // Play explosion audio (if any)
                if (explosionAudio)
                {
                    AudioSource.PlayClipAtPoint(explosionAudio, transform.position, 1.0f);
                }


                if (!XRPoserGrenade) ////////////////// HACK TO WORK WITH XRPoser /////////////////////////
                {
                    transform.gameObject.GetComponent<Renderer>().enabled = false; 
                }

                // Time to go!
                StartCoroutine(CleanUpTheScene());
            }
        }
    }

    private IEnumerator CleanUpTheScene()
    {
        yield return new WaitForSeconds(SelfDestructDelay);
        Destroy(this.gameObject);
        //if (CheckRemainingItems() <= 1)
        //{
            
        //}
        //else
        //{
        //    Debug.LogError("Increase the DelayBeforeSelfDestruct time on: " + this.gameObject.name);
        //}

    }

    int totalItems;
    private int CheckRemainingItems()
    {
        PHF_Detonator[] detonators;
        PHF_Detonator_Reactor[] reactors;

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        if (colliders.Length > 0)
        {
            detonators = (PHF_Detonator[])GameObject.FindObjectsOfType(typeof(PHF_Detonator));
            reactors = (PHF_Detonator_Reactor[])GameObject.FindObjectsOfType(typeof(PHF_Detonator_Reactor));
            totalItems = detonators.Length + reactors.Length;
        }
        return totalItems;

    }

    private void SendMessages(Collider hit, GameObject _originator)
    {
        //Send Messages
        //If an ememy, send kill damage. Hardcode bulletDamage to 5000 to ensure the kill
        //hit.SendMessageUpwards("HitCallback", new HealthManager.DamageInfo(transform.position, transform.forward, 5000, hit, null, false), SendMessageOptions.DontRequireReceiver);
        GameObject.FindGameObjectWithTag("GameController")?.SendMessage("RootAlertNearby", transform.position, SendMessageOptions.DontRequireReceiver);

        //If an explodable, send Detonate message
        hit.SendMessageUpwards("StartDetonator", new OriginatorInfo(1000, _originator, false), SendMessageOptions.DontRequireReceiver);
        //If not an explodable, but has a collider and rigidbody, send React message
        hit.SendMessageUpwards("StartReactor", new OriginatorInfo(1000, _originator, false), SendMessageOptions.DontRequireReceiver);

        //If player or Enemy receiving BLAST damage
        hit.SendMessageUpwards("ReceiveDamage", new OriginatorInfo(1000, _originator, false), SendMessageOptions.DontRequireReceiver);

        if (debugMessages)
        {
            Debug.Log("VIA AREA_DAMAGE: Sent message to Detonate or React to: " + hit.transform.gameObject.name);
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
        else if (originator?.layer == LayerMask.NameToLayer("Player"))
        {
            var heading = XRRigAimTarget.transform.position - transform.GetComponent<Renderer>().bounds.center;
            var distance = heading.magnitude;
            var direction = heading / distance; // This is now the normalized direction.
            return direction;
        }
        else if (originator?.layer == LayerMask.NameToLayer("Enemy"))
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

    private bool IamProtected(GameObject originator, bool isGunShot)
    {
        if (XRPoserGrenade)
        {
            return false;
        }

        if (isGunShot) ////////////////// HACK TO WORK WITH XRPoser Rifle Bullet /////////////////////////
        {
            return false;
        }

        if (debugMessages)
        {
            Debug.Log("IamProtected 01: Originator Name: " + originator.name + " My name: " + this.gameObject.name);
        }
        
        Vector3 rayOrigin = transform.GetComponent<Renderer>().bounds.center;
        ray = new Ray(rayOrigin, GetTargetDirection(originator));

        if (this.gameObject.name != originator.name) //Is this me or someone else?
        {
            // It's not me.
            if (Physics.Raycast(ray, out theRayhit, Mathf.Infinity)) // If a raycast hits SOMETHING with a collider
            {
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

            //It's me,  and I WANT to explode because I'm the originator
            return false;
        }
    } // End IamProtected

    void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere at the transform's position
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}

// Class to encapsulate damage parameters for the callback function.
public class OriginatorInfo
{
    public int integrityHit;
    public GameObject originator;
    public bool isGunShot;


    public OriginatorInfo(int integrityHit, GameObject originator, bool isGunShot)
    {
        this.integrityHit = integrityHit;
        this.originator = originator;
        this.isGunShot = isGunShot;
    }
}
