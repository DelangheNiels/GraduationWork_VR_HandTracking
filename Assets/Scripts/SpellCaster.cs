using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;

public class SpellCaster : MonoBehaviour
{

    public enum Spells { fireball, electricity, fireBeam, none};

    [SerializeField]
    private OVRSkeleton leftHand;
    [SerializeField]
    private OVRSkeleton rightHand;

    [SerializeField]
    float maxDistanceToCastBothHandsSpells;

    int palmIndex;
    bool isPalmIndexSet;

    bool areHandsTogether;

    //Spels________________________
    [SerializeField] private GameObject fireball;

    [SerializeField] private GameObject electricity;

    [SerializeField] private GameObject fireBeam;

    private List<KeyValuePair<string, GameObject>> spells;
    private List<KeyValuePair<string, Spells>> handsSpellList;

    private GameObject CastedSpellForBothHands;

    // Start is called before the first frame update
    void Start()
    {
        CastedSpellForBothHands = null;

        spells = new List<KeyValuePair<string, GameObject>>();
        handsSpellList = new List<KeyValuePair<string, Spells>>();

        handsSpellList.Add(new KeyValuePair<string, Spells>("left", Spells.none));
        handsSpellList.Add(new KeyValuePair<string, Spells>("right", Spells.none));

        palmIndex = -1;
        isPalmIndexSet = false;
        areHandsTogether = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Find index of hand palm
        FindpalmIndex();

        CheckHandsDistance();

    }

    private Vector3 GetCastPosition(string hand)
    {
        Vector3 position = new Vector3();

        if (palmIndex > -1)
        {
            switch (hand)
            {
                case "left":
                    position = leftHand.Bones[palmIndex].Transform.position;
                    break;
                case "right":
                    position = rightHand.Bones[palmIndex].Transform.position;
                    break;
                default:
                    break;
            }
        }

        return position;
    }

    private void FindpalmIndex()
    {
        if (!isPalmIndexSet)
        {
            if (leftHand.Bones.Count != 0)
            {
                for (int i = 0; i < leftHand.Bones.Count; i++)
                {
                    if (leftHand.Bones[i].Id == OVRSkeleton.BoneId.Body_LeftHandPalm)
                    {
                        palmIndex = i;
                        isPalmIndexSet = true;
                    }
                }
            }


        }
    }

    private void CheckHandsDistance()
    {
        if (isPalmIndexSet)
        {
            var distance = Mathf.Abs(leftHand.Bones[palmIndex].Transform.position.x - rightHand.Bones[palmIndex].Transform.position.x);

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

        if (hand.Equals("left"))
        {
            pos.x -= 0.2f;
            pos.z += 1.0f;
            pos.y += 1.2f;
        }

        if (hand.Equals("right"))
        {
            pos.x += 0.3f;
            pos.y += 1;
            pos.z += 0.8f;
        }

        Instantiate(fireball, pos, Quaternion.identity);
        SetCastedSpellForHand(hand, Spells.fireball);
    }

    public void CastElectricitySpell(string hand)
    {
        Vector3 pos = GetCastPosition(hand);

        if (hand.Equals("left"))
        {
            pos.x -= 0.2f;
            pos.z += 1.0f;
            pos.y += 1.0f;
        }

        if (hand.Equals("right"))
        {
            pos.x += 0.3f;
            pos.y += 1;
            pos.z += 1.0f;
        }


        var spell = Instantiate(electricity, pos, Quaternion.identity);
        spells.Add(new KeyValuePair<string, GameObject>(hand, spell));
        SetCastedSpellForHand(hand, Spells.electricity);
    }

    public void CastFireBeamSpell(string hand)
    {
        Debug.Log("--------------both hands: " + hand + " ---------------------");
        SetCastedSpellForHand(hand, Spells.fireBeam);

        Vector3 pos = new Vector3();
        if (areHandsTogether && AreBothHandsCastingSameSpell(Spells.fireBeam) && CastedSpellForBothHands == null)
        {
            CastedSpellForBothHands = Instantiate(fireBeam, pos, Quaternion.identity);

        }

    }

    public void ClearSpells(string hand)
    {
        Debug.Log("------------- Clearing spells --------------------------");

        if (hand == "both" && CastedSpellForBothHands != null)
        {
            Destroy(CastedSpellForBothHands);
            CastedSpellForBothHands = null;
            Debug.Log("-------------------Destroying both hands spell-------------------");

            SetCastedSpellForHand("left", Spells.none);
            SetCastedSpellForHand("right", Spells.none);
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


