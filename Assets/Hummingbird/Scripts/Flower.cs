using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages a single flower with nectar
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is full."), SerializeField]
    private Color _fullFlowerColor = new Color(1f, 0f, 0.3f);

    [Tooltip("The color when the flower is full."), SerializeField]
    private Color _emptyFlowerColor = new Color(0.5f, 0f, 1f);

    /// <summary>
    /// Trigger to represent the nectar
    /// </summary>
    [HideInInspector]
    public Collider NectarCollider;

    //Collider for petals
    private Collider _flowerCollider;

    //Flower's material
    private Material _flowerMat;

    /// <summary>
    /// Vector pointing straight out of the flower
    /// </summary>
    public Vector3 FlowerUpVector
    {
        get
        {
            return NectarCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the nectar collider
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return NectarCollider.transform.position;
        }
    }

    /// <summary>
    /// The amount of nectar remaining in the flower
    /// </summary>
    public float NectarAmount { get; private set; }

    /// <summary>
    /// Used to check if there is any nectar
    /// </summary>
    public bool HasNectar
    {
        get { return NectarAmount > 0f; }
    }

    /// <summary>
    /// Attempts to consume nectar from the flower. 
    /// </summary>
    /// <param name="amount">The amount of nectar to remove</param>
    /// <returns>The actual amount successfully removed</returns>
    public float Feed(float amount)
    {
        float nectarTaken = Mathf.Clamp(amount, 0f, NectarAmount);
        NectarAmount -= amount;

        if(NectarAmount <= 0)
        {
            NectarAmount = 0;

            _flowerCollider.gameObject.SetActive(false);
            NectarCollider.gameObject.SetActive(false);

            _flowerMat.SetColor("_BaseColor", _emptyFlowerColor);
        }

        return nectarTaken;
    }

    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        NectarAmount = 1f;

        _flowerCollider.gameObject.SetActive(true);
        NectarCollider.gameObject.SetActive(true);

        _flowerMat.SetColor("_BaseColor", _fullFlowerColor);
    }

    private void Awake()
    {
        //Get flower material
        if(gameObject.TryGetComponent(out MeshRenderer meshRenderer))
        {
            _flowerMat = meshRenderer.material;
        }

        _flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        NectarCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
