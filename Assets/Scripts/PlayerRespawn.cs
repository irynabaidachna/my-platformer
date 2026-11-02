
using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Vector3 checkpoint;

    private void Awake()
    {
        checkpoint = transform.position;
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpoint = position;
    }

    public void Respawn()
    {
        transform.position = checkpoint;
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
    }
}
