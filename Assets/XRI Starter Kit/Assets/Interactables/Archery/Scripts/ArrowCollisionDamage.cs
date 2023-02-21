using System;
using System.Collections;
using System.Collections.Generic;
using MikeNspired.UnityXRHandPoser;
using UnityEngine;

public class ArrowCollisionDamage : SimpleCollisionDamage
{
    private Rigidbody rb;
    private bool canDamage = true;
    private void Awake() => rb = GetComponent<Rigidbody>();

    public void AdjustDamage(float power) => damage *= power;

    protected override void Damage(IDamageable damageable)
    {
        if (!canDamage) return;
        damageable.TakeDamage(damage, gameObject);
        canDamage = false;
    }
}