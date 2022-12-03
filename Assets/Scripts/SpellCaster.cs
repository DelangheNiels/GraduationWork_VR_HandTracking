using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction.Input;

public class SpellCaster : MonoBehaviour
{
    [SerializeField]
    private OVRSkeleton leftHand;
    [SerializeField]
    private OVRSkeleton rightHand;

    [SerializeField]
    private Hand leftH;
    [SerializeField]
    private Hand rightH;

    [SerializeField]
    private GameObject fireball;

    [SerializeField]
    private GameObject electricity;

    private List<KeyValuePair<string, GameObject>> spells;

    public void CastFireBallSpell(string hand)
    {
        Vector3 pos = GetCastPosition(hand);
        pos.z +=1;

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
        spells.Add(new KeyValuePair<string, GameObject>(hand,spell));

    }

    public void ClearSpells(string hand)
    {
        List<GameObject> spellsToDestroy = new List<GameObject>();
       
        for (int i=0; i< spells.Count; i++)
        {
            if(spells[i].Key == hand)
            {
                spellsToDestroy.Add(spells[i].Value);
                spells.Remove(spells[i]);
            }
        }

        for(int i=0; i< spellsToDestroy.Count; i++)
        {
            Destroy(spellsToDestroy[i]);
        }

    }

    private Vector3 GetCastPosition(string hand)
    {
        Vector3 position = new Vector3();

        switch (hand)
        {
            case "left":
                position = leftH.transform.position;
                break;
            case "right":
                position = rightH.transform.position;
                break;
            default:
                break;
        }
       
        return position;
    }

    // Start is called before the first frame update
    void Start()
    {
        spells = new List<KeyValuePair<string, GameObject>>();
    }

    // Update is called once per frame
    void Update()
    {
       
    }
}
