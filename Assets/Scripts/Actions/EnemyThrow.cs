using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyThrow : MonoBehaviour
{
    public Transform enemy;
    public Transform attackPoint;
    public GameObject objectToThrow;
    private GameObject projectile;
    private Transform target;
    private ParticleSystem ps;

    public float throwCooldown;

    //public KeyCode throwKey = KeyCode.Mouse0;
    public float throwForce;
    public float throwUpwardForce;

    public bool readyToThrow;
    public bool isThrown, isSpawned, isTargetSet;

    private void Start()
    {
        readyToThrow = true;
        isThrown = false;
        isSpawned = false;
        isTargetSet = false;
    }

    private void Update()
    {

    }

    public void SetTarget(Transform _target)
    {
        Debug.Log("Set Throwable Target");
        isTargetSet = true;
        target = _target;
    }

    public void SetTargetBool(bool _value) => isTargetSet = _value;

    public void FocusOnTarget()
    {
        Debug.Log("Focus on target");
        projectile.transform.LookAt(target);
    }

    public void Spawn()
    {
        Debug.Log("Spawn Throwable");
        isSpawned = true;
        isThrown = false;

        projectile = Instantiate(objectToThrow, attackPoint.position, enemy.rotation);
        projectile.transform.up = projectile.transform.forward;
        projectile.transform.SetParent(enemy.transform);
    }

    public void Charge()
    {
        Debug.Log("Charge Throwable");
        readyToThrow = true;
        ps = projectile.GetComponent<ParticleSystem>();
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        var main = ps.main;
        main.startSpeed = 0.4f;
    }

    public void ThrowObject()
    {
        if (isSpawned && readyToThrow)
        {
            readyToThrow = false;
            Debug.Log("Throw Throwable");

            //instantiate object to throw

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            var main = ps.main;
            main.startSpeed = 4f;

            //get rigidbody component
            Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

            isThrown = true;

            //Add force
            Vector3 forceToAdd = (projectile.transform.up * throwForce) + (transform.up * throwUpwardForce);

            projectileRb.AddForce(forceToAdd, ForceMode.Impulse);
        }
    }
}
