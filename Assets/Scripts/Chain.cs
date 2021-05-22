using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chain : MonoBehaviour
{
    public GameObject ChainObj;
    Text chainText;
    // Start is called before the first frame update
    void Start()
    {
        chainText = this.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void CallVanished(int x, int y)
    {
        ChainObj.transform.position = new Vector3Int(x, y, 0);
        StartCoroutine(DisplayChain());
    }

    private IEnumerator DisplayChain()
    {
        chainText.text = string.Format("{0}れんさ！", Global.Chain);
        yield return new WaitForSeconds(1f);
        chainText.text = "";
    }
}
