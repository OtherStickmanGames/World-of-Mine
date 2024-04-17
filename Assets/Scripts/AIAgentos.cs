using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AIAgentos : MonoBehaviour
{
    NavMeshAgent m_Agent;
    Player player;
    RaycastHit m_HitInfo = new RaycastHit();

    void Start()
    {
        m_Agent = GetComponent<NavMeshAgent>();
        player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        //if (Input.GetKeyDown(KeyCode.O))
        {
            if (m_Agent.isOnNavMesh)
                m_Agent.destination = player.transform.position;
        }
    }
}
