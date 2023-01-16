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

    private List<DynamicGesture> possibleDynamicGesturesForLeftHand = new List<DynamicGesture>();
    private List<DynamicGesture> possibleDynamicGesturesForRightHand = new List<DynamicGesture>();
    //-------------------------------------------------------------------------------------------------------------------//

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
                    ResetDynamicRecognition(ref performedDynamicGestureLeftHand, ref timerNextStepLeft, ref sequenceIndexLeftHand, ref possibleDynamicGesturesForLeftHand, ref isCastingDynamicSpellLeft);
                }
            }

            if (recognizeDynamicGestures && sequenceIndexRightHand != 0 && !isCastingDynamicSpellRight)
            {
                timerNextStepRight += Time.deltaTime;
                if (timerNextStepRight >= maxTimeNextStep)
                {
                    ResetDynamicRecognition(ref performedDynamicGestureRightHand, ref timerNextStepRight, ref sequenceIndexRightHand, ref possibleDynamicGesturesForRightHand, ref isCastingDynamicSpellRight);
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

    List<Gesture> Recognize(OVRSkeleton.SkeletonType handType, List<Gesture> listOfGestures, bool doClosestCheck = true)
    {
       
        List<Gesture> foundGestures = new List<Gesture>();

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

                if(doClosestCheck)
                {
                    if (!isDiscarded && sumDistance < currentMinDistance && sumAngleOffset < currentMinAngle)
                    {
                        currentMinDistance = sumDistance;
                        currentMinAngle = sumAngleOffset;

                        if(foundGestures.Count == 0)
                        {
                            foundGestures.Add(gesture);
                        }

                        else
                        {
                            foundGestures[0] = gesture;
                        }
                    }
                }

                else
                    if(!isDiscarded)
                        foundGestures.Add(gesture);

            }
            
        }
        
        return foundGestures;
    }

    List<Gesture> RecognizeDynamicGesture(OVRSkeleton.SkeletonType hand)
    {
        
        List<Gesture> gestureList = new List<Gesture>();

        foreach(var dynamicGesture in dynamicGestures)
        {
            for (int i = 0; i < dynamicGesture.gestures.Count; i++)
                gestureList.Add(dynamicGesture.gestures[i]);
        }

        return Recognize(hand, gestureList, false);
    }
    void HandleDynamicGestures()
    {
        HandleDynamicHandGesture(OVRSkeleton.SkeletonType.HandLeft, ref timerNextStepLeft, ref sequenceIndexLeftHand, ref possibleDynamicGesturesForLeftHand, ref performedDynamicGestureLeftHand, ref isCastingDynamicSpellLeft);
        HandleDynamicHandGesture(OVRSkeleton.SkeletonType.HandRight, ref timerNextStepRight, ref sequenceIndexRightHand, ref possibleDynamicGesturesForRightHand, ref performedDynamicGestureRightHand, ref isCastingDynamicSpellRight);
    }

    void HandleStaticGestures()
    {
        if (!recognizeDynamicGestures)
        {
            List<Gesture> foundGesturesLeftHand = Recognize(OVRSkeleton.SkeletonType.HandLeft, gestures,true);
            if(foundGesturesLeftHand.Count > 0)
            {
                Gesture currentGesture = foundGesturesLeftHand[0];
                HandleGesture(currentGesture, ref previousGestureLeft,ref doneLeft);
            }

            else
            {
                HandleGesture(new Gesture(), ref previousGestureLeft, ref doneLeft);
            }

            List<Gesture> foundGesturesRightHand = Recognize(OVRSkeleton.SkeletonType.HandRight, gestures, true);
            if (foundGesturesRightHand.Count > 0)
            {
                Gesture currentGesture = foundGesturesRightHand[0];
                HandleGesture(currentGesture, ref previousGestureRight, ref doneRight);
            }

            else
            {
                HandleGesture(new Gesture(), ref previousGestureRight, ref doneRight);
            }



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

    void HandleGesture(Gesture currentGesture, ref Gesture previousGesture, ref bool isPerformed)
    {
        bool hasRecognized = !currentGesture.Equals(new Gesture());

        if (hasRecognized &&!currentGesture.Equals(previousGesture))
        {
            isPerformed = true;

            currentGesture.onRecognized?.Invoke();
            previousGesture = currentGesture;
            return;
        }

        else
        {
            if (isPerformed && !currentGesture.Equals(previousGesture))
            {

                isPerformed = false;

                previousGesture.onRecognizedEnd?.Invoke();
                previousGesture = new Gesture();
            }
        }
    }

    void HandleDynamicHandGesture(OVRSkeleton.SkeletonType hand,ref float nextStepTimer ,ref int sequenceIndex, ref List<DynamicGesture> listPossibleDynamicGestures, ref DynamicGesture prevPerformedDynamicGesture, ref bool isCastingSpell)
    {
        bool increaseIndex = false;

        List<Gesture> foundGestures = RecognizeDynamicGesture(hand);
        bool hasRecognized = foundGestures.Count > 0;

        if (hasRecognized)
        {
            foreach (var gesture in foundGestures)
            {
                for (int i = 0; i < dynamicGestures.Count; i++)
                {
                    if (sequenceIndex < dynamicGestures[i].gestures.Count)
                    {
                        if (gesture.hand.Equals(dynamicGestures[i].hand) && gesture.Equals(dynamicGestures[i].gestures[sequenceIndex]))
                        {
                            timerNextStepLeft = 0.0f;
                            gesture.onRecognized?.Invoke();

                            if (!listPossibleDynamicGestures.Contains(dynamicGestures[i]))
                                listPossibleDynamicGestures.Add(dynamicGestures[i]);

                            increaseIndex = true;
                        }

                        else
                        {
                            if (listPossibleDynamicGestures.Contains(dynamicGestures[i]))
                                listPossibleDynamicGestures.Remove(dynamicGestures[i]);
                        }
                    }
                }
            }


            if (listPossibleDynamicGestures.Count > 0)
            {
                foreach (var dynamicGesture in listPossibleDynamicGestures)
                {
                    if (sequenceIndex == dynamicGesture.gestures.Count && prevPerformedDynamicGesture.Equals(new DynamicGesture()))
                    {
                        dynamicGesture.onRecognized?.Invoke();
                        prevPerformedDynamicGesture = dynamicGesture;
                        isCastingSpell = true;

                    }
                }
            }

            if (prevPerformedDynamicGesture.Equals(new DynamicGesture()) && increaseIndex)
                sequenceIndex++;

        }

        else
        {
            if (!prevPerformedDynamicGesture.Equals(new DynamicGesture()))
            {
                prevPerformedDynamicGesture.onRecognizedEnd?.Invoke();
                ResetDynamicRecognition(ref prevPerformedDynamicGesture, ref nextStepTimer, ref sequenceIndex, ref listPossibleDynamicGestures, ref isCastingSpell);
            }
        }
    }

    void ResetDynamicRecognition(ref DynamicGesture prevPerformedDynamicGesture, ref float nextStepTimer,ref int sequenceIndex, ref List<DynamicGesture> listPossibleDynamicGestures, ref bool isCastingSpell)
    {
        prevPerformedDynamicGesture.onRecognizedEnd?.Invoke();
        nextStepTimer = 0.0f;
        sequenceIndex = 0;
        prevPerformedDynamicGesture = new DynamicGesture();
        isCastingSpell = false;
        listPossibleDynamicGestures.Clear();
    }

}
