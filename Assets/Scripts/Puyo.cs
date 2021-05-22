using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Puyo : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    public Sprite[] color = new Sprite[5];
    void Awake()
    {
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetState(GameManager.ePuyoState ePuyo)
    {
        switch (ePuyo)
        {
            case GameManager.ePuyoState.eNone:
                spriteRenderer.sprite = null;
                break;
            case GameManager.ePuyoState.eWall:
                spriteRenderer.sprite = null;
                break;
            case GameManager.ePuyoState.eRed:
                spriteRenderer.sprite = color[1];
                break;
            case GameManager.ePuyoState.eBlue:
                spriteRenderer.sprite = color[2];
                break;
            case GameManager.ePuyoState.eYellow:
                spriteRenderer.sprite = color[3];
                break;
            case GameManager.ePuyoState.eGreen:
                spriteRenderer.sprite = color[4];
                break;

        }

    }
}
