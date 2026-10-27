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
        readonly Dictionary<string,Vector3> workFeet=new Dictionary<string,Vector3>();
        readonly Dictionary<string,Vector3> soleInFoot=new Dictionary<string,Vector3>();
        readonly Dictionary<string,Vector3> gripInHand=new Dictionary<string,Vector3>();
        readonly Dictionary<string,Vector3> crankWrists=new Dictionary<string,Vector3>();
        readonly Dictionary<string,float> crankTwists=new Dictionary<string,float>();
        public Vector3 CrankWrist(string side)=>crankWrists[side];
        public float CrankTwist(string side)=>crankTwists[side];
        Gesture gesture;
        float started, duration, nextAction, bounceStart = -10;
        float lookX, lookY;
        float workBlend;
        public float WorkWeight => workBlend;
        public const float WorkingYaw=-35;
        public static float WorkingYawAt(float time)=>WorkingYaw+5*Mathf.Sin(FishWorkStation.CrankAngle(time));
        public static readonly Vector3 WorkingOffset=new Vector3(-.065f,0,-.025f);
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
            // Calibrate each closed hand independently in its own mirrored
            // bind frame, then restore every joint before normal evaluation.
            foreach(string side in new[]{"L","R"}) {
                float sign=side=="L"?1:-1;
                var palm=bones["J_Bip_"+side+"_Hand"];var m1=bones["J_Bip_"+side+"_Middle1"];var m2=bones["J_Bip_"+side+"_Middle2"];var m3=bones["J_Bip_"+side+"_Middle3"];
                var saved=new[]{m1.localRotation,m2.localRotation,m3.localRotation};
                try {
                    m1.localRotation=rest[m1.name]*Quaternion.Euler(0,0,sign*40);
                    m2.localRotation=rest[m2.name]*Quaternion.Euler(0,0,sign*54);
                    m3.localRotation=rest[m3.name]*Quaternion.Euler(0,0,sign*40);
                    Vector3 a=palm.InverseTransformPoint(m1.position),ab=palm.InverseTransformPoint(m2.position)-a,ac=palm.InverseTransformPoint(m3.position)-a,n=Vector3.Cross(ab,ac);
                    if(n.sqrMagnitude<1e-14f)throw new System.InvalidOperationException("Singular own grip calibration");
                    gripInHand[side]=a+(Vector3.Cross(n,ab)*ac.sqrMagnitude+Vector3.Cross(ac,n)*ab.sqrMagnitude)/(2*n.sqrMagnitude);
                }finally{m1.localRotation=saved[0];m2.localRotation=saved[1];m3.localRotation=saved[2];}
            }
            // Use an actual own rigid sole vertex as the support point. This
            // lets the heel rise with the shin without separating sock and shoe.
            foreach(string side in new[]{"L","R"})if(bones.TryGetValue("J_Bip_"+side+"_Foot",out var foot)) {
                Vector3 contact=model.InverseTransformPoint(foot.position);float floor=float.PositiveInfinity,front=float.NegativeInfinity;
                foreach(var skin in target.GetComponentsInChildren<SkinnedMeshRenderer>()) {
                    int owner=System.Array.IndexOf(skin.bones,foot);if(owner<0)continue;
                    var positions=skin.sharedMesh.vertices;var weights=skin.sharedMesh.boneWeights;
                    for(int i=0;i<positions.Length;i++)if(weights[i].boneIndex0==owner&&weights[i].weight0>.99999f) {
                        Vector3 q=model.InverseTransformPoint(skin.transform.TransformPoint(positions[i]));
                        if(q.y<floor-.00001f){floor=q.y;front=q.z;contact=q;}
                        else if(Mathf.Abs(q.y-floor)<.00001f&&q.z>front){front=q.z;contact=q;}
                    }
                }
                workFeet[side]=contact;soleInFoot[side]=foot.InverseTransformPoint(model.TransformPoint(contact));
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
            Vector3 chest = new Vector3(0, 0, breath * 0.7f*(1-workBlend));
            Vector3 leftArm = new Vector3(0, 0, breath * 1.4f*(1-workBlend));
            Vector3 rightArm = new Vector3(0, 0, -breath * 1.4f*(1-workBlend));
            // Turn the palm plane toward the viewer instead of presenting its edge.
            Vector3 leftElbow = new Vector3(-55, 0, -12), rightElbow = new Vector3(-55, 0, 12);
            float leftLeg = 0, rightLeg = 0, leftKnee = 0, rightKnee = 0;
            float rootY = 0, lean = 0, squash = 1;
            ClosedEyes = 0;
            SmileEyes = 0;
            Joy = state == "complete" ? 65 : state == "idle" ? 8 : 0;
            // One shared crank phase drives the whole effort: sit into bent
            // knees, press through the waist, twist the shoulders, recover.
            float crankPhase=FishWorkStation.CrankAngle(now);
            float drive=Mathf.Sin(crankPhase),compression=(1-Mathf.Cos(crankPhase))*.5f;
            head=Vector3.Lerp(head,new Vector3(-4+10*Mathf.Sin(crankPhase-.65f)-40*Mathf.Max(0,drive),
                -7+4*Mathf.Cos(crankPhase),-4*drive),workBlend);
            chest+=new Vector3(9+25*drive,12*Mathf.Cos(crankPhase),3*drive)*workBlend;
            // Follow the added part of the physical crank orbit with the
            // hips; lowering only the hands would overextend the arms.
            float orbitGrowth=FishWorkStation.CrankRadius-.085f;
            float pivotShift=FishWorkStation.CrankCenter.y-.457f;
            rootY+=(-.008f-.032f*compression+pivotShift+orbitGrowth*Mathf.Cos(crankPhase))*workBlend;
            lean+=3*drive*workBlend;
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
            Vector3 rock=Quaternion.Euler(0,WorkingYaw,0)*Vector3.forward*(.035f*drive*workBlend);
            rock+=FishWorkStation.GeneratorRotation*Vector3.forward*(orbitGrowth*drive*workBlend);
            Vector3 sideLoad=Vector3.right*(.010f*(1-drive)*workBlend);
            model.localPosition=restPosition+Vector3.up*rootY+WorkingOffset*workBlend+rock+sideLoad;
            model.localRotation = restRotation * Quaternion.Euler(0, WorkingYawAt(now) * workBlend, lean);
            model.localScale = Vector3.Scale(restScale, new Vector3(1 / Mathf.Sqrt(squash), squash, 1));
            Rotate("J_Bip_C_Hips",new Vector3(0,-6*drive,0)*workBlend);
            Rotate("J_Bip_C_Spine",new Vector3(3+5*drive,-6*Mathf.Cos(crankPhase),0)*workBlend);
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
            foreach(string side in new[]{"L","R"}) {
                float sign=side=="L"?1:-1;
                for(int j=1;j<=3;j++)Rotate("J_Bip_"+side+"_Thumb"+j,Vector3.zero);
                if(workBlend<=.0001f)continue;
                GripFingers(side,sign,workBlend);
                SolveCrankArm(side,now,workBlend);
                for(int j=1;j<=3;j++) {
                    string n="J_Bip_"+side+"_Thumb"+j;
                    Quaternion closed=rest[n]*Quaternion.Euler(0,sign*(j==1?-55:j==2?-18:-10),sign*(j==1?-12:0));
                    bones[n].localRotation=Quaternion.Slerp(rest[n],closed,workBlend);
                }
            }
            Rotate("J_Bip_L_UpperLeg", new Vector3(leftLeg, 0, 0));
            Rotate("J_Bip_R_UpperLeg", new Vector3(rightLeg, 0, 0));
            Rotate("J_Bip_L_LowerLeg", new Vector3(leftKnee, 0, 0));
            Rotate("J_Bip_R_LowerLeg", new Vector3(rightKnee, 0, 0));
            Rotate("J_Bip_L_Foot",Vector3.zero);Rotate("J_Bip_R_Foot",Vector3.zero);
            if(workBlend>.0001f){WorkLeg("L",workBlend);WorkLeg("R",workBlend);}
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

        Vector3 WorkFramePoint(Vector3 point) {
            Vector3 p=restPosition+restRotation*Vector3.Scale(restScale,point);
            return model.parent?model.parent.TransformPoint(p):p;
        }

        public Vector3 PlantedFootPosition(string side) {
            float sign=side=="L"?-1:1;
            return WorkFramePoint(WorkingOffset+Quaternion.Euler(0,WorkingYaw,0)*
                (workFeet[side]+new Vector3(sign*.012f,0,0)));
        }

        public Vector3 SoleContactPosition(string side)=>bones["J_Bip_"+side+"_Foot"].TransformPoint(soleInFoot[side]);

        void WorkLeg(string side,float weight) {
            if(!bones.TryGetValue("J_Bip_"+side+"_UpperLeg",out var hip)||
                !bones.TryGetValue("J_Bip_"+side+"_LowerLeg",out var knee)||
                !bones.TryGetValue("J_Bip_"+side+"_Foot",out var foot))return;
            Quaternion baseHip=hip.localRotation,baseKnee=knee.localRotation;
            Vector3 sole=foot.TransformPoint(soleInFoot[side]);
            float a=Vector3.Distance(hip.position,knee.position),b=Vector3.Distance(knee.position,sole);
            Vector3 target=PlantedFootPosition(side),delta=target-hip.position;
            float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.9999f);
            Vector3 direction=delta.normalized;
            Vector3 forward=restRotation*Quaternion.Euler(0,WorkingYaw,0)*Vector3.forward;
            if(model.parent)forward=model.parent.TransformDirection(forward);
            Vector3 pole=Vector3.ProjectOnPlane(forward,direction).normalized;
            float along=(a*a-b*b+length*length)/(2*length);
            Vector3 kneeTarget=hip.position+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            hip.rotation=Quaternion.FromToRotation(knee.position-hip.position,kneeTarget-hip.position)*hip.rotation;
            knee.rotation=Quaternion.FromToRotation(foot.TransformPoint(soleInFoot[side])-knee.position,hip.position+direction*length-knee.position)*knee.rotation;
            Quaternion solvedHip=hip.localRotation,solvedKnee=knee.localRotation;
            hip.localRotation=Quaternion.Slerp(baseHip,solvedHip,weight);knee.localRotation=Quaternion.Slerp(baseKnee,solvedKnee,weight);
            // Foot local rest frame follows the calf; the selected rigid sole
            // point stays planted while the heel pumps with the effort.
        }

        void Rotate(string name, Vector3 degrees)
        {
            if (bones.TryGetValue(name, out Transform bone)) bone.localRotation = rest[name] * Quaternion.Euler(degrees);
        }

        static float SmoothRange(float from, float to, float value) =>
            Mathf.SmoothStep(0, 1, Mathf.InverseLerp(from, to, value));

        // Solve the actual closed-finger grip center, rather than placing the
        // wrist at an arbitrary offset. The freely turning cylindrical grip
        // lets the palm follow the forearm while staying on the shaft axis.
        void SolveCrankArm(string side,float time,float weight) {
            var upper=bones["J_Bip_"+side+"_UpperArm"];var elbow=bones["J_Bip_"+side+"_LowerArm"];var hand=bones["J_Bip_"+side+"_Hand"];
            Quaternion baseUpper=upper.localRotation,baseElbow=elbow.localRotation,baseHand=hand.rotation;
            float a=Vector3.Distance(upper.position,elbow.position),b=Vector3.Distance(elbow.position,hand.position);
            Vector3 grip=WorkFramePoint(FishWorkStation.HandGrip(time,side));
            Vector3 shaft=(WorkFramePoint(FishWorkStation.HandGrip(time,side)+FishWorkStation.GeneratorRotation*Vector3.right)-grip).normalized;
            Vector3 caseForward=(WorkFramePoint(FishWorkStation.GeneratorRotation*Vector3.forward)-WorkFramePoint(Vector3.zero)).normalized;
            Quaternion palm=baseHand;
            for(int pass=0;pass<4;pass++) {
                Vector3 reach=grip-(pass==0?upper.position:elbow.position);
                Vector3 radial=Vector3.ProjectOnPlane(reach,shaft).normalized;
                if(radial.sqrMagnitude<.5f)radial=Vector3.ProjectOnPlane(Vector3.down,shaft).normalized;
                palm=Quaternion.LookRotation(shaft*(side=="L"?-1:1),Vector3.Cross(shaft,radial));
                crankWrists[side]=grip-palm*gripInHand[side];
                Vector3 delta=crankWrists[side]-upper.position;
                float length=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.995f);
                Vector3 direction=delta.normalized;
                Vector3 pole=Vector3.ProjectOnPlane(Vector3.down*.8f-caseForward*.6f+shaft*.15f,direction).normalized;
                float along=(a*a-b*b+length*length)/(2*length);
                Vector3 bend=upper.position+direction*along+pole*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
                upper.rotation=Quaternion.FromToRotation(elbow.position-upper.position,bend-upper.position)*upper.rotation;
                elbow.rotation=Quaternion.FromToRotation(hand.position-elbow.position,upper.position+direction*length-elbow.position)*elbow.rotation;
            }
            // Transport axial forearm roll about the already solved wrist ray;
            // this rotates the cuff without moving the contact point.
            Vector3 axis=(hand.position-elbow.position).normalized;
            Vector3 from=Vector3.ProjectOnPlane(elbow.up,axis),to=Vector3.ProjectOnPlane(palm*Vector3.up,axis);
            crankTwists[side]=from.sqrMagnitude>1e-8f&&to.sqrMagnitude>1e-8f?Vector3.SignedAngle(from,to,axis):0;
            elbow.rotation=Quaternion.AngleAxis(crankTwists[side],axis)*elbow.rotation;
            Quaternion solvedUpper=upper.localRotation,solvedElbow=elbow.localRotation;
            upper.localRotation=Quaternion.Slerp(baseUpper,solvedUpper,weight);elbow.localRotation=Quaternion.Slerp(baseElbow,solvedElbow,weight);
            hand.rotation=Quaternion.Slerp(baseHand,palm,weight);
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
                    Quaternion grip = rest[name] * Quaternion.Euler(0, 0, sign * (joint == 2 ? 54 : 40));
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
