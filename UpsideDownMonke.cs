using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Reflection;
using GorillaLocomotion;

namespace UpsideDownMonke
{
    [BepInPlugin("org.auralius.gorillatag.upsidedown", "Upside Down Monke", "0.5.0.0")]
    [BepInProcess("Gorilla Tag.exe")]
    public class MonkePlugin : BaseUnityPlugin
    {
        private void Awake() => new Harmony("com.auralius.gorillatag.upsidedown").PatchAll(Assembly.GetExecutingAssembly());

        [HarmonyPatch(typeof(Player))]
        [HarmonyPatch("Update")]
        private class UpsideDownMonke_Patch
        {
            private static bool first;
            private static GameObject rightBall;
            private static GameObject leftBall;
            private static Vector3 rightOffset;
            private static Vector3 leftOffset;

            private static bool wait;
            private static bool invert;
            private static void Postfix(Player __instance)
            {
                if (!first)
                {
                    __instance.transform.rotation = Quaternion.Euler(0f, 0f, 180f); // Flip the player.

                    Physics.gravity *= -1f; // Flip gravity.

                    rightOffset = __instance.rightHandOffset; // Get controller offsets.
                    leftOffset = __instance.leftHandOffset;

                    rightBall = GameObject.CreatePrimitive(PrimitiveType.Sphere); // Create spheres.
                    leftBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    leftBall.transform.localScale = rightBall.transform.localScale = Vector3.one * __instance.minimumRaycastDistance; // Set scale to size of hands.

                    GameObject.Destroy(rightBall.GetComponent<Rigidbody>()); // Destroy unnecessary components.
                    GameObject.Destroy(rightBall.GetComponent<SphereCollider>());
                    GameObject.Destroy(leftBall.GetComponent<Rigidbody>());
                    GameObject.Destroy(leftBall.GetComponent<SphereCollider>());

                    first = true;
                }

                try { // Cycle through all VRRigs to find the ones belonging to you and disable rendering.
                    foreach (VRRig rig in Resources.FindObjectsOfTypeAll<VRRig>())
                        if (rig.photonView.IsMine) // || rig.isOfflineVRRig // Will add later.
                            rig.gameObject.transform.Find("gorilla").GetComponent<Renderer>().enabled = false;
                } catch { }

                Vector3 rightPos = __instance.rightHandTransform.position;
                Vector3 leftPos = __instance.leftHandTransform.position;

                Quaternion rightDir = __instance.rightHandTransform.rotation;
                Quaternion leftDir = __instance.leftHandTransform.rotation;

                rightBall.transform.position = rightPos + (rightDir * rightOffset);
                leftBall.transform.position = leftPos + (leftDir * leftOffset);

                if (__instance.transform.position.y > 40f && !wait) // If you fall 'below' a certain position then ACTIVATE!
                {
                    __instance.GetComponent<Rigidbody>().velocity *= -1f; // Invert velocity. This is what shoots you back to where you started.
                    if (__instance.GetComponent<Rigidbody>().velocity.magnitude < 15f) // If you're moving too slowly then invert gravity to help you get back to the living.
                    {
                        __instance.GetComponent<Rigidbody>().velocity *= 1.5f; // Just a small little boost.
                        Physics.gravity *= -1f; // Invertus.
                        invert = true;
                    }
                    wait = true;
                }
                else if (wait && ((__instance.IsHandTouching(true) || __instance.IsHandTouching(false)) && invert) || (!invert && __instance.transform.position.y < 40f)) // if wait and if any hands are touching while invert OR not invert and you are 'above' a certain position
                {
                    if (invert)
                    {
                        Physics.gravity *= -1f; // Uninvertus.
                        invert = false;
                    }
                    wait = false;
                }
            }
        }
    }
}