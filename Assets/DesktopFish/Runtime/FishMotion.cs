using System.Collections.Generic;
using UnityEngine;

namespace DesktopFish
{
    // Procedural 3D gestures inspired by dsh-pet's idle/action/idle timing.
    // Each pose is evaluated from rest, so gestures blend back without accumulating rotations.
    public sealed class FishMotion
    {
        public enum Gesture { None, Wave, Bow, Doze, Step, Cheer }
        public const float WaveDuration = 5.2f;
        static readonly string[] FingerNames = { "Index", "Middle", "Ring", "Little" };
        readonly Transform model;
        readonly Vector3 restPosition, restScale;
        readonly Vector3 waveWristOffset;
        readonly bool rebuiltRig, articulatedHands;
        readonly Quaternion restRotation;
        readonly Dictionary<string, Transform> bones = new Dictionary<string, Transform>();
        readonly Dictionary<string, Quaternion> rest = new Dictionary<string, Quaternion>();
        readonly Dictionary<string, Quaternion> worldRest = new Dictionary<string, Quaternion>();
        readonly Dictionary<string, Vector3> restPositions = new Dictionary<string, Vector3>();
        Gesture gesture;
        float started, duration, nextAction, bounceStart = -10;
        float lookX, lookY;
        float workBlend;
        public float WorkWeight => workBlend;
        int previousRandom = -1;
        public bool randomActions = true;
        public float ClosedEyes { get; private set; }
        public float SmileEyes { get; private set; }
        public float BlinkWeight(float blink = 0) => Mathf.Max(ClosedEyes, blink) * (1 - SmileEyes * .01f);
        public float Joy { get; private set; }

        public FishMotion(Transform target)
        {
            model = target;
            restPosition = target.localPosition;
            restScale = target.localScale;
            restRotation = target.localRotation;
            var profile = target.GetComponent<FishRigProfile>();
            rebuiltRig = profile;
            articulatedHands = profile && profile.articulatedHandRig;
            waveWristOffset = profile ? profile.waveWristOffset : new Vector3(.110f, .085f, .075f);
            foreach (Transform bone in target.GetComponentsInChildren<Transform>(true))
            {
                if (bones.ContainsKey(bone.name)) continue;
                bones.Add(bone.name, bone);
                rest.Add(bone.name, bone.localRotation);
                worldRest.Add(bone.name, Quaternion.Inverse(target.rotation) * bone.rotation);
                restPositions.Add(bone.name, bone.localPosition);
            }
            if (articulatedHands) {
                // Keep bind frames neutral while evaluating the same arm start
                // frame used by the registered reference default-pose solver.
                rest["J_Bip_L_UpperArm"] *= Quaternion.Euler(0,0,62);
                rest["J_Bip_R_UpperArm"] *= Quaternion.Euler(0,0,-62);
            }
            nextAction = Time.time + 8;
        }

        public void Play(Gesture value, float now)
        {
            gesture = value;
            started = now;
            duration = value == Gesture.Wave ? WaveDuration : value == Gesture.Bow ? 4.2f :
                value == Gesture.Doze ? 7 : value == Gesture.Step ? 4 : 2.4f;
            nextAction = now + duration + UnityEngine.Random.Range(8, 15);
        }

        public void Click(float now)
        {
            bounceStart = now;
            Play(Gesture.Cheer, now);
        }

        public void Tick(float now, float dt, float pointerX, float pointerY, string state, bool dragging)
        {
            if (dragging)
            {
                gesture = Gesture.None;
                nextAction = now + 10;
            }
            else if (gesture != Gesture.None && now - started >= duration) gesture = Gesture.None;
            if (randomActions && gesture == Gesture.None && state == "idle" && now > nextAction)
            {
                int pick;
                do { pick = UnityEngine.Random.Range(0, 4); } while (pick == previousRandom);
                previousRandom = pick;
                Play((Gesture)(pick + 1), now);
            }
            float follow = 1 - Mathf.Exp(-5 * dt);
            lookX = Mathf.Lerp(lookX, pointerX, follow);
            lookY = Mathf.Lerp(lookY, pointerY, follow);
            workBlend = Mathf.Lerp(workBlend,
                state == "working" && gesture == Gesture.None && !dragging ? 1 : 0, follow);
            Evaluate(now, lookX, lookY, state, dragging);
        }

