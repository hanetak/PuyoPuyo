using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle : MonoBehaviour
{
    Rigidbody2D _rigidbody2D;
    SpriteRenderer _spriteRenderer;
    IEnumerator Start()
    {
        _rigidbody2D = this.GetComponent<Rigidbody2D>();
        _spriteRenderer = this.GetComponent<SpriteRenderer>();
        // 移動方向と速さをランダムに決める
        float dir = Random.Range(0, 359);
        float spd = Random.Range(5.0f, 10.0f);
        SetVelocity(dir, spd);

        // 見えなくなるまで小さくする
        while (transform.localScale.x > 0.01f)
        {
            yield return new WaitForSeconds(0.01f);
            // だんだん小さくする
            transform.localScale *= 0.9f;
            // だんだん減速する
            _rigidbody2D.velocity *= 0.9f;
        }

        // 消滅
        Destroy(this.gameObject);
    }

    void SetVelocity(float direction, float speed)
    {
        Vector2 v;
        v.x = Mathf.Cos(Mathf.Deg2Rad * direction);
        v.y = Mathf.Sin(Mathf.Deg2Rad * direction);
        _rigidbody2D.velocity = v;
    }
}
