using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;
using System;

public class SpellCaster : MonoBehaviour
{
    [System.Serializable]
    public enum Hands { left, right , both };
    public enum Spells { fireball, electricity, fireBeam, electricityBeam,none};

    [SerializeField]
    private OVRSkeleton leftHand;
    [SerializeField]
    private OVRSkeleton rightHand;

    [SerializeField]
    float maxDistanceToCastBothHandsSpells;

    bool areHandsTogether;

    bool hasStarted;

    List<OVRBone> rightHandFingerBones = new List<OVRBone>();
    List<OVRBone> leftHandFingerBones = new List<OVRBone>();

    //Spels________________________
    [SerializeField] private GameObject fireball;

    [SerializeField] private GameObject electricity;

    [SerializeField] private GameObject fireBeam;

    [SerializeField] private GameObject electricityBeam;

    private List<KeyValuePair<string, GameObject>> spells;
    private List<KeyValuePair<string, Spells>> handsSpellList;

    private List<GameObject> CastedSpellsForBothHands;

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
        rightHandFingerBones = new List<OVRBone>(rightHand.Bones);
        leftHandFingerBones = new List<OVRBone>(leftHand.Bones);
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayRoutine(2.5f, Initialize));
        CastedSpellsForBothHands = new List<GameObject>();

        spells = new List<KeyValuePair<string, GameObject>>();
        handsSpellList = new List<KeyValuePair<string, Spells>>();

        handsSpellList.Add(new KeyValuePair<string, Spells>("left", Spells.none));
        handsSpellList.Add(new KeyValuePair<string, Spells>("right", Spells.none));

        areHandsTogether = false;
    }

    // Update is called once per frame
    void Update()
    {
        CheckHandsDistance();

    }

    private Vector3 GetCastPosition(string hand)
    {
        Vector3 position = new Vector3();

        if (hasStarted)
        {
            switch (hand)
            {
                case "left":
                    position = leftHand.transform.localPosition;
                    break;
                case "right":
                    position = rightHand.transform.localPosition;
                    break;
                default:
                    break;
            }
        }

        return position;
    }

    private void CheckHandsDistance()
    {
        if (hasStarted)
        {
            var distance = Mathf.Abs(leftHand.transform.localPosition.x - rightHand.transform.localPosition.x);

            if (distance < maxDistanceToCastBothHandsSpells)
            {
                areHandsTogether = true;
            }

            else
            {
                areHandsTogether = false;
            }
        }
    }

    private bool AreBothHandsCastingSameSpell(Spells spell)
    {
        bool result = handsSpellList[0].Value == spell && handsSpellList[1].Value == spell;
        return result;
    }

    private void SetCastedSpellForHand(string hand, Spells spell)
    {
        for(int i=0; i< handsSpellList.Count; i++)
        {
            if(handsSpellList[i].Key == hand)
            {
                handsSpellList[i] = new KeyValuePair<string, Spells>(hand, spell);
            }
        }
    }

    //-----------------------------------------------------------------------//
    //SPELLS
    public void CastFireBallSpell(string hand)
    {
        Vector3 pos = GetCastPosition(hand);
        pos.z += 1;

        Instantiate(fireball, pos, Quaternion.identity);
        SetCastedSpellForHand(hand, Spells.fireball);
    }

    public void CastElectricitySpell(string hand)
    {
        Vector3 pos = GetCastPosition(hand);

        var spell = Instantiate(electricity, pos, Quaternion.identity);
        spells.Add(new KeyValuePair<string, GameObject>(hand, spell));

        SetCastedSpellForHand(hand, Spells.electricity);
    }

    public void CastFireBeamSpell(string hand)
    {
        SetCastedSpellForHand(hand, Spells.fireBeam);

        if (areHandsTogether && AreBothHandsCastingSameSpell(Spells.fireBeam) && CastedSpellsForBothHands.Count == 0)
        {
            Vector3 posLeftHand = GetCastPosition("left");
            Vector3 posRightHand = GetCastPosition("right");

            Vector3 pos = posLeftHand;
            pos.x = (posLeftHand.x + posRightHand.x) / 2;
            pos.z -= 0.5f;
           
            CastedSpellsForBothHands.Add(Instantiate(fireBeam, pos, Quaternion.identity));

        }

    }

    public void CastElectricityBeamSpell(string hand)
    {
        SetCastedSpellForHand(hand, Spells.electricityBeam);

        if (areHandsTogether && AreBothHandsCastingSameSpell(Spells.electricityBeam) && CastedSpellsForBothHands.Count == 0)
        {
            Vector3 posLeftHand = GetCastPosition("left");
            Vector3 posRightHand = GetCastPosition("right");

            Vector3 pos = posLeftHand;
            pos.x = (posLeftHand.x + posRightHand.x) / 2;

            CastedSpellsForBothHands.Add(Instantiate(electricityBeam, pos, Quaternion.identity));
            CastedSpellsForBothHands.Add(Instantiate(electricityBeam, pos, Quaternion.identity));
            CastedSpellsForBothHands.Add(Instantiate(electricityBeam, pos, Quaternion.identity));
            CastedSpellsForBothHands.Add(Instantiate(electricityBeam, pos, Quaternion.identity));

        }
    }

    public void ClearSpells(string hand)
    {

        if (CastedSpellsForBothHands.Count != 0)
        {
           foreach(var spell in CastedSpellsForBothHands)
            {
                Destroy(spell);
            }
            CastedSpellsForBothHands.Clear();

            SetCastedSpellForHand(hand, Spells.none);
        }

        else
        {
            List<GameObject> spellsToDestroy = new List<GameObject>();

            for (int i = 0; i < spells.Count; i++)
            {
                if (spells[i].Key == hand)
                {
                    spellsToDestroy.Add(spells[i].Value);
                    spells.Remove(spells[i]);
                }
            }

            for (int i = 0; i < spellsToDestroy.Count; i++)
            {
                Destroy(spellsToDestroy[i]);
            }

            SetCastedSpellForHand(hand, Spells.none);
        }

    }
}


