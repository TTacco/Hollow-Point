using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace HollowPoint
{
    class Component_EnemyBelow : MonoBehaviour
    {
        GameObject transformSlave;
        BoxCollider2D bx;

        public void Awake()
        {
            bx = gameObject.AddComponent<BoxCollider2D>();
            bx.enabled = false;
            bx.isTrigger = true;
            bx.size = new Vector2(50, 50);
            bx.offset = new Vector2(0, 0);
        }

        public void Start()
        {    
            StartCoroutine(IgnoreCollision());
        }

        public IEnumerator IgnoreCollision()
        {
            do
            {
                yield return null;
            }
            while (HeroController.instance == null || GameManager.instance == null);
            bx.enabled = true;
            //gameObject.transform.parent = HeroController.instance.transform;
            Physics2D.IgnoreCollision(bx, HeroController.instance.GetComponent<BoxCollider2D>(), true);
            gameObject.transform.SetParent(transformSlave.transform);

            DontDestroyOnLoad(transformSlave);
        }

        public void Update()
        {
            transformSlave.transform.position = HeroController.instance.transform.position + new Vector3(0, -20, -0.1f);
        }

        public void FixedUpdate()
        {
        }

        void OnTriggerStay2D(Collider2D col)
        {
            Modding.Logger.Log("Staying in contact " + col.name);
        }

    }
}
