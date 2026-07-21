using AIPProvider.src.Utilities;
using AIPProvider.src.Utilities.Settings;
using Recorder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Radar.STT
{
    internal class GunFunnel
    {
        private Utils utils;
        private const int trail_size = 10;
        private const int path_size = 20;
        private const int path_step_size = 20;

        private Transform transform;

        private List<Transform> trail; // past positions, rotation ajusted to velocity
        private TrailVect trailVel;

        private List<Transform> path; // future positions

        private Vector3 funnel;
        private bool funnelFound;

        private DebugLine gunLine;

        private List<DebugLine> linesPath;
        private List<DebugLine> linesUp;

        // ---------------- old code --------------------
        private TrailVect vel;
        private Gimball gimball;

        private DebugLine line;


        public GunFunnel(Utils utilities)
        {
            utils = utilities;
            transform = new Transform();

            trailVel = new TrailVect(trail_size);
            trail = new List<Transform>();
            for (int i = 0; i < trail_size; i++) { trail.Add(new Transform()); }
            path = new List<Transform>();
            for (int i = 0; i < path_size; i++) { path.Add(new Transform()); }


            linesPath = new List<DebugLine>();
            linesUp = new List<DebugLine>();
            for (int i = 0; i < path_size; i++)
            {
                Vector3 color = 255 * new Vector3(
                        Utils.MapRange(i, 0, path_size - 1, 1.0f, 0.5f),
                        Utils.MapRange(i, 0, path_size - 1, 1.0f, 0.3f),
                        0.1f
                    );
                linesPath.Add(utils.MakeLine(color));
                linesUp.Add(utils.MakeLine(color));
            }

            gunLine = utils.MakeLine(ColorMap.Gun);

            // ---------------- old code --------------------
            vel = new TrailVect(10);
            gimball = new Gimball(utilities);
            line = utils.MakeLine((int)255, 255, 255);
            //lineY = utils.MakeLine(0.0f, 1.0f, 0.0f);
            //lineZ = utils.MakeLine(0.0f, 0.0f, 1.0f);
        }

        public void Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            // TODO: get target's actual data
            Vector3 pos = state.radar.sttedTarget?.position ?? state.kinematics.position;
            Quaternion rot = state.radar.sttedTarget?.rotation ?? state.kinematics.rotation;
            Vector3 vel = state.radar.sttedTarget?.velocity ?? state.kinematics.velocity;
            //if (state.radar.sttedTarget != null)
            //{
            //    pos = state.radar.sttedTarget?.position ?? state.kinematics.position;
            //    rot = state.radar.sttedTarget?.rotation ?? state.kinematics.rotation;
            //    vel = state.radar.sttedTarget?.velocity ?? state.kinematics.velocity;
            //}
            if(state.radar.twsedTargets.Length >= 1)
            {
                pos = state.radar.twsedTargets[0].position;
                rot = state.radar.twsedTargets[0].rotation;
                vel = state.radar.twsedTargets[0].velocity;
            }

            Vector3 gunMuzzle = state.kinematics.rotation.quat * Vector3.forward * 1100 + state.kinematics.velocity;

            funnel = Vector3.zero;

            AddTrail(pos, rot, vel);
            MakePath(vel, state.kinematics.position, gunMuzzle);

            /*
            int iterCount = path_size * path_step_size;
            int keyIndex = 0;
            Transform marchingTransform = trail[0].Clone();
            //Quaternion stepRotation = Quaternion.Inverse(trail[1].rotation) * trail[0].rotation; // wrong Quat order
            //Quaternion stepRotation = trail[0].rotation * Quaternion.Inverse(trail[1].rotation); // 
            Quaternion stepRotation = AverageRot();
            for (int i = 0; i < iterCount; i++)
            {
                // rotate path
                marchingTransform.rotation = stepRotation * marchingTransform.rotation;
                // move path
                marchingTransform.position = marchingTransform.position + marchingTransform.forward * vel.magnitude * Time.fixedDeltaTime;

                if(CheckCollision(i, state.kinematics.position, marchingTransform.position, 1100 + vel.magnitude))
                {
                    gunLine.start = state.kinematics.position;
                    gunLine.end = marchingTransform.position;
                    utils.Draw(gunLine);
                }

                if ((i % path_step_size) == 0)
                {
                    // path keypoint, draw lines and stuff
                    path[keyIndex] = marchingTransform.Clone();

                    keyIndex++;
                }
            }
            */
            DrawPath();


            return;

            //---------------- old code --------------------
            Vector3 velo = (state.kinematics.position - transform.position); // / Time.fixedDeltaTime;
            //vel.Add(velo);

            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            //transform.rotation = Quaternion.FromToRotation(transform.forward, vel.Avg()) * transform.rotation;
            transform.rotation = Quaternion.FromToRotation(transform.forward, velo) * transform.rotation;
            //transform.rotation = Quaternion.FromToRotation(transform.forward, state.kinematics.velocity) * transform.rotation;

            gimball.Update(transform);

            //line.start = transform.position;
            //line.end = transform.position + velo * 100f;
            //line.end = transform.position + state.kinematics.velocity.vec3 * 100f;
            //utils.Draw(line);
        }

        public bool TryGetFunnel(OutboundState state, ref List<int> actions, out Vector3 corrVect)
        {
            corrVect = Vector3.zero;
            if ((funnel - transform.position).magnitude < 10 || (funnel - transform.position).magnitude > 1000)
            {
                // out of range
                return false;
            }

            if (state.radar.twsedTargets.Length < 1)
            {
                // no target locked, no pipper
                return false;
            }

            //corrVect = transform.InverseTransformVector(funnel);
            corrVect = funnel;
            Vector3 gunMuzzle = state.kinematics.rotation.quat * Vector3.forward * 1100 + state.kinematics.velocity;
            float align = Vector3.Dot(gunMuzzle.normalized, (funnel - transform.position).normalized);

            if (align >= 0.995f)
            {
                actions.Add((int)InboundAction.SelectHardpoint);
                actions.Add(-1);
                actions.Add((int)InboundAction.Fire);
                utils.Log("GUN GUN GUN");
            }

            return true;
        }

        private void MakePath(Vector3 vel, Vector3 selfOrigin, Vector3 muzzle)
        {
            // loop parameters
            int iterCount = path_size * path_step_size;
            int keyIndex = 0;

            // starting values
            Transform marchingTransform = trail[0].Clone();
            Vector3 marchingVel = trailVel.Latest();

            // increment sizes
            //Quaternion stepRotation = Quaternion.Inverse(trail[1].rotation) * trail[0].rotation; // wrong Quat order
            //Quaternion stepRotation = trail[0].rotation * Quaternion.Inverse(trail[1].rotation);
            Quaternion stepRotation = AverageRot();
            Vector3 accel = trailVel.AvgDelta();

            funnelFound = false;
            for (int i = 0; i < iterCount; i++)
            {
                // TODO: Adjust velocity based on acceleration and gravity

                // rotate path
                marchingTransform.rotation = stepRotation * marchingTransform.rotation;
                // move path
                marchingTransform.position = marchingTransform.position + marchingTransform.forward * vel.magnitude * Time.fixedDeltaTime;

                // ---------------------------------------------------------------------------------------------------
                // TODO: checking for pipper point should somehow be done here since we have the full resolution trail
                // ---------------------------------------------------------------------------------------------------
                // Step 1) find intersection point
                // Step 2) check if nose is matching 


                if (CheckCollision(i, selfOrigin, marchingTransform.position, muzzle))
                {
                    gunLine.start = selfOrigin;
                    gunLine.end = marchingTransform.position;
                    utils.Draw(gunLine);
                    //funnel = marchingTransform.position - transform.position;
                    if(!funnelFound)
                    {
                        funnel = marchingTransform.position;
                        funnelFound = true;
                    }
                }

                if ((i % path_step_size) == 0)
                {
                    // path keypoint, draw lines and stuff
                    path[keyIndex] = marchingTransform.Clone();

                    keyIndex++;
                }
            }
        }




        private void AddTrail(Vector3 pos, Quaternion rot, Vector3 vel)
        {
            // adjust rotation to make it match the velocity
            // this removes error caused by AoA and stuff
            Transform t = new Transform();
            t.position = pos;
            t.rotation = Quaternion.FromToRotation(rot * Vector3.forward, vel) * rot;
            // if i swap the two quanternion i get a prediciton similar to the missile bug

            // add the adjusted transform to the trail (history)
            for (int i = trail.Count - 1; i > 0; i--)
            {
                trail[i] = trail[i - 1].Clone();
            }
            trail[0] = t;

            trailVel.Add(vel);
        }

        private Quaternion AverageRot()
        {
            Quaternion avg = new Quaternion();
            for (int i = trail_size - 1; i > 0; i--)
            {
                Quaternion stepRotation = trail[i-1].rotation * Quaternion.Inverse(trail[i].rotation);
                avg = Quaternion.Slerp(avg, stepRotation, 0.5f);
            }
            return avg;
            //return trail[0].rotation * Quaternion.Inverse(trail[1].rotation);
        }

        private bool CheckCollision(int i, Vector3 point, Vector3 origin, Vector3 muzzle)
        {
            float muzzleVel = muzzle.magnitude;

            float bulletTime = (point - origin).magnitude / muzzleVel; // time for bullet to reach target
            float planeTime = i * Time.deltaTime;

            if(Math.Abs(bulletTime - planeTime) < 0.2f)
            {
                return true;
            }
            return false;
        }

        private void DrawPath()
        {
            for(int i = 0; i < path_size - 2; i++)
            {
                linesPath[i].start = path[i].position;
                linesPath[i].end = path[i + 1].position;
                linesUp[i].start = path[i].position;
                linesUp[i].end = path[i].position + path[i].up * 20;
                utils.Draw(linesPath[i]);
                utils.Draw(linesUp[i]);
            }

        }

    }
}
