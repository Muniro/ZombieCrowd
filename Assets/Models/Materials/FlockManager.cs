using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlocakManager : MonoBehaviour
{
    // Start is called before the first frame update

    public GameObject fishPrefab;
    public int numFish = 20;
    public GameObject[] allFish;
    public Vector3 swimLimits = new Vector3(5,5,5);
    void Start()
    {
        allFish = new GameObject[numFish];

        for(int i = 0; i < numFish; i++)
        {
            
            Vector3 pos = this.transform.position + 
            new Vector3(Random.Range(-swimLimits.x, swimLimits.x),
            Random.Range(-swimLimits.y, swimLimits.y),
            Random.Range(-swimLimits.z, swimLimits.z));

            allFish[i] = (GameObject) Instantiate(fishPrefab, pos, Quaternion.identity);
        }
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
