using UnityEngine;

namespace DesktopFish
{
    // Dimensions authored for the rebuilt rig, independent of the legacy avatar.
    public sealed class FishRigProfile : MonoBehaviour
    {
        // Complete calibrated wrist/metacarpal and three-joint digit frames.
        public bool articulatedHandRig;
        public Vector3 waveWristOffset = new Vector3(0.11f, 0.14f, 0.21f);
    }
}
