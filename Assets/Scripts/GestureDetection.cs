using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

// struct = class without functions
[System.Serializable]
public struct Gesture
{
    public string name;

    public List<Vector3> fingerDatas;
    public List<Quaternion> fingersRotationData;

    public UnityEvent onRecognized;

    public UnityEvent onRecognizedEnd;
}

public class GestureDetection : MonoBehaviour
{
    [Header("Threshold value")]
    public float distanceThreshold = 0.1f;
    public float angleThreshold = 0.25f;

    [Header("Hand Skeleton")]
    public OVRSkeleton skeleton;

    [Header("List of Gestures")]
    public List<Gesture> gestures;

    private List<OVRBone> fingerbones = null;

    [Header("DebugMode")]
    public bool debugMode = true;

    private bool hasStarted = false;
    private bool hasRecognized = false;
    private bool done = false;

    private Gesture previousGesture = new Gesture();

    void Start()
    {
        //Used coroutine to wait until Oculus hands are available
        StartCoroutine(DelayRoutine(2.5f, Initialize));
    }

    // Coroutine used to delay Initialization
    public IEnumerator DelayRoutine(float delay, Action actionToDo)
    {
        yield return new WaitForSeconds(delay);
        actionToDo.Invoke();
    }

    public void Initialize()
    {
        SetSkeleton();

        hasStarted = true;
    }
    public void SetSkeleton()
    {
        // Populate list of fingerbones from the current hand 
        fingerbones = new List<OVRBone>(skeleton.Bones);
    }

    void Update()
    {
        // if in debug mode and we press Space, save a gesture
        if (debugMode && Input.GetKeyDown(KeyCode.Space))
        {
            Save();
        }

        //if the initialization was successful
        if (hasStarted.Equals(true))
        {
            Gesture currentGesture = Recognize();

            hasRecognized = !currentGesture.Equals(new Gesture());

            // and if the gesture is recognized
            if (hasRecognized && !currentGesture.Equals(previousGesture))
            {
                // we change another boolean to avoid a loop of event
                done = true;

                currentGesture.onRecognized?.Invoke();
                previousGesture = currentGesture;
                return;
            }

            else
            {
                if (done && !currentGesture.Equals(previousGesture))
                {

                    done = false;

                    previousGesture.onRecognizedEnd?.Invoke();
                    previousGesture = new Gesture();
                }
            }
        }
    }

    void Save()
    {
        Gesture g = new Gesture();

        g.name = "New Gesture";

        List<Vector3> data = new List<Vector3>();
        List<Quaternion> rotationData = new List<Quaternion>();

        foreach (var bone in fingerbones)
        {
            // the fingers positions are in base at the hand Root
            data.Add(skeleton.transform.InverseTransformPoint(bone.Transform.position));
            rotationData.Add(bone.Transform.rotation);
           
        }

        g.fingerDatas = data;
        g.fingersRotationData = rotationData;

        gestures.Add(g);
    }

    Gesture Recognize()
    {
        Gesture currentGesture = new Gesture();

        float currentMinDistance = Mathf.Infinity;
        float currentMinAngle = Mathf.Infinity;

        foreach (var gesture in gestures)
        {
            float sumDistance = 0;
            float sumAngleOffset = 0;

            bool isDiscarded = false;
           
            for (int i = 0; i < fingerbones.Count; i++)
            {
               
                Vector3 currentData = skeleton.transform.InverseTransformPoint(fingerbones[i].Transform.position);

                float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);
                float dot = Quaternion.Dot(fingerbones[i].Transform.rotation, gesture.fingersRotationData[i]);

                if (distance > distanceThreshold || (1 - dot) > angleThreshold)
                {
                    isDiscarded = true;
                    break;
                }

                sumDistance += distance;
                sumAngleOffset += (1 - dot);
            }

            if (!isDiscarded && sumDistance < currentMinDistance && sumAngleOffset < currentMinAngle)
            {
                currentMinDistance = sumDistance;
                currentMinAngle = sumAngleOffset;

                currentGesture = gesture;
            }
        }
        
        return currentGesture;
    }
}
