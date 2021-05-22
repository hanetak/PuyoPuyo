using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private AudioSource audio;
    //効果音
    public AudioClip sRotate;
    public AudioClip sFall;
    public AudioClip sVanish;

    //パーティクル
    public GameObject Particle;
    //ゲームの状況
    public enum eGameState
    {
        eMove,//ぷよ移動中
        eLock,//ぷよ断離
        eGameover,//スコア計算
    }
    eGameState _state = eGameState.eMove;

    //ぷよ 
    [SerializeField] GameObject _puyoPrefab = null;
    //Nextぷよ
    public GameObject[] _puyosPrefabNext = new GameObject[4];

    //chainテキスト
    public GameObject _textChain = null;

    //ゲームオーバーテキスト
    public GameObject _textGameover = null;

    const int FIELD_SIZE_X = 8;
    const int FIELD_SIZE_Y = 14;

    const int DEFAULT_MOVE_X = 3;
    const int DEFAULT_MOVE_Y = 13;

    const int MOVE_SIZE_X = 3;
    const int MOVE_SIZE_Y = 3;




    //ぷよの種類
    public enum ePuyoState
    {
        eNone,
        eWall,
        eRed,
        eBlue,
        eYellow,
        eGreen,
        eMax
    }

    //出現するぷよのリスト
    private ePuyoState[,] _nextPuyo = new ePuyoState[3, 2];
    //ぷよの実態
    private GameObject[,] _fieldPuyosObject = new GameObject[FIELD_SIZE_X, FIELD_SIZE_Y];
    private GameObject[,] _nextPuyosObject = new GameObject[3, 2];
    private Puyo[,] _fieldPuyosScript = new Puyo[FIELD_SIZE_X, FIELD_SIZE_Y];
    private Puyo[,] _nextPuyosScript = new Puyo[3, 2];
    //フィールド上にあるぷよの状態
    private ePuyoState[,] _fieldPuyoState = new ePuyoState[FIELD_SIZE_X, FIELD_SIZE_Y];
    //最終的なぷよの状態
    private ePuyoState[,] _fieldPuyoStateFinal = new ePuyoState[FIELD_SIZE_X, FIELD_SIZE_Y];

    //動作中のぷよの状態  
    private ePuyoState[,] _movePuyoState = new ePuyoState[MOVE_SIZE_X, MOVE_SIZE_Y];
    private int _movePuyoX;
    private int _movePuyoY;

    //消去する時に使うぷよの状態
    private ePuyoState[,] _erasedPuyoState = new ePuyoState[FIELD_SIZE_X, FIELD_SIZE_Y];

    //消去するぷよの位置
    private List<int> _vanishPuyosPosX = new List<int>();
    private List<int> _vanishPuyosPosY = new List<int>();
    private List<int> _vanishPuyosPosX_Total = new List<int>();
    private List<int> _vanishPuyosPosY_Total = new List<int>();

    //ぷよぷよの回転状態
    enum ePuyoAngle
    {
        e0,
        e90,
        e180,
        e270,
        eMax
    }

    ePuyoAngle _epuyoAngle;

    //探索ぷよのチェック
    static readonly bool[,] _checked_Start = new bool[FIELD_SIZE_X, FIELD_SIZE_Y];
    bool[,] _checked = new bool[FIELD_SIZE_X, FIELD_SIZE_Y];

    //落下タイマー
    const float _fallTime = 1.0f;
    float _fallTimer = _fallTime;

    // Start is called before the first frame update
    void Start()
    {
        audio = GetComponent<AudioSource>();
        // 初期状態の設定
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                //プヨの実態　（最初はNone
                GameObject newObject = GameObject.Instantiate<GameObject>(_puyoPrefab);
                Puyo newPuyo = newObject.GetComponent<Puyo>();
                _fieldPuyosObject[i, j] = newObject;
                _fieldPuyosScript[i, j] = newPuyo;
                newObject.transform.localPosition = new Vector3(i, j, 0.0f);
                _fieldPuyoState[i, j] = (i == 0 || j == 0 || i == FIELD_SIZE_X - 1 || j == FIELD_SIZE_Y) ? ePuyoState.eWall : ePuyoState.eNone;
                _fieldPuyoStateFinal[i, j] = _fieldPuyoState[i, j];
            }
        }
        SetNextPuyo();
        StartMove();
    }

    // Update is called once per frame
    void Update()
    {
        switch (_state)
        {
            case eGameState.eMove:
                UpdateMove();
                break;
            case eGameState.eLock:
                break;
            case eGameState.eGameover:
                _textGameover.SetActive(true);
                break;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {

        }

    }
    void UpdateMove()
    {
        _fallTimer -= Time.deltaTime;
        if (Input.GetKeyDown(KeyCode.A))
        {
            bool isCollision = CheckCollision(-1, 0);
            if (!isCollision)
            {
                //右に移動
                _movePuyoX--;
            }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            bool isCollision = CheckCollision(1, 0);
            if (!isCollision)
            {
                //左に移動
                _movePuyoX++;
            }
        }
        if (Input.GetKeyDown(KeyCode.S) || _fallTimer < 0)
        {
            bool isCollision = CheckCollision(0, -1);
            //床に接地
            if (isCollision)
            {
                _state = eGameState.eLock;
                MergePuyo();
                StartCoroutine(LoopPuyo());
                if (_fieldPuyoStateFinal[DEFAULT_MOVE_X, DEFAULT_MOVE_Y] != ePuyoState.eNone)
                {
                    _state = eGameState.eGameover;
                }
            }
            //下に移動
            else
            {
                _movePuyoY--;
                _fallTimer = _fallTime;
            }
        }
        //回転
        if (Input.GetKeyDown(KeyCode.Space))
        {
            bool isCollision = CheckCollisionRotate();
            if (!isCollision)
            {
                audio.PlayOneShot(sRotate);
                PuyoRotate();
            }
        }
        //ぷよの状態を更新
        UpdatePuyoState();
    }

    void UpdatePuyoState()
    {
        // ぷよの状態反映（フィールド上）
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                // ぷよの状態
                _fieldPuyoStateFinal[i, j] = _fieldPuyoState[i, j];
            }
        }


        //動作中のぷよの状態反映
        if (_state == eGameState.eMove)
        {
            for (int i = 0; i < MOVE_SIZE_X; i++)
            {
                for (int j = 0; j < MOVE_SIZE_Y; j++)
                {
                    if ((0 < _movePuyoX - 1 + i && 0 < _movePuyoY - 1 + j) && (FIELD_SIZE_X > _movePuyoX + i && FIELD_SIZE_Y >= _movePuyoY + j))
                    {
                        if (_fieldPuyoStateFinal[_movePuyoX - 1 + i, _movePuyoY - 1 + j] == ePuyoState.eNone)
                        {
                            _fieldPuyoStateFinal[_movePuyoX - 1 + i, _movePuyoY - 1 + j] = _movePuyoState[i, j];
                        }
                    }
                }
            }
        }

        // ぷよの状態反映
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 0; j < FIELD_SIZE_Y; j++)
            {
                // ぷよの状態
                _fieldPuyosScript[i, j].SetState(_fieldPuyoStateFinal[i, j]);
            }
        }

    }

    bool CheckCollision(int offsetX, int offsetY)
    {
        //動作中ぷよの進行方向を検査する
        for (int i = 0; i < MOVE_SIZE_X; i++)
        {
            for (int j = 0; j < MOVE_SIZE_Y; j++)
            {
                if (_movePuyoState[i, j] != ePuyoState.eNone)
                {
                    if ((0 <= _movePuyoX - 1 + i + offsetX) && (FIELD_SIZE_X >= _movePuyoX + i + offsetX) && (0 <= _movePuyoY - 1 + j + offsetY && FIELD_SIZE_Y >= _movePuyoY + j + offsetY))
                    {
                        if (_fieldPuyoState[_movePuyoX - 1 + i + offsetX, _movePuyoY - 1 + j + offsetY] != ePuyoState.eNone)
                        {
                            return true;
                        }
                    }
                }

            }
        }
        return false;
    }

    bool CheckCollisionRotate()
    {
        int x = 0;
        int y = 0;
        switch (_epuyoAngle)
        {

            case ePuyoAngle.e0:
                x = 0; y = 1;
                break;
            case ePuyoAngle.e90:
                x = 1; y = 0;
                break;
            case ePuyoAngle.e180:
                x = 2; y = 1;
                break;
            case ePuyoAngle.e270:
                x = 1; y = 2;
                break;
        }
        if (_fieldPuyoState[_movePuyoX - 1 + x, _movePuyoY - 1 + y] != ePuyoState.eNone)
        {
            return true;
        }
        return false;
    }

    //ぷよを回転させる
    void PuyoRotate()
    {
        _epuyoAngle = _epuyoAngle + 1 == ePuyoAngle.eMax ? ePuyoAngle.e0 : _epuyoAngle + 1;
        switch (_epuyoAngle)
        {
            case ePuyoAngle.e0:
                _movePuyoState[1, 2] = _movePuyoState[2, 1];
                _movePuyoState[2, 1] = ePuyoState.eNone;
                break;
            case ePuyoAngle.e90:
                _movePuyoState[0, 1] = _movePuyoState[1, 2];
                _movePuyoState[1, 2] = ePuyoState.eNone;
                break;
            case ePuyoAngle.e180:
                _movePuyoState[1, 0] = _movePuyoState[0, 1];
                _movePuyoState[0, 1] = ePuyoState.eNone;
                break;
            case ePuyoAngle.e270:
                _movePuyoState[2, 1] = _movePuyoState[1, 0];
                _movePuyoState[1, 0] = ePuyoState.eNone;
                break;

        }
    }

    //ぷよをくっつける
    void MergePuyo()
    {
        // 落下後のぷよをフィールドに反映
        for (int i = 0; i < MOVE_SIZE_X; i++)
        {
            for (int j = 0; j < MOVE_SIZE_Y; j++)
            {
                if (FIELD_SIZE_Y > _movePuyoY - 1 + j)
                {
                    if (_movePuyoState[i, j] != ePuyoState.eNone)
                    {
                        _fieldPuyoState[_movePuyoX - 1 + i, _movePuyoY - 1 + j] = _movePuyoState[i, j];
                    }
                }
            }
        }
    }

    //浮いているぷよを落とす
    void DropPuyo()
    {
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            //落下するぷよのリスト
            var dropList = new List<ePuyoState>();
            //積み重なってるぷよのカウント
            int count = 0;
            bool isCount = true;
            for (int j = 1; j < FIELD_SIZE_Y; j++)
            {
                //浮いているぷよをリストに格納する
                if (_fieldPuyoState[i, j] != ePuyoState.eNone && _fieldPuyoState[i, j - 1] == ePuyoState.eNone)
                {
                    dropList.Add(_fieldPuyoState[i, j]);
                    _fieldPuyoState[i, j] = ePuyoState.eNone;
                }
                //ぷよがない場合カウントをやめる
                if (_fieldPuyoState[i, j] == ePuyoState.eNone)
                {
                    isCount = false;
                }
                if (isCount)
                {
                    count++;
                }
            }
            //落下させる
            for (int j = 0; j < dropList.Count; j++)
            {
                //j = 0は床のため +1
                _fieldPuyoState[i, j + count + 1] = dropList[j];
            }
        }
    }



    //四つの塊のぷよを削除する
    void VanishPuyo()
    {
        _vanishPuyosPosX_Total = new List<int>();
        _vanishPuyosPosY_Total = new List<int>();
        _isVanish = false;
        _colors = new HashSet<ePuyoState>();
        var addPuyo = new List<int>();
        for (int i = 1; i < FIELD_SIZE_X - 1; i++)
        {
            for (int j = 1; j < FIELD_SIZE_Y; j++)
            {
                if (_fieldPuyoState[i, j] != ePuyoState.eNone && _fieldPuyoState[i, j] != ePuyoState.eWall)
                {
                    _vanishPuyosPosX = new List<int>();
                    _vanishPuyosPosY = new List<int>();
                    Array.Copy(_fieldPuyoState, _erasedPuyoState, _fieldPuyoState.Length);
                    int _count = ConnectPuyo(i, j, 0, _erasedPuyoState[i, j]);
                    if (_count >= 4)
                    {
                        _vanishPuyosPosX_Total.Add(i);
                        _vanishPuyosPosY_Total.Add(j);
                        _vanishPuyosPosX_Total.AddRange(_vanishPuyosPosX);
                        _vanishPuyosPosY_Total.AddRange(_vanishPuyosPosY);
                        addPuyo.Add(_count);
                        _colors.Add(_fieldPuyoState[i, j]);
                        //ぷよを削除する
                        Array.Copy(_erasedPuyoState, _fieldPuyoState, _fieldPuyoState.Length);
                        _isVanish = true;
                    }
                }
            }
        }
        //スコアを加算する
        for (int i = 0; i < addPuyo.Count; i++)
        {
            Global.ScoreAdd(_chain, addPuyo[i], _colors.Count);
        }
        if (_isVanish)
        {
            Chain();
        }
    }

    //連鎖テキストを消去場所に表示させる
    void Chain()
    {
        for (int i = 0; i < _vanishPuyosPosX_Total.Count; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                GameObject.Instantiate<GameObject>(Particle, new Vector3Int(_vanishPuyosPosX_Total[i], _vanishPuyosPosY_Total[i], 0), Quaternion.identity);
            }
            _textChain.GetComponent<Chain>().CallVanished(_vanishPuyosPosX_Total[0], _vanishPuyosPosY_Total[0]);
        }
    }

    //落下可能フラグ
    bool _isFall()
    {
        for (int i = 0; i < FIELD_SIZE_X; i++)
        {
            for (int j = 1; j < FIELD_SIZE_Y; j++)
            {
                if (_fieldPuyoState[i, j] != ePuyoState.eNone && _fieldPuyoState[i, j - 1] == ePuyoState.eNone)
                {
                    return true;
                }
            }
        }
        return false;
    }

    //消去可能フラグ
    bool _isVanish;

    //連鎖数
    int _chain;
    //消したぷよの種類
    HashSet<ePuyoState> _colors;


    //連鎖処理
    private IEnumerator LoopPuyo()
    {
        //初期化
        _chain = 1;
        //処理開始
        audio.PlayOneShot(sFall);
        if (_isFall())
        {
            DropPuyo();
            yield return new WaitForSeconds(0.5f);
        }
        UpdatePuyoState();
        VanishPuyo();
        if (_isVanish)
        {
            audio.PlayOneShot(sVanish);
            yield return new WaitForSeconds(0.5f);
        }
        UpdatePuyoState();
        while (_isFall())
        {
            _chain++;
            yield return new WaitForSeconds(0.5f);
            DropPuyo();
            audio.PlayOneShot(sFall);
            UpdatePuyoState();
            VanishPuyo();
            if (_isVanish)
            {
                audio.PlayOneShot(sVanish);
                yield return new WaitForSeconds(0.5f);
            }
            UpdatePuyoState();
        }
        _state = eGameState.eMove;
        //次のぷよを開始する
        StartMove();
    }
    int ConnectPuyo(int x, int y, int count, ePuyoState searchObj)
    {
        count++;
        _erasedPuyoState[x, y] = ePuyoState.eNone;
        int[,] dirs = { { 0, 1 }, { 1, 0 }, { 0, -1 }, { -1, 0 } };
        for (int i = 0; i < dirs.GetLength(0); i++)
        {
            int nx = x + dirs[i, 0];
            int ny = y + dirs[i, 1];
            if (nx > 0 && nx < FIELD_SIZE_X && ny > 0 && ny < FIELD_SIZE_Y)
            {
                if (_erasedPuyoState[nx, ny] == searchObj)
                {
                    _vanishPuyosPosX.Add(nx);
                    _vanishPuyosPosY.Add(ny);
                    count = ConnectPuyo(nx, ny, count, searchObj);
                }
            }

        }
        return (count);
    }

    //ぷよのリストを作る
    void SetNextPuyo()
    {
        for (int i = 0; i < _nextPuyo.GetLength(0); i++)
        {
            for (int j = 0; j < _nextPuyo.GetLength(1); j++)
            {
                _nextPuyo[i, j] = (ePuyoState)UnityEngine.Random.Range(2, (int)ePuyoState.eMax);
                if (i < 2)
                {
                    Puyo newPuyo = _puyosPrefabNext[i * 2 + j].GetComponent<Puyo>();
                    _nextPuyosScript[i, j] = newPuyo;
                    _nextPuyosScript[i, j].SetState(_nextPuyo[i, j]);
                }
            }
        }
    }

    //ネクストぷよを更新する
    void UpdateNextPuyo()
    {
        //新しいプヨを追加
        for (int i = 0; i < _nextPuyo.GetLength(0); i++)
        {
            for (int j = 0; j < _nextPuyo.GetLength(1); j++)
            {
                if (i + 1 < _nextPuyo.GetLength(0))
                {
                    _nextPuyo[i, j] = _nextPuyo[i + 1, j];
                    _nextPuyosScript[i, j].SetState(_nextPuyo[i, j]);
                }
                else
                {
                    _nextPuyo[i, j] = (ePuyoState)UnityEngine.Random.Range(2, (int)ePuyoState.eMax);
                }
            }
        }
    }

    //ぷよを開始する
    void StartMove()
    {
        //初期状態の設定
        _fallTimer = _fallTime;
        _movePuyoX = DEFAULT_MOVE_X;
        _movePuyoY = DEFAULT_MOVE_Y;
        _epuyoAngle = ePuyoAngle.e0;

        // 状態を設定
        for (int i = 0; i < MOVE_SIZE_X; i++)
        {
            for (int j = 0; j < MOVE_SIZE_Y; j++)
            {
                //一つ目のぷよ
                if (i == 1 && j == 1)
                {
                    _movePuyoState[i, j] = _nextPuyo[0, 0];
                }
                //二つ目のぷよ
                else if (i == 1 && j == 2)
                {
                    _movePuyoState[i, j] = _nextPuyo[0, 1];
                }
                else
                {
                    _movePuyoState[i, j] = ePuyoState.eNone;
                }
            }
        }
        //nextぷよを更新する
        UpdateNextPuyo();
    }

}
