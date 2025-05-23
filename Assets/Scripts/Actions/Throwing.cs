using System.Collections;
using UnityEngine;

public class Throwing : MonoBehaviour
{
    public Transform cam;
    public Transform attackPoint;
    public GameObject objectToThrow;
    public GameObject projectile;
    public int totalThrows;
    public float throwCooldown;

    public KeyCode throwKey = KeyCode.Mouse0;
    public float throwForce;
    public float throwUpwardForce;

    public bool readyToThrow;

    public float destroyTimer = 5f;

    private void Start() => readyToThrow = true;

    private void Update()
    {
        if (Input.GetKeyDown(throwKey) && readyToThrow && totalThrows > 0)
        {
            Throw();
            StartCoroutine(DestroyWithDelay());
        }
    }

    private GameObject Throw()
    {
        readyToThrow = false;

        //instantiate object to throw
        projectile = Instantiate(objectToThrow, attackPoint.position, cam.rotation);
        projectile.transform.up = projectile.transform.forward * throwForce * Time.deltaTime;

        //get rigidbody component
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        //Add force
        Vector3 forceToAdd = (cam.transform.forward * throwForce) + (transform.up * throwUpwardForce);

        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);

        totalThrows--;

        // implement throwCooldown
        Invoke(nameof(ResetThrows), throwCooldown);
        return projectile;
    }

    private void ResetThrows() => readyToThrow = true;

    private IEnumerator DestroyWithDelay()
    {
        yield return new WaitForSeconds(destroyTimer);

        Destroy(projectile);
    }
}
