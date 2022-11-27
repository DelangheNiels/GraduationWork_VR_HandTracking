using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Spell : MonoBehaviour
{
    [SerializeField]
    protected string spellName;
    [SerializeField]
    protected float manaCost;
    [SerializeField]
    protected GameObject spellObject;

    [SerializeField]
    public abstract void Cast();

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
