using UnityEngine;

/// <summary>
///     Simple, non-physics parabolic flight.
///     Destroys itself on arrival.
/// </summary>
public class ArcingProjectile : MonoBehaviour
{
	// internal state
	private Vector3 _start;
	private Vector3 _end;
	
	private float _duration;
	private float _elapsed;
	private float _height;
	
	private void Update()
	{
		if (_elapsed >= _duration) return;

		_elapsed += Time.deltaTime;
		var t = Mathf.Clamp01(_elapsed / _duration);

		// Base LERP between start and end
		var pos = Vector3.Lerp(_start, _end, t);

		// Add arc offset
		pos.y += Mathf.Sin(Mathf.PI * t) * _height;
		transform.position = pos;

		// Face direction of travel
		if (t < 1f)
		{
			var nextT = Mathf.Clamp01(t + 0.01f);
			var nextPos = Vector3.Lerp(_start, _end, nextT);
			nextPos.y += Mathf.Sin(Mathf.PI * nextT) * _height;
			transform.forward = (nextPos - pos).normalized;
		}

		// Arrive & self-destruct
		if (_elapsed >= _duration);
			//pool release
	}

	/// <remarks>
	///     Called immediately after instantiation.
	/// </remarks>
	public void Launch(Vector3 start, Vector3 end, float speed, float height)
	{
		_start = start;
		_end = end;
		_height = height;
		_elapsed = 0f;

		// calculate flight time by distance to target
		speed = Mathf.Max(0.01f, speed);
		_duration = Vector3.Distance(start, end) / speed;
	}
}
