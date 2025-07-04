using System;
using System.Buffers;
using UnityEngine;
using UnityEngine.Pool;
public class ShootOverTime : MonoBehaviour
{
	[Header("Projectile")]
	[SerializeField] private ArcingProjectile ProjectilePrefab;
	[SerializeField] private Transform ProjectileStartPoint;

	// Replace this with some kind of targeted enemy position
	[SerializeField] private Transform DebugEndPoint;

	[Header("Flight Settings")]
	[SerializeField][Min(0.1f)] private float ProjectileSpeed = 5f; // units / second
	[SerializeField] private float ArcHeight = 2f; // world units

	public ObjectPool<ArcingProjectile> ArrowPool;

    /// <summary>
    ///     Fires the projectile
    /// </summary>
    /// 
    public void Start()
    {
        CreatePool();
    }

    public void Shoot()
	{
        // Instantiate with generic overload so no GetComponent is needed.ArrowOnDestroy
		ArcingProjectile projectile = ArrowPool.Get();
		
        projectile.Launch(transform.position, DebugEndPoint.position, ProjectileSpeed, ArcHeight);

    }

	public void CreatePool()
	{
		ArrowPool = new ObjectPool<ArcingProjectile>(CreateFunk, ArrowOnGet, ArrowOnLoose, ArrowOnDestroy, true, 1, 10);
	}


	private ArcingProjectile CreateFunk()
	{
		return Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);

	}

	private void ArrowOnGet(ArcingProjectile projectile)
	{
		projectile.gameObject.SetActive(true);
        
    }


	private void ArrowOnLoose(ArcingProjectile projectile)
	{
		projectile.gameObject.SetActive(false);
	}

	private void ArrowOnDestroy(ArcingProjectile projectile)
	{
		Destroy(projectile.gameObject);
	}
}
