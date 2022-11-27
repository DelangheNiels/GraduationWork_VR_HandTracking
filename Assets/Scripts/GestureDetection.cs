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
}

public class GestureDetection : MonoBehaviour
{
    [Header("Threshold value")]
    public float distanceThreshold = 0.1f;
    public float dotThreshold = 0.25f;

    [Header("Hand Skeleton")]
    public OVRSkeleton skeleton;

    [Header("List of Gestures")]
    public List<Gesture> gestures;

    private List<OVRBone> fingerbones = null;

    [Header("DebugMode")]
    public bool debugMode = true;

    private bool hasStarted = false;
    private bool hasRecognize = false;
    private bool done = false;

    //// Add an event if you want to make happen when a gesture is not identified
    //[Header("Not Recognized Event")]
    //public UnityEvent notRecognize;

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
        // Populate the private list of fingerbones from the current hand we put in the skeleton
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

            hasRecognize = !currentGesture.Equals(new Gesture());

            // and if the gesture is recognized
            if (hasRecognize && !currentGesture.Equals(previousGesture))
            {
                // we change another boolean to avoid a loop of event
                done = true;

                Debug.Log("The name of the found gesture is: " + currentGesture.name);
                //currentGesture.onRecognized?.Invoke();
                previousGesture = currentGesture;
            }

            else
            {
                if (done)
                {
                    Debug.Log("Not Recognized");
                    // we set to false the boolean again, so this will not loop
                    done = false;

                    // and finally we will invoke an event when we end to make the previous gesture
                    //notRecognize?.Invoke();
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

        float currentMin = Mathf.Infinity;

        foreach (var gesture in gestures)
        {
            float sumDistance = 0;

            bool isDiscarded = false;

            for (int i = 0; i < fingerbones.Count; i++)
            {
               
                Vector3 currentData = skeleton.transform.InverseTransformPoint(fingerbones[i].Transform.position);

                // with a new float we calculate the distance between the current gesture we are making with all the gesture we saved
                float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);
                //float angle = Quaternion.Angle(skeleton.transform.rotation, gesture.fingersRotationData[i]);
                float dot = Math.Abs(Quaternion.Dot(skeleton.transform.rotation, gesture.fingersRotationData[i]));
                //Debug.Log(dot + "zzzzzzzzzzzzzzzz" + (1 - dotThreshold));
                // if the distance is bigger than threshold discard gesture
                if (distance > distanceThreshold )
                {
                    //Debug.Log("discard");
                    isDiscarded = true;
                    break;
                }
                //Debug.Log("No discard uuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuuu");
                sumDistance += distance;
            }

            if (!isDiscarded && sumDistance < currentMin)
            {
                currentMin = sumDistance;

                currentGesture = gesture;
            }
        }

        return currentGesture;
    }
}
