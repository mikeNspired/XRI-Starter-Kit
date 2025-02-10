// Author MikeNspired. 

using System.Collections.Generic;
using UnityEngine;

namespace MikeNspired.XRIStarterKit
{
    public class DistanceGrabberLineBender : MonoBehaviour
    {
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private int vertexCount = 30;
        private float vertCount = 30;
        private Transform Target;
        private float projectionDistance;

        void Start()
        {
            //Adding .01f brings the line to the position of the target
            vertCount = vertexCount + .01f;

            OnValidate();
            largeParticleArray = new ParticleSystem.Particle[largeParticles.main.maxParticles];
            smallParticlesArray = new ParticleSystem.Particle[particleSystem.main.maxParticles];
        }

        private void OnValidate()
        {
            if (!lineRenderer)
                lineRenderer = GetComponent<LineRenderer>();
        }

        void Update()
        {
            UpdateLinerRenderer();
            UpdateParticles();
        }

        public void Start(Transform target)
        {
            Target = target;
        }

        public void Stop()
        {
            Target = null;
        }

        private void UpdateLinerRenderer()
        {
            if (!Target)
            {
                lineRenderer.enabled = false;
                return;
            }

            lineRenderer.enabled = true;

            //Get direction from item to transform, for plane projection
            Vector3 itemNormalVector = transform.position - Target.position;

            //Create a point to project onto the plane 
            Vector3 positionToProject = transform.position + transform.forward * .4f;

            Vector3 v = positionToProject - Target.position;
            Vector3 projection = Vector3.Project(v, itemNormalVector.normalized);

            //Position projection in the space of the item
            Vector3 projectedPoint = positionToProject - projection;

            var lineBendGoal = projectedPoint;
            var pointList = new List<Vector3>();

            //Create curved line
            for (float ratio = 0; ratio <= 1; ratio += 1 / vertCount)
            {
                var tangent1 = Vector3.Lerp(transform.position, lineBendGoal, ratio);
                var tangent2 = Vector3.Lerp(lineBendGoal, Target.position, ratio);
                var curve = Vector3.Lerp(tangent1, tangent2, ratio);

                pointList.Add(curve);
            }

            lineRenderer.positionCount = pointList.Count;
            lineRenderer.SetPositions(pointList.ToArray());

            projectionDistance = Vector3.Distance(projectedPoint, Target.position);
        }


        [Header("Particle Settings")] [SerializeField]
        private new ParticleSystem particleSystem = null;

        [SerializeField] private ParticleSystem largeParticles = null;
        [SerializeField] private float smallAttractSpeed = 2f, largeAttractSpeed = .5f;
        [SerializeField] private float minSmallParticleOutput = 100;
        [SerializeField] private float maxSmallParticleOutput = 1000;

        [Tooltip("How far projection is before reaching max Particle output")] [SerializeField]
        private float projectionDistanceMax = .7f;

        [SerializeField] private int particleMoveToCount = 8;
        private Vector3[] particleMoveToTargets;

        [Tooltip("What line position small emitter rotates to look at")] [SerializeField]
        private int linePositionParticleLookAt = 25;
        private ParticleSystem.Particle[] smallParticlesArray,largeParticleArray;

        private void UpdateParticles()
        {
            if (!Target)
            {
                particleSystem.gameObject.SetActive(false);
                largeParticles.gameObject.SetActive(false);
                return;
            }

            particleSystem.gameObject.SetActive(true);
            largeParticles.gameObject.SetActive(true);
            if (!particleSystem.isPlaying)
            {
                particleSystem.Play();
                largeParticles.Play();
            }

            particleSystem.transform.position = Target.position;
            largeParticles.transform.position = Target.position;

            UpdateLargeParticles();
            UpdateSmallParticles();
        }

        private void UpdateSmallParticles()
        {
            particleSystem.transform.LookAt(lineRenderer.GetPosition(linePositionParticleLookAt));

            int totalParticles = particleSystem.GetParticles(smallParticlesArray);

            float startLife = particleSystem.main.startLifetime.constant;
            var emission = particleSystem.emission;

            //Emit more particles when bending line
            emission.rateOverTime = Mathf.Lerp(minSmallParticleOutput, maxSmallParticleOutput, Mathf.Clamp(projectionDistance, 0, projectionDistanceMax) / projectionDistanceMax);

            particleMoveToTargets = new Vector3[particleMoveToCount];

            //Update target points from line Renderer
            for (int i = 0; i < particleMoveToCount; i++)
                particleMoveToTargets[i] = (lineRenderer.GetPosition(lineRenderer.positionCount - (i + 2)));

            for (int i = 0; i < totalParticles; i++)
            {
                for (int j = 1; j <= particleMoveToCount; j++)
                {
                    if (smallParticlesArray[i].remainingLifetime > startLife - (startLife / particleMoveToCount * j))
                    {
                        smallParticlesArray[i].position = Vector3.Lerp(smallParticlesArray[i].position, particleMoveToTargets[j - 1], smallAttractSpeed * Time.deltaTime);
                        break;
                    }
                }
            }

            particleSystem.SetParticles(smallParticlesArray, totalParticles);
        }

        private void UpdateLargeParticles()
        {
            int totalParticles = largeParticles.GetParticles(largeParticleArray);

            for (int i = 0; i < totalParticles; i++)
            {
                largeParticleArray[i].position = Vector3.Lerp(largeParticleArray[i].position, transform.position, largeAttractSpeed * Time.deltaTime);
            }

            largeParticles.SetParticles(largeParticleArray, totalParticles);
        }
    }
}