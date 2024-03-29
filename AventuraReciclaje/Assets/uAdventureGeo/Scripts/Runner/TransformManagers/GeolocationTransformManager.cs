﻿using AssetPackage;
using MapzenGo.Helpers;
using System.Collections.Generic;
using uAdventure.Runner;
using UnityEngine;

namespace uAdventure.Geo
{
    public class GeolocationTransformManager : ITransformManager
    {
        private static GameObject particleSystemPrefab;

        public Transform ExtElemReferenceTransform
        {
            get
            {
                return transform;
            }
            set
            {
                if (!particleSystemPrefab)
                {
                    particleSystemPrefab = Resources.Load<GameObject>("GeoParticles");
                }

                transform = value;
                collider = transform.gameObject.GetComponent<Collider>();
                renderer = transform.gameObject.GetComponent<Renderer>();
                positioner = transform.gameObject.GetComponent<GeoPositioner>();

                var particlesGo = Object.Instantiate(particleSystemPrefab,transform);
                particles = particlesGo.GetComponent<ParticleSystem>();
                material = particles.GetComponent<ParticleSystemRenderer>().material;

                character = GameObject.FindObjectOfType<GeoPositionedCharacter>();
                interactuable = transform.GetComponent<Interactuable>() ?? transform.GetComponentInChildren<Interactuable>();

                representable = transform.gameObject.GetComponent<Representable>();
                if (representable != null)
                {
                    representable.RepresentableChanged += Adapted;
                    Adapted();
                }
            }
        }

        public ExtElemReference Context { get; set; }

        private GeoPositionedCharacter character;
        private Vector2d latLon;
        private float rotation;
        private float interactionRange;
        private bool revealOnRange;
        private GeoPositioner positioner;
        private Material material;
        private Texture2D particleTexture;
        private Transform transform;
        private Interactuable interactuable;
        private Collider collider;
        private Renderer renderer;
        private ParticleSystem particles;
        private Representable representable;
        private bool shown = true;

        public void Configure(Dictionary<string, object> parameters)
        {
            latLon = (Vector2d)parameters["Position"];
            rotation = (float)parameters["Rotation"];
            interactionRange = (float)parameters["InteractionRange"];
            revealOnRange = (bool)parameters["RevealOnlyOnRange"];
            particleTexture = Game.Instance.ResourceManager.getImage(parameters["RevealParticleTexture"] as string);
        }


        public void Update()
        {
            if (material && material.mainTexture != particleTexture)
            {
                material.mainTexture = particleTexture;
            }

            var pos = GM.LatLonToMeters(latLon) - positioner.Tile.Rect.Center;
            var basePosition = new Vector3((float) pos.x, 10, (float) pos.y);
            var centerVector = new Vector3(0, 0, transform.localScale.y/2f);
            transform.localPosition = basePosition + centerVector;
            transform.localRotation = Quaternion.Euler(90, rotation, 0);

            if (interactionRange <= 0 || GM.SeparationInMeters(character.LatLon, latLon) <= interactionRange)
            {
                SetShown(true);
                if (interactuable != null)
                {
                    interactuable.setInteractuable(true);
                }
            }
            else if (shown)
            {
                SetShown(false);
                if (interactuable != null)
                {
                    interactuable.setInteractuable(true);
                }
            }
        }

        private void Adapted()
        {
            // Position
            var metersSizeAt0 = representable.Size / (float) GM.GetPixelsPerMeter(0, 19);
            var pixelScaleAt = (float) GM.GetPixelsPerMeter(latLon.x, 19) / (float)GM.GetPixelsPerMeter(0, 19);
            transform.localScale = new Vector3(metersSizeAt0.x * pixelScaleAt, metersSizeAt0.y * pixelScaleAt, 1);

        }

        private void SetShown(bool shown)
        {
            if (this.shown != shown)
            {
                this.shown = shown;
                if (shown)
                {
                    // TODO change this after: https://github.com/e-ucm/unity-tracker/issues/29
                    TrackerAsset.Instance.setVar("geo_element_" + positioner.Element.getId(), 1);
                }
                if (revealOnRange)
                {
                    collider.enabled = shown;
                    renderer.enabled = shown;
                    SetParticles(shown);
                }
            }
        }

        private void SetParticles(bool enabled)
        {
            if (enabled)
            {
                particles.gameObject.SetActive(true);
                particles.Play();
            }
            else
            {
                particles.gameObject.SetActive(false);
                particles.time = 0;
                particles.Stop();
            }
        }
    }
}
