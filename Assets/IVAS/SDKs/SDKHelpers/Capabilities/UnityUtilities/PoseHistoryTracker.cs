using System;
using System.Collections.Generic;

using UnityEngine;

namespace SensorsSDK.UnityUtilities
{
    public class PoseHistoryTracker
    {
        struct Pose
        {
            public DateTime Time;
            public Vector3 Position;
            public Quaternion Rotation;
        }

        const float maxHistorySeconds = 0.5f;

        private Queue<Pose> history = new Queue<Pose>((int)(maxHistorySeconds * 60 + 10));

        private Pose lastQueuedPose;

        // Update is called once per frame
        public void AddPose(DateTime now, Matrix4x4 mat)
        {
            AddPose(now, mat.GetColumn(3), mat.rotation);
        }

        public void AddPose(DateTime now, GameObject obj)
        {
            AddPose(now, obj.transform);
        }

        public void AddPose(DateTime now, Transform transform)
        {
            AddPose(now, transform.position, transform.rotation);
        }

        public void AddPose(DateTime now, Vector3 position, Quaternion rotation)
        {
            lastQueuedPose.Time = now;
            lastQueuedPose.Position = position;
            lastQueuedPose.Rotation = rotation;

            history.Enqueue(lastQueuedPose);

            while ((now - history.Peek().Time).Milliseconds > (maxHistorySeconds * 1000))
            {
                history.Dequeue();
            }
        }

        public bool LookupHistoricalPose(DateTime searchTime, ref Vector3 position, ref Quaternion rotation)
        {
            bool poseUpdated = false;
            bool hasLastPose = false;
            if (searchTime > lastQueuedPose.Time)
            {
                rotation = lastQueuedPose.Rotation;
                position = lastQueuedPose.Position;
                return true;
            }

            Pose lastPose = new Pose();
            // FIXME: Do a faster list search. Iterative is *very slow*. Lazily, maybe just reverse search?
            foreach (Pose currentPose in history)
            {
                if (currentPose.Time >= searchTime)
                {
                    // We can only interpolate if we have a previous pose to interpolate against.
                    if (hasLastPose)
                    {
                        float lerpValue = (searchTime - lastPose.Time).Milliseconds / (float)((currentPose.Time - lastPose.Time).Milliseconds);
                        rotation = Quaternion.Slerp(lastPose.Rotation, currentPose.Rotation, lerpValue);
                        position = Vector3.Lerp(lastPose.Position, currentPose.Position, lerpValue);
                        poseUpdated = true;
                    }
                    // If our oldest time entry is too recent, but the requested time is in the
                    // range we should support, we likely haven't filled our buffer yet. Just return
                    // our oldest data.
                    else if ((lastPose.Time - searchTime).Milliseconds <= (maxHistorySeconds * 1000))
                    {
                        rotation = currentPose.Rotation;
                        position = currentPose.Position;
                        poseUpdated = true;
                    }

                    // Once we've advanced past the search time, there is no point in searching further.
                    break;
                }

                hasLastPose = true;
                lastPose = currentPose;
            }

            //if (!poseUpdated)
            //{
            //    Debug.LogError($"Could not find pose in for {searchTime} in {history.Count} entires, ranging {history.ToArray()[0].Time} to {history.ToArray()[history.Count - 1].Time}");
            //}

            return poseUpdated;
        }
    }
}
