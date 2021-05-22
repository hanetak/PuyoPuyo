using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global
{
    //初期化
    public static void Init()
    {
        _score = 0;
    }
    //スコア
    static int _score;
    public static int Score
    {
        get { return _score; }
    }

    //連鎖
    static int _chain;
    public static int Chain
    {
        get { return _chain; }
    }

    //スコア計算(消したぷよの個数×（連鎖ボーナス＋連結ボーナス＋色数ボーナス）×10 )
    public static void ScoreAdd(int chain, int conect, int color)
    {
        _chain = chain;
        int chainBouns = 0;
        int conectBouns = 0;
        int colorBouns = 0;
        //連鎖ボーナス
        if (chain < 5)
        {
            chainBouns = chain < 4 ? (chain - 1) * 8 : 32;
        }
        else
        {
            chainBouns = (chain - 4) * 32 + 32;
        }
        //連結ボーナス
        conectBouns = conect < 5 ? 0 : (conect - 4) + 1;

        //色数ボーナス
        for (int i = 0; i < color; i++)
        {
            colorBouns += (3 * i);
        }

        Debug.Log(chainBouns + "," + conectBouns + "," + colorBouns);
        _score += conect * Mathf.Max(1, (chainBouns + conectBouns + colorBouns)) * 10;
    }

    // Start is called before the first frame update

    // Update is called once per frame
    void Update()
    {

    }
}
