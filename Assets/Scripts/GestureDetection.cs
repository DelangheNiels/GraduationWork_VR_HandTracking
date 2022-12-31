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
        HandleDynamicGestureLeftHand();
        HandleDynamicGestureRightHand();
    }

    void HandleStaticGestures()
    {
        if (!recognizeDynamicGestures)
        {
            List<Gesture> foundGesturesLeftHand = Recognize(OVRSkeleton.SkeletonType.HandLeft, gestures,true);
            if(foundGesturesLeftHand.Count > 0)
            {
                Gesture currentGesture = foundGesturesLeftHand[0];
                HandleGestureLeftHand(currentGesture);
            }

            else
            {
                HandleGestureLeftHand(new Gesture());
            }

            List<Gesture> foundGesturesRightHand = Recognize(OVRSkeleton.SkeletonType.HandRight, gestures, true);
            if (foundGesturesRightHand.Count > 0)
            {
                Gesture currentGesture = foundGesturesRightHand[0];
                HandleGestureRightHand(currentGesture);
            }

            else
            {
                HandleGestureRightHand(new Gesture());
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

    void ResetDynamicGestureRecognitionLeftHand()
    {
        performedDynamicGestureLeftHand.onRecognizedEnd?.Invoke();
        timerNextStepLeft = 0.0f;
        sequenceIndexLeftHand = 0;
        performedDynamicGestureLeftHand = new DynamicGesture();
        isCastingDynamicSpellLeft = false;
        possibleDynamicGesturesForLeftHand.Clear();
    }

    void ResetDynamicGestureRecognitionRightHand()
    {
        performedDynamicGestureRightHand.onRecognizedEnd?.Invoke();
        timerNextStepRight = 0.0f;
        sequenceIndexRightHand = 0;
        performedDynamicGestureRightHand = new DynamicGesture();
        isCastingDynamicSpellRight = false;
        possibleDynamicGesturesForRightHand.Clear();
    }

    void HandleDynamicGestureLeftHand()
    {
        bool increaseIndex = false;

        List<Gesture> foundGestures = RecognizeDynamicGesture(OVRSkeleton.SkeletonType.HandLeft);
        bool hasRecognized = foundGestures.Count > 0;

        if (hasRecognized)
        {
            foreach (var gesture in foundGestures)
            {
                for (int i = 0; i < dynamicGestures.Count; i++)
                {
                    if (sequenceIndexLeftHand < dynamicGestures[i].gestures.Count)
                    {
                        if (gesture.hand.Equals(dynamicGestures[i].hand) && gesture.Equals(dynamicGestures[i].gestures[sequenceIndexLeftHand]))
                        {
                            timerNextStepLeft = 0.0f;
                            gesture.onRecognized?.Invoke();

                            if (!possibleDynamicGesturesForLeftHand.Contains(dynamicGestures[i]))
                                possibleDynamicGesturesForLeftHand.Add(dynamicGestures[i]);

                            increaseIndex = true;
                        }

                        else
                        {
                            if (possibleDynamicGesturesForLeftHand.Contains(dynamicGestures[i]))
                                possibleDynamicGesturesForLeftHand.Remove(dynamicGestures[i]);
                        }
                    }
                }
            }


            if (possibleDynamicGesturesForLeftHand.Count > 0)
            {
                foreach (var dynamicGesture in possibleDynamicGesturesForLeftHand)
                {
                    if (sequenceIndexLeftHand == dynamicGesture.gestures.Count &&  performedDynamicGestureLeftHand.Equals(new DynamicGesture()))
                    {
                        dynamicGesture.onRecognized?.Invoke();
                        performedDynamicGestureLeftHand = dynamicGesture;

                    }
                }
            }

            if (performedDynamicGestureLeftHand.Equals(new DynamicGesture()) && increaseIndex)
                sequenceIndexLeftHand++;

        }

        else
        {
            if (!performedDynamicGestureLeftHand.Equals(new DynamicGesture()))
            {
                performedDynamicGestureLeftHand.onRecognizedEnd?.Invoke();
                ResetDynamicGestureRecognitionLeftHand();
            }
        }
    }

    void HandleDynamicGestureRightHand()
    {
        bool increaseIndex = false;

        List<Gesture> foundGestures = RecognizeDynamicGesture(OVRSkeleton.SkeletonType.HandRight);
        bool hasRecognized = foundGestures.Count > 0;

        if (hasRecognized)
        {
            foreach (var gesture in foundGestures)
            {
                for (int i = 0; i < dynamicGestures.Count; i++)
                {
                    if(sequenceIndexRightHand < dynamicGestures[i].gestures.Count)
                    {
                        if (gesture.hand.Equals(dynamicGestures[i].hand) && gesture.Equals(dynamicGestures[i].gestures[sequenceIndexRightHand]))
                        {
                            timerNextStepLeft = 0.0f;
                            gesture.onRecognized?.Invoke();

                            if(!possibleDynamicGesturesForRightHand.Contains(dynamicGestures[i]))
                                possibleDynamicGesturesForRightHand.Add(dynamicGestures[i]);

                            increaseIndex = true;
                        }

                        else
                        {
                            if (possibleDynamicGesturesForRightHand.Contains(dynamicGestures[i]))
                                possibleDynamicGesturesForRightHand.Remove(dynamicGestures[i]);
                        }
                    }
                }
            }
                

            if (possibleDynamicGesturesForRightHand.Count > 0)
            {
                foreach (var dynamicGesture in possibleDynamicGesturesForRightHand)
                {
                    if (sequenceIndexRightHand == dynamicGesture.gestures.Count && performedDynamicGestureRightHand.Equals(new DynamicGesture()))
                    {
                        dynamicGesture.onRecognized?.Invoke();
                        performedDynamicGestureRightHand = dynamicGesture;
                        
                    }
                }
            }

            if (performedDynamicGestureRightHand.Equals(new DynamicGesture()) && increaseIndex)
                sequenceIndexRightHand++;

        }

        else
        {
            if (!performedDynamicGestureRightHand.Equals(new DynamicGesture()))
            {
                performedDynamicGestureRightHand.onRecognizedEnd?.Invoke();
                ResetDynamicGestureRecognitionRightHand();
            }
        }
    }

}
