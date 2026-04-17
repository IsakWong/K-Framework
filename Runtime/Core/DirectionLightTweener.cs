using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework.Core
{
    [RequireComponent(typeof(Light))]
    public class DirectionLightTweener : MonoBehaviour
    {
        public Collider Volume;

        private Light Light;

        private List<Light> lights = new();

        private void OnDestroy()
        {
            lights.Remove(GetComponent<Light>());
        }

        private void FixedUpdate()
        {
            if (Camera.main)
            {
                var pos = Camera.main.transform.position;
                if (Vector3.Distance(pos, transform.position) < 250)
                {
                    gameObject.SetActive(true);
                }
            }
            else
            {
            }
        }

        private void Awake()
        {
            if (lights.Count != 0)
            {
                gameObject.SetActive(false);
            }

            lights.Add(GetComponent<Light>());
        }
    }
}