using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class NewBehaviourScript : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject obstacle;
    GameObject[] agents;
    void Start()
    {
        agents = GameObject.FindGameObjectsWithTag("agent");
    }

    // Update is called once per frame
    void Update()
    {
        if(Mouse.current == null)
            return;
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            RaycastHit hitInfo;

            Vector2 mousePos = Mouse.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if(Physics.Raycast(ray, out hitInfo))
            {
                Instantiate(obstacle, hitInfo.point, obstacle.transform.rotation );
                foreach(GameObject a in agents)
                {
                    
                }
            }
        }

    }
}
