using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public struct Gesture
{
    public string name;
    public List<Vector3> fingerData;
    public UnityEvent OnRecognized;

}

public class GestureDetection : MonoBehaviour
{
    [SerializeField]
    private OVRSkeleton skeleton;

    private List<OVRBone> fingerBones;

    [SerializeField]
    private List<Gesture> gestures;

    [SerializeField]
    bool debugMode = true;

    [SerializeField]
    float threshold = 0.1f;

    Gesture previousGesture = new Gesture();

    // Start is called before the first frame update
    void Start()
    {
        //set all fingers of hand
        fingerBones = new List<OVRBone>(skeleton.Bones);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Space) && debugMode)
        {
            Save();
        }

        //Check if gesture exitst
        Gesture currentGesture = Recognize();
        bool isRecognized = !currentGesture.Equals(new Gesture());
        //Check if it is a new gesture
        if(isRecognized && !currentGesture.Equals(previousGesture))
        {
            Debug.Log("New Gesture found: " + currentGesture.name);
            previousGesture = currentGesture;
            currentGesture.OnRecognized.Invoke();
        }
        
    }

    void Save()
    {
        Gesture gesture = new Gesture();
        gesture.name = "New Gesture";
        List<Vector3> data = new List<Vector3>();
        foreach(var bone in fingerBones)
        {
            //finger position relative to root
            data.Add(skeleton.transform.InverseTransformPoint(bone.Transform.position));
        }

        gesture.fingerData = data;
        gestures.Add(gesture);
    }

    Gesture Recognize()
    {
        Gesture currentGesture = new Gesture();
        float currentMinDistance = Mathf.Infinity;

        foreach(var gesture in gestures)
        {
            float totalDistance = 0;
            bool isDiscarded = false;

            for(int i=0; i < fingerBones.Count; i++)
            {
                Vector3 currentData = skeleton.transform.InverseTransformPoint(fingerBones[i].Transform.position);
                float distance = Vector3.Distance(currentData, fingerBones[i].Transform.position);

                if(distance > threshold)
                {
                    isDiscarded = true;
                    break;
                }

                totalDistance += distance;
            }

            if(!isDiscarded && totalDistance < currentMinDistance)
            {
                //get most propable gesture
                currentMinDistance = totalDistance;
                currentGesture = gesture;
            }
        }

        return currentGesture;
    }
}
