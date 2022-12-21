using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandSkeletonDebugInformation : MonoBehaviour
{

    [SerializeField] private OVRHand hand;

    [SerializeField] private OVRSkeleton skeleton;

    // Start is called before the first frame update
    void Start()
    {
        if (!hand)
            hand = GetComponent<OVRHand>();

        if (!skeleton)
            skeleton = GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        //if(Input.GetKeyDown(KeyCode.Space) && hand.IsTracked)
        //{
        //    DisplayBoneInfo();
        //}
    }

    void DisplayBoneInfo()
    {
        Debug.Log("--------------------------------------------------");
        foreach (var bone in skeleton.Bones)
        {
            Debug.Log(skeleton.GetSkeletonType() + ": boneID-> " + bone.Id + " | pos-> " + bone.Transform.position);
        }

        Debug.Log("--------------------------------------------------");
    }
}
