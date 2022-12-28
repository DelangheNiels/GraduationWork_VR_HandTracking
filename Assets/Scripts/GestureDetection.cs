using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System;

[System.Serializable]
public struct Gesture
{
    public string name;

    public OVRSkeleton hand;

    public List<Vector3> fingerDatas;
    public List<Quaternion> fingersRotationData;

    public UnityEvent onRecognized;
    public UnityEvent onRecognizedEnd;
}

[System.Serializable]
public struct DynamicGesture
{
    public string name;

    public OVRSkeleton hand;

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
    [SerializeField] OVRSkeleton handForSavingGesture;

    [Header("Hand Skeletons")]
    public OVRSkeleton rightHandSkeleton;
    public OVRSkeleton leftHandSkeleton;

    [Header("List of Gestures")]
    public List<Gesture> gestures = new List<Gesture>();

    private List<OVRBone> rightHandFingerbones = null;
    private List<OVRBone> leftHandFingerbones = null;

    [Header("Recognize Dynamic Gestures")]
    [SerializeField] private bool recognizeDynamicGestures = true;

    [Header("List of Dynamic Gestures")]
    public List<DynamicGesture> dynamicGestures = new List<DynamicGesture>();

    [Header("Dynamic Gesture values")]
    [SerializeField] private float maxTimeNextStep = 3.0f;
    private float timerNextStepLeft = 0.0f;
    private float timerNextStepRight = 0.0f;

    private DynamicGesture performedDynamicGestureLeftHand = new DynamicGesture();
    private DynamicGesture performedDynamicGestureRightHand = new DynamicGesture();

    private int sequenceIndexLeftHand = 0;
    private int sequenceIndexRightHand = 0;

    private bool isCastingDynamicSpellLeft = false;
    private bool isCastingDynamicSpellRight = false;

    private bool hasStarted = false;
    
    private bool doneRight = false;
    private bool doneLeft = false;

