
using UnityEngine;

public class Lever : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject target;

    private bool isOn;

    public void Interact()
    {
        isOn = !isOn;
        target.SetActive(isOn);
    }
}