        public void Evaluate(float now, float pointerX, float pointerY, string state, bool dragging)
        {
            float elapsed = now - started;
            float weight = gesture == Gesture.None ? 0 :
                Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / 0.65f)) *
                Mathf.SmoothStep(0, 1, Mathf.Clamp01((duration - elapsed) / 0.65f));
            if (gesture == Gesture.Wave)
                weight = SmoothRange(0, 0.85f, elapsed) * (1 - SmoothRange(4.15f, 5.15f, elapsed));
            float wristEnvelope = SmoothRange(1.10f, 1.35f, elapsed) * (1 - SmoothRange(3.45f, 3.70f, elapsed));
            float wave = Mathf.Sin((elapsed - 1.15f) / 0.85f * Mathf.PI * 2) * wristEnvelope;
            float breath = Mathf.Sin(now * 1.7f);
            Vector3 head = new Vector3(-pointerY * 8, -pointerX * 12, Mathf.Sin(now) * 1.1f);
            Vector3 chest = new Vector3(0, 0, breath * 0.7f);
            Vector3 leftArm = new Vector3(0, 0, breath * 1.4f);
            Vector3 rightArm = new Vector3(0, 0, -breath * 1.4f);
            // Turn the palm plane toward the viewer instead of presenting its edge.
            Vector3 leftElbow = new Vector3(-55, 0, -12), rightElbow = new Vector3(-55, 0, 12);
            float leftLeg = 0, rightLeg = 0, leftKnee = 0, rightKnee = 0;
            float rootY = 0, lean = 0, squash = 1;
            ClosedEyes = 0;
            SmileEyes = 0;
            Joy = state == "complete" ? 65 : state == "idle" ? 8 : 0;
            // Keep the generator steady; the right hand follows its crank and
            // the left hand braces the housing instead of swaying independently.
            head = Vector3.Lerp(head, new Vector3(10 + Mathf.Sin(now * 4.4f) * 1.3f,
                -3, -1), workBlend);
            chest.x += 4 * workBlend;
            switch (gesture)
            {
                case Gesture.Wave:
                    head = Vector3.Lerp(head, new Vector3(0, 0, Mathf.Sin(elapsed * 1.3f) * 2.5f), weight * 0.9f);
                    Joy = Mathf.Lerp(Joy, 70, weight);
                    if (rebuiltRig)
                        SmileEyes = 100 * SmoothRange(.70f, 1.30f, elapsed) * (1 - SmoothRange(3.30f, 4.00f, elapsed));
                    break;
                case Gesture.Bow:
                    chest.x += 20 * weight;
                    head = Vector3.Lerp(head, new Vector3(14, 0, 0), weight);
                    leftArm.z -= 9 * weight;
                    rightArm.z += 9 * weight;
                    leftElbow.z -= 18 * weight;
                    rightElbow.z += 18 * weight;
                    leftLeg = rightLeg = -8 * weight;
                    leftKnee = rightKnee = 16 * weight;
                    rootY = -0.012f * weight;
                    ClosedEyes = 85 * weight;
                    break;
                case Gesture.Doze:
                    head = Vector3.Lerp(head, new Vector3(16 + Mathf.Sin(elapsed * 2.5f) * 3, 0, 7), weight);
                    chest.x += 5 * weight;
                    rootY = -0.006f * weight;
                    ClosedEyes = 100 * weight;
                    break;
                case Gesture.Step:
                    float step = Mathf.Sin(elapsed * 8) * weight;
                    leftLeg = step * 16;
                    rightLeg = -leftLeg;
                    leftKnee = Mathf.Max(0, -step) * 18;
                    rightKnee = Mathf.Max(0, step) * 18;
                    leftArm.x += -step * 12;
                    rightArm.x += step * 12;
                    rootY = Mathf.Abs(step) * 0.008f;
                    lean = step * 2;
                    Joy = 35 * weight;
                    break;
                case Gesture.Cheer:
                    float jump = elapsed > 0.25f && elapsed < 1.05f ?
                        Mathf.Sin((elapsed - 0.25f) / 0.8f * Mathf.PI) : 0;
                    rootY = jump * 0.045f;
                    leftArm += new Vector3(0, 55, -95) * weight;
                    rightArm += new Vector3(0, -55, 95) * weight;
                    leftElbow.z -= 25 * weight;
                    rightElbow.z += 25 * weight;
                    head.z = Mathf.Sin(elapsed * 5) * 4 * weight;
                    Joy = 90 * weight;
                    break;
            }
            // SmileEyes denotes total closure, including the closure in ALL_Joy.
            if(model.GetComponent<FishRigProfile>()?.referenceCoreRig==true)SmileEyes=Mathf.Max(SmileEyes,Joy);
            float bounce = now - bounceStart;
            if (bounce >= 0 && bounce < 0.65f)
                squash += -0.10f * Mathf.Sin(bounce / 0.65f * Mathf.PI * 3) * Mathf.Exp(-bounce * 5);
            if (dragging)
            {
                lean = Mathf.Sin(now * 4) * 3;
                head.z += 9;
                leftArm.z -= 10;
                rightArm.z += 10;
                leftLeg = 6; rightLeg = -6;
                rootY = 0.012f;
            }
            model.localPosition = restPosition + Vector3.up * rootY;
            model.localRotation = restRotation * Quaternion.Euler(0, 0, lean);
            model.localScale = Vector3.Scale(restScale, new Vector3(1 / Mathf.Sqrt(squash), squash, 1));
            Rotate("J_Bip_C_Head", head);
            Rotate("J_Bip_C_Chest", chest);
            LiftShoulder("L", gesture == Gesture.Wave ? weight : 0);
            LiftShoulder("R", gesture == Gesture.Wave ? weight : 0);
            Rotate("J_Bip_L_UpperArm", leftArm);
            Rotate("J_Bip_R_UpperArm", rightArm);
            Rotate("J_Bip_L_LowerArm", leftElbow);
            Rotate("J_Bip_R_LowerArm", rightElbow);
            Rotate("J_Bip_L_Hand", Vector3.zero);
            Rotate("J_Bip_R_Hand", Vector3.zero);
            float relaxed = gesture == Gesture.Cheer ? 1 - weight :
                gesture == Gesture.Step ? 1 - 0.65f * weight : 1;
            RestArm("L", -1, relaxed);
            RestArm("R", 1, relaxed);
            if (gesture == Gesture.Wave)
            {
                WaveArm("L", -1, 14 - wave * 16, weight);
                WaveArm("R", 1, -14 + wave * 16, weight);
            }
            else if (gesture == Gesture.Cheer)
            {
                if (rebuiltRig) {
                    WaveArm("L", -1, -32, weight);
                    WaveArm("R", 1, 32, weight);
                } else {
                    PresentPalm("L", 35, weight);
                    PresentPalm("R", -35, weight);
                }
            }
            if (rebuiltRig && !articulatedHands) {
                AlignForearmRoll("L", -1);
                AlignForearmRoll("R", 1);
            }
            float fingerRelax = gesture == Gesture.Wave || gesture == Gesture.Cheer ? 1 - weight : 1;
            if (gesture == Gesture.Wave) fingerRelax = 1 - 0.88f * weight;
            RelaxFingers("L", 1, fingerRelax);
            RelaxFingers("R", -1, fingerRelax);
            if (workBlend > .0001f)
            {
                WorkArm("R", 1, FishWorkStation.CrankGrip(now) + Vector3.up * .025f, workBlend);
                WorkArm("L", -1, FishWorkStation.BraceWrist, workBlend);
                GripFingers("R", -1, workBlend);
                GripFingers("L", 1, workBlend * .65f);
            }
            Rotate("J_Bip_L_UpperLeg", new Vector3(leftLeg, 0, 0));
            Rotate("J_Bip_R_UpperLeg", new Vector3(rightLeg, 0, 0));
            Rotate("J_Bip_L_LowerLeg", new Vector3(leftKnee, 0, 0));
            Rotate("J_Bip_R_LowerLeg", new Vector3(rightKnee, 0, 0));
            Rotate("J_Opt_C_FoxTail1_01", new Vector3(0, Mathf.Sin(now * 1.9f) * 5, 0));
            // Gentle authored sway, independent of the unavailable VRChat PhysBone runtime.
            for (int i = 1; i <= 5; i++)
            {
                Rotate("J_Sec_Hair" + i + "_01", new Vector3(Mathf.Sin(now * 1.5f + i * 0.5f) * 0.7f,
                    0, Mathf.Sin(now * 1.3f + i) * 0.8f));
                string name = "J_Sec_Hair" + i + "_01";
                if (!rebuiltRig && bones.TryGetValue(name, out Transform hair))
                    hair.rotation = Quaternion.Slerp(hair.rotation, model.rotation * worldRest[name], 0.55f);
            }
        }

        void Rotate(string name, Vector3 degrees)
        {
            if (bones.TryGetValue(name, out Transform bone)) bone.localRotation = rest[name] * Quaternion.Euler(degrees);
        }

        static float SmoothRange(float from, float to, float value) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value));

        void WorkArm(string side, float sign, Vector3 localTarget, float weight)
        {
            if (!bones.TryGetValue("J_Bip_" + side + "_UpperArm", out Transform upper) ||
                !bones.TryGetValue("J_Bip_" + side + "_LowerArm", out Transform elbow) ||
                !bones.TryGetValue("J_Bip_" + side + "_Hand", out Transform hand)) return;
            Quaternion baseUpper = upper.localRotation, baseElbow = elbow.localRotation, baseHand = hand.rotation;
            float a = Vector3.Distance(upper.position, elbow.position), b = Vector3.Distance(elbow.position, hand.position);
            Vector3 delta = model.TransformPoint(localTarget) - upper.position;
            float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, (a + b) * .995f);
            Vector3 direction = delta.normalized;
            Vector3 pole = Vector3.ProjectOnPlane(model.TransformDirection(new Vector3(sign, -.6f, .35f)), direction).normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 elbowTarget = upper.position + direction * along + pole * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(elbow.position - upper.position, elbowTarget - upper.position) * upper.rotation;
            elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position, upper.position + direction * distance - elbow.position) * elbow.rotation;
            Quaternion solvedUpper = upper.localRotation, solvedElbow = elbow.localRotation;
            upper.localRotation = Quaternion.Slerp(baseUpper, solvedUpper, weight);
            elbow.localRotation = Quaternion.Slerp(baseElbow, solvedElbow, weight);
            Quaternion palm = model.rotation * Quaternion.LookRotation(sign > 0 ? Vector3.left : Vector3.right, Vector3.back);
            hand.rotation = Quaternion.Slerp(baseHand, palm, weight);
        }

        void GripFingers(string side, float sign, float weight)
        {
            for (int i = 0; i < FingerNames.Length; i++)
            {
                string finger = FingerNames[i];
                for (int joint = 1; joint <= 3; joint++)
                {
                    string name = "J_Bip_" + side + "_" + finger + joint;
                    if (!bones.TryGetValue(name, out Transform bone)) continue;
                    Quaternion grip = rest[name] * Quaternion.Euler(0, 0, sign * (joint == 2 ? 62 : 40));
                    bone.localRotation = Quaternion.Slerp(bone.localRotation, grip, weight);
                }
            }
        }

        void LiftShoulder(string side, float weight)
        {
            string name = "J_Bip_" + side + "_Shoulder";
            if (!bones.TryGetValue(name, out Transform shoulder)) return;
            Vector3 shrug = model.TransformVector(new Vector3(0, 0.012f, 0.035f) * weight);
            shoulder.localPosition = restPositions[name] + shoulder.parent.InverseTransformVector(shrug);
        }

        void WaveArm(string side, float sign, float tilt, float weight)
        {
            if (!bones.TryGetValue("J_Bip_" + side + "_UpperArm", out Transform upper) ||
                !bones.TryGetValue("J_Bip_" + side + "_LowerArm", out Transform elbow) ||
                !bones.TryGetValue("J_Bip_" + side + "_Hand", out Transform hand)) return;
            Quaternion baseUpper = upper.localRotation, baseElbow = elbow.localRotation, baseHand = hand.rotation;
            float a = Vector3.Distance(upper.position, elbow.position);
            float b = Vector3.Distance(elbow.position, hand.position);
            // Fixed face-side targets and an outward/downward pole keep the elbows
            // bent. Wrist motion never drives the shoulder or stretches the rig.
            Vector3 offset = model.TransformVector(new Vector3(sign * waveWristOffset.x, waveWristOffset.y, waveWristOffset.z));
            float distance = Mathf.Clamp(offset.magnitude, Mathf.Abs(a - b) + 0.001f, (a + b) * 0.94f);
            Vector3 direction = offset.normalized;
            Vector3 target = upper.position + direction * distance;
            Vector3 pole = model.TransformDirection(new Vector3(sign * 0.55f, -0.70f, 0.85f));
            pole = (pole - direction * Vector3.Dot(pole, direction)).normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            float height = Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            Vector3 elbowTarget = upper.position + direction * along + pole * height;
            upper.rotation = Quaternion.FromToRotation(elbow.position - upper.position,
                elbowTarget - upper.position) * upper.rotation;
            elbow.rotation = Quaternion.FromToRotation(hand.position - elbow.position,
                target - elbow.position) * elbow.rotation;
            Quaternion raisedUpper = upper.localRotation, raisedElbow = elbow.localRotation;
            upper.localRotation = Quaternion.Slerp(baseUpper, raisedUpper, weight);
            elbow.localRotation = Quaternion.Slerp(baseElbow, raisedElbow, weight);
            hand.rotation = baseHand;
            PresentPalm(side, tilt, weight);
        }

        void AlignForearmRoll(string side, float sign)
        {
            if (!bones.TryGetValue("J_Bip_" + side + "_LowerArm", out Transform elbow) ||
                !bones.TryGetValue("J_Bip_" + side + "_Hand", out Transform hand)) return;
            // The IK solve fixes the forearm's direction but leaves its axial
            // roll unconstrained. Match the palm's roll before skinning the wrist.
            Quaternion palm = hand.rotation;
            Vector3 axis = sign * (hand.position - elbow.position).normalized;
            Vector3 up = Vector3.ProjectOnPlane(palm * Vector3.up, axis);
            if (up.sqrMagnitude < .0001f) return;
            up.Normalize();
            elbow.rotation = Quaternion.LookRotation(Vector3.Cross(axis, up), up);
            hand.rotation = palm;
        }

        void RelaxFingers(string side, float sign, float weight)
        {
            for (int i = 0; i < FingerNames.Length; i++)
            {
                string finger = FingerNames[i];
                // Short, rounded fingers fan out when presenting the rebuilt
                // palm, then close together again as the hand relaxes.
                float spread = rebuiltRig && !articulatedHands ? sign * (1.5f - i) * 12 * (1 - weight) : 0;
                Rotate("J_Bip_" + side + "_" + finger + "1", new Vector3(0, spread, sign * 24 * weight));
                Rotate("J_Bip_" + side + "_" + finger + "2", new Vector3(0, 0, sign * 34 * weight));
                Rotate("J_Bip_" + side + "_" + finger + "3", new Vector3(0, 0, sign * 18 * weight));
            }
        }

        void RestArm(string side, float sign, float weight)
        {
            if (!bones.TryGetValue("J_Bip_" + side + "_UpperArm", out Transform upper) ||
                !bones.TryGetValue("J_Bip_" + side + "_LowerArm", out Transform elbow) ||
                !bones.TryGetValue("J_Bip_" + side + "_Hand", out Transform hand)) return;
            Aim(upper, elbow, new Vector3(sign * 0.20f, -0.95f, 0.22f), weight);
            Aim(elbow, hand, new Vector3(-sign * 0.20f, 0.05f, 1), weight);
            Quaternion palm = Quaternion.LookRotation(side == "R" ? Vector3.left : Vector3.right, Vector3.forward);
            // Let the rebuilt palm lean forward at rest instead of folding
            // almost 90 degrees across the short, rounded wrist.
            if (rebuiltRig && !articulatedHands) palm = Quaternion.AngleAxis(-30, Vector3.right) * palm;
            hand.rotation = Quaternion.Slerp(hand.rotation, model.rotation * palm, weight);
        }

        void Aim(Transform bone, Transform child, Vector3 direction, float weight)
        {
            Quaternion target = Quaternion.FromToRotation(child.position - bone.position,
                model.TransformDirection(direction)) * bone.rotation;
            bone.rotation = Quaternion.Slerp(bone.rotation, target, weight);
        }

        void PresentPalm(string side, float tilt, float weight)
        {
            if (!bones.TryGetValue("J_Bip_" + side + "_Hand", out Transform hand)) return;
            // In the source T-pose the fingers follow +/-X and the palm faces -Y.
            // Present the palm to the front camera with fingers pointing upward.
            Quaternion palm = Quaternion.LookRotation(side == "R" ? Vector3.left : Vector3.right, Vector3.back);
            Quaternion target = model.rotation * Quaternion.AngleAxis(tilt, Vector3.forward) * palm;
            hand.rotation = Quaternion.Slerp(hand.rotation, target, weight);
        }
    }
}
