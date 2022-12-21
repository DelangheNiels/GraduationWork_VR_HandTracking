using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

[System.Serializable]
public struct Gesture
{
    public string name;

    public List<Vector3> fingerDatas;
    public List<Quaternion> fingersRotationData;

    public UnityEvent onRecognized;
    public UnityEvent onRecognizedEnd;
}

[System.Serializable]
public struct DynamicGesture
{
    public string name;

    public List<Gesture> gestures;

    public UnityEvent onRecognized;
    public UnityEvent onRecognizedEnd;
}

public class GestureDetection : MonoBehaviour
{
    [Header("Threshold value")]
    [SerializeField] private float distanceThreshold = 0.1f;
    [SerializeField] private float angleThreshold = 0.25f;

    [Header("DebugMode")]
    [SerializeField] private bool debugMode = true;

    [Header("Hand Skeleton")]
    public OVRSkeleton skeleton;

    [Header("List of Gestures")]
    public List<Gesture> gestures = new List<Gesture>();

    private List<OVRBone> fingerbones = null;

    [Header("Recognize Dynamic Gestures")]
    [SerializeField] private bool recognizeDynamicGestures = true;

    [Header("List of Dynamic Gestures")]
    public List<DynamicGesture> dynamicGestures = new List<DynamicGesture>();

    [Header("Dynamic Gesture values")]
    [SerializeField] private float maxTimeNextStep = 3.0f;
    private float timerNextStep = 0.0f;
    private DynamicGesture performedDynamicGesture = new DynamicGesture();
    private int sequenceIndex = 0;
    private bool isCastingDynamicSpell = false;

    private bool hasStarted = false;
    private bool hasRecognized = false;
    private bool done = false;

    private Gesture previousGesture = new Gesture();

    //Only use when in debug mode to save gestures to list
    private DynamicGesture dynamicGestureToSave = new DynamicGesture();

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

        dynamicGestureToSave.gestures = new List<Gesture>();

        hasStarted = true;
    }
    public void SetSkeleton()
    {
        fingerbones = new List<OVRBone>(skeleton.Bones);
    }

    void Update()
    {
        Debugging();

        if(hasStarted)
        {
            if (recognizeDynamicGestures && sequenceIndex != 0)
            {
                timerNextStep += Time.deltaTime;
                if (timerNextStep >= maxTimeNextStep)
                {
                    ResetDynamicGestureRecognition();
                }
            }

            HandleDynamicGestures();

            HandleStaticGestures();
        }
        
    }

    void Debugging()
    {
        if (debugMode && Input.GetKeyDown(KeyCode.Space))
        {
            if (!recognizeDynamicGestures)
            {
                Save(gestures);
            }

            else
            {
                Save(dynamicGestureToSave.gestures);
            }

        }

        //Start new creation of dynamic gesture
        if(debugMode && Input.GetKeyDown(KeyCode.LeftControl))
        {
            Debug.Log("adding dynamic gesture");
            StartNewDynamicGesture();
        }
    }

    void Save(List<Gesture> gestureList)
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

        gestureList.Add(g);
    }

    void StartNewDynamicGesture()
    {
        dynamicGestures.Add(dynamicGestureToSave);

        dynamicGestureToSave = new DynamicGesture();
        dynamicGestureToSave.name = "New DynamicGesture";
        dynamicGestureToSave.gestures = new List<Gesture>();
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

    Gesture RecognizeDynamicGesture()
    {
        Gesture currentGesture = new Gesture();

        float currentMinDistance = Mathf.Infinity;
        float currentMinAngle = Mathf.Infinity;

        List<Gesture> gestureList = new List<Gesture>();

        if (performedDynamicGesture.Equals(new DynamicGesture()))
        {
            foreach(var gesture in dynamicGestures)
            {
                gestureList.Add(gesture.gestures[0]);
            }
        }

        else
        {
            gestureList = performedDynamicGesture.gestures;
        }

        foreach (var gesture in gestureList)
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
    void HandleDynamicGestures()
    {
        Gesture currentGesture = RecognizeDynamicGesture();

        hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized)
        {
            if (sequenceIndex == 0)
            {
                foreach(var gesture in dynamicGestures)
                {
                    if(currentGesture.Equals(gesture.gestures[0]))
                    {
                        performedDynamicGesture = gesture;
                        sequenceIndex++;
                    }
                }
            }

            else
            {
                if(currentGesture.Equals(performedDynamicGesture.gestures[sequenceIndex]))
                {
                    timerNextStep = 0.0f;
                    sequenceIndex++;

                    if (!performedDynamicGesture.Equals(new DynamicGesture()) && sequenceIndex == performedDynamicGesture.gestures.Count)
                    {
                        isCastingDynamicSpell = true;
                        performedDynamicGesture.onRecognized?.Invoke();
                    }
                }
            }
        }

        else
        {
            if(isCastingDynamicSpell)
            {
                Debug.Log("----------------Resetting Gesture---------------------");
                ResetDynamicGestureRecognition();
            }
        }
       
    }

    void HandleStaticGestures()
    {
        if (hasStarted && !recognizeDynamicGestures)
        {
            Gesture currentGesture = Recognize();

            hasRecognized = !currentGesture.Equals(new Gesture());

            if (hasRecognized && !currentGesture.Equals(previousGesture))
            {
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

    void ResetDynamicGestureRecognition()
    {
        performedDynamicGesture.onRecognizedEnd?.Invoke();
        timerNextStep = 0.0f;
        sequenceIndex = 0;
        performedDynamicGesture = new DynamicGesture();
        isCastingDynamicSpell = false;
    }
}