    private Gesture previousGestureRight = new Gesture();
    private Gesture previousGestureLeft = new Gesture();

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
        rightHandFingerbones = new List<OVRBone>(rightHandSkeleton.Bones);
        leftHandFingerbones = new List<OVRBone>(leftHandSkeleton.Bones);
    }

    void Update()
    {
        Debugging();

        if(hasStarted)
        {
            if (recognizeDynamicGestures && sequenceIndexLeftHand != 0 && !isCastingDynamicSpellLeft)
            {
                timerNextStepLeft += Time.deltaTime;
                if (timerNextStepLeft >= maxTimeNextStep)
                {
                    ResetDynamicGestureRecognitionLeftHand();
                }
            }

            if (recognizeDynamicGestures && sequenceIndexRightHand != 0 && !isCastingDynamicSpellRight)
            {
                timerNextStepRight += Time.deltaTime;
                if (timerNextStepRight >= maxTimeNextStep)
                {
                    ResetDynamicGestureRecognitionRightHand();
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
        if(handForSavingGesture)
        {
            Gesture g = new Gesture();

            g.name = "New Gesture";

            List<Vector3> data = new List<Vector3>();
            List<Quaternion> rotationData = new List<Quaternion>();

            foreach (var bone in handForSavingGesture.Bones)
            {
                // the fingers positions are in base at the hand Root
                data.Add(handForSavingGesture.transform.InverseTransformPoint(bone.Transform.position));
                rotationData.Add(bone.Transform.rotation);

            }

            g.fingerDatas = data;
            g.fingersRotationData = rotationData;
            g.hand = handForSavingGesture;

            gestureList.Add(g);
        }
    }

    void StartNewDynamicGesture()
    {
        dynamicGestures.Add(dynamicGestureToSave);

        dynamicGestureToSave = new DynamicGesture();
        dynamicGestureToSave.name = "New DynamicGesture";
        dynamicGestureToSave.gestures = new List<Gesture>();
    }

    Gesture Recognize(OVRSkeleton.SkeletonType handType, List<Gesture> listOfGestures)
    {
        Gesture currentGesture = new Gesture();

        float currentMinDistance = Mathf.Infinity;
        float currentMinAngle = Mathf.Infinity;

        foreach (var gesture in listOfGestures)
        {
            if(gesture.hand.GetSkeletonType().Equals(handType))
            {

                float sumDistance = 0;
                float sumAngleOffset = 0;

                bool isDiscarded = false;

                List<OVRBone> bones = GetBonesFromHand(gesture.hand);

                for (int i = 0; i < bones.Count; i++)
                {

                    Vector3 currentData = gesture.hand.transform.InverseTransformPoint(bones[i].Transform.position);

                    float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);
                    float dot = Quaternion.Dot(bones[i].Transform.rotation, gesture.fingersRotationData[i]);

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
            
        }
        
        return currentGesture;
    }

    Gesture RecognizeDynamicGesture(OVRSkeleton.SkeletonType hand)
    {
        
        List<Gesture> gestureList = new List<Gesture>();

        if(hand.Equals(OVRSkeleton.SkeletonType.HandLeft))
        {
            if (performedDynamicGestureLeftHand.Equals(new DynamicGesture()))
            {
                foreach (var gesture in dynamicGestures)
                {
                    gestureList.Add(gesture.gestures[0]);
                }
            }

            else
            {
                gestureList = performedDynamicGestureLeftHand.gestures;
            }
        }

        if (hand.Equals(OVRSkeleton.SkeletonType.HandRight))
        {
            if (performedDynamicGestureRightHand.Equals(new DynamicGesture()))
            {
                foreach (var gesture in dynamicGestures)
                {
                    gestureList.Add(gesture.gestures[0]);
                }
            }

            else
            {
                gestureList = performedDynamicGestureRightHand.gestures;
            }
        }

        return Recognize(hand, gestureList);
    }
    void HandleDynamicGestures()
    {
        HandleDynamicGestureLeftHand();
        HandleDynamicGestureRightHand();
    }

    void HandleStaticGestures()
    {
        if (!recognizeDynamicGestures)
        {
            Gesture currentGesture = Recognize(OVRSkeleton.SkeletonType.HandLeft,gestures);
            HandleGestureLeftHand(currentGesture);

            currentGesture = Recognize(OVRSkeleton.SkeletonType.HandRight,gestures);
            HandleGestureRightHand(currentGesture);

        }
    }

    List<OVRBone> GetBonesFromHand(OVRSkeleton hand)
    {
        List<OVRBone> bones = new List<OVRBone>();

        if (hand.Equals(leftHandSkeleton))
            bones = leftHandFingerbones;
        if (hand.Equals(rightHandSkeleton))
            bones = rightHandFingerbones;

        return bones;
    }

    void HandleGestureRightHand(Gesture currentGesture)
    {
        bool hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized &&!currentGesture.Equals(previousGestureRight))
        {
            doneRight = true;

            currentGesture.onRecognized?.Invoke();
            previousGestureRight = currentGesture;
            return;
        }

        else
        {
            if (doneRight && !currentGesture.Equals(previousGestureRight))
            {

                doneRight = false;

                previousGestureRight.onRecognizedEnd?.Invoke();
                previousGestureRight = new Gesture();
            }
        }
    }

    void HandleGestureLeftHand(Gesture currentGesture)
    {
        bool hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized &&!currentGesture.Equals(previousGestureLeft))
        {
            doneLeft = true;

            currentGesture.onRecognized?.Invoke();
            previousGestureLeft = currentGesture;
            return;
        }

        else
        {
            if (doneLeft && !currentGesture.Equals(previousGestureLeft))
            {

                doneLeft = false;

                previousGestureLeft.onRecognizedEnd?.Invoke();
                previousGestureLeft = new Gesture();
            }
        }
    }

    void HandleDynamicGestureLeftHand()
    {
        Gesture currentGesture = RecognizeDynamicGesture(OVRSkeleton.SkeletonType.HandLeft);
        bool hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized)
        {
            if (sequenceIndexLeftHand == 0)
            {
                foreach (var gesture in dynamicGestures)
                {
                    if (currentGesture.Equals(gesture.gestures[0]) && gesture.hand.GetSkeletonType().Equals(OVRSkeleton.SkeletonType.HandLeft))
                    {
                        performedDynamicGestureLeftHand = gesture;
                        sequenceIndexLeftHand = 1;
                        return;
                    }
                }
            }

            else
            {
                if (currentGesture.Equals(performedDynamicGestureLeftHand.gestures[sequenceIndexLeftHand]))
                {
                    timerNextStepLeft = 0.0f;

                    if (!performedDynamicGestureLeftHand.Equals(new DynamicGesture()) && sequenceIndexLeftHand >= performedDynamicGestureLeftHand.gestures.Count - 1 && !isCastingDynamicSpellLeft)
                    {
                        isCastingDynamicSpellLeft = true;
                        performedDynamicGestureLeftHand.onRecognized?.Invoke();
                    }

                    else
                    {
                        if (!isCastingDynamicSpellLeft)
                            sequenceIndexLeftHand++;
                    }

                }
            }
        }

        else
        {
            if (isCastingDynamicSpellLeft)
            {
                ResetDynamicGestureRecognitionLeftHand();
            }
        }
    }

    void HandleDynamicGestureRightHand()
    {
        Gesture currentGesture = RecognizeDynamicGesture(OVRSkeleton.SkeletonType.HandRight);
        bool hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized)
        {
            if (sequenceIndexRightHand == 0)
            {
                foreach (var gesture in dynamicGestures)
                {
                    if (currentGesture.Equals(gesture.gestures[0]) && gesture.hand.GetSkeletonType().Equals(OVRSkeleton.SkeletonType.HandRight))
                    {
                        performedDynamicGestureRightHand = gesture;
                        sequenceIndexRightHand = 1;
                        return;
                    }
                }
            }

            else
            {
                if (currentGesture.Equals(performedDynamicGestureRightHand.gestures[sequenceIndexRightHand]))
                {
                    timerNextStepRight = 0.0f;

                    if (!performedDynamicGestureRightHand.Equals(new DynamicGesture()) && sequenceIndexRightHand >= performedDynamicGestureRightHand.gestures.Count - 1 && !isCastingDynamicSpellRight)
                    {
                        isCastingDynamicSpellRight = true;
                        performedDynamicGestureRightHand.onRecognized?.Invoke();
                    }

                    else
                    {
                        if (!isCastingDynamicSpellRight)
                            sequenceIndexRightHand++;
                    }
                }
            }
        }

        else
        {
            if (isCastingDynamicSpellRight)
            {
                ResetDynamicGestureRecognitionRightHand();
            }
        }
    }

    void ResetDynamicGestureRecognitionLeftHand()
    {
        performedDynamicGestureLeftHand.onRecognizedEnd?.Invoke();
        timerNextStepLeft = 0.0f;
        sequenceIndexLeftHand = 0;
        performedDynamicGestureLeftHand = new DynamicGesture();
        isCastingDynamicSpellLeft = false;
    }

    void ResetDynamicGestureRecognitionRightHand()
    {
        performedDynamicGestureRightHand.onRecognizedEnd?.Invoke();
        timerNextStepRight = 0.0f;
        sequenceIndexRightHand = 0;
        performedDynamicGestureRightHand = new DynamicGesture();
        isCastingDynamicSpellRight = false;
    }
}
