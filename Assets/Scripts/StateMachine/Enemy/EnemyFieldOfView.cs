
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EFOV
{
    public class FieldOfView : MonoBehaviour
    {
        public float viewRadius;
        [UnityEngine.Range(0, 360)]
        public float viewAngle;
        private Camera _camera;

        public LayerMask targetMask;
        public LayerMask obstacleMask;

        public List<Transform> visibleTargets = new();
        public List<Transform> currentVisibleTargets = new();
        public List<RaycastHit> currentHitData = new();
        public EnemyControllerContext _context;
        public EnemyProperties _enemyProperties;

        public bool isVisionInitialized = false;
        private Vector3 _rayStart;
        private Vector3 _rayDir;
        Ray ray;

        public void InitializeFOV(EnemyControllerContext context, EnemyProperties enemyProperties)
        {
            _context = context;
            //visibleTargets = _context.visibleTargets;
            _enemyProperties = enemyProperties;
            isVisionInitialized = true;
        }

        public void FindVisibleTargets()
        {
            Debug.Log("Looking for Player");
            currentHitData.Clear();
            currentVisibleTargets.Clear();
            int PlayerLayer = LayerMask.GetMask("Player");
            Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, _enemyProperties.viewRadius, _enemyProperties.targetMask);

            for (int i = 0; i < targetsInRadius.Length; i++)
            {
                Transform target = targetsInRadius[i].transform;
                //_rayStart = transform.position;
                Vector3 dirToTarget = (target.position - transform.position).normalized;

                if (Vector3.Angle(transform.forward, dirToTarget) < _enemyProperties.viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(transform.position, target.position);
                    //_rayDir = dirToTarget * dstToTarget;

                    ray = new Ray(transform.position, dirToTarget);
                    RaycastHit _hitData = new();
                    _rayStart = ray.origin;
                    _rayDir = ray.direction * dstToTarget;
                    if (Physics.Raycast(ray, out _hitData, dstToTarget, _enemyProperties.targetMask.value, QueryTriggerInteraction.Collide))
                    {
                        if (!currentHitData.Contains(_hitData)) { currentHitData.Add(_hitData); }

                        if (!currentVisibleTargets.Contains(target)) { currentVisibleTargets.Add(target); }
                    }
                }
            }

            visibleTargets = currentVisibleTargets.ToList();
            _context._visibleTargets = currentHitData.ToList();
            Debug.Log("Players in View" + _context._visibleTargets.Count);
        }

        public void FindAllTargets()
        {
            currentHitData.Clear();
            currentVisibleTargets.Clear();
            Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, _enemyProperties.viewRadius, _enemyProperties.targetMask);
            Collider[] obstaclesInRadius = Physics.OverlapSphere(transform.position, _enemyProperties.viewRadius, _enemyProperties.obstacleMask);
            targetsInRadius = targetsInRadius.Concat(obstaclesInRadius).ToArray();

            for (int i = 0; i < targetsInRadius.Length; i++)
            {
                Transform target = targetsInRadius[i].transform;
                _rayStart = transform.position;
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                _rayDir = dirToTarget;
                if (Vector3.Angle(transform.forward, dirToTarget) < _enemyProperties.viewAngle / 2)
                {
                    float dstToTarget = Vector3.Distance(transform.position, target.position);
                    //Debug.DrawRay(_rayStart, (_rayDir *dstToTarget), Color.yellow, 1f, false);
                    Ray ray = new(transform.position, dirToTarget);
                    RaycastHit _hitData = new();
                    if (Physics.Raycast(ray, out _hitData, dstToTarget, _enemyProperties.targetMask |= 1 << _enemyProperties.obstacleMask))
                    {
                        if (!currentHitData.Contains(_hitData)) { currentHitData.Add(_hitData); }

                        if (!currentVisibleTargets.Contains(target)) { currentVisibleTargets.Add(target); }

                        _context._visibleTargets.Add(_hitData);
                        visibleTargets.Add(target);
                    }
                }
            }

            visibleTargets = currentVisibleTargets.ToList();
            _context._visibleTargets = currentHitData.ToList();
        }

        public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
        {
            if (!angleIsGlobal)
            {
                angleInDegrees += transform.eulerAngles.y;
            }

            return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
        }

        void OnDrawGizmos()
        {
            if (isVisionInitialized)
            {
                //Handles.color = Color.white;
                //Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360, _enemyProperties.viewRadius);
                //Vector3 viewAngleA = DirFromAngle(-_enemyProperties.viewAngle / 2, false);
                //Vector3 viewAngleB = DirFromAngle(_enemyProperties.viewAngle / 2, false);

                //Handles.DrawLine(transform.position, transform.position + viewAngleA * _enemyProperties.viewRadius);
                //Handles.DrawLine(transform.position, transform.position + viewAngleB * _enemyProperties.viewRadius);

                //Handles.color = Color.red;
                //foreach (Transform visibleTarget in visibleTargets)
                //{
                //    Handles.DrawLine(transform.position, visibleTarget.position);
                //}
                //Debug.DrawRay(_rayStart, _rayDir, Color.yellow, 0.1f, false);
                //Debug.DrawRay(ray.origin, _rayDir, Color.yellow, 0.1f, false);
            }
        }
    }
}
