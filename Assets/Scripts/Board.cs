using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MRuby;
using DG.Tweening;

[CustomMRubyClass]
public class BoardObject : MonoBehaviour
{
    public int ObjectID;

    public void MoveTo(int x, int y, float duration)
    {
        transform.DOLocalMove(new Vector3(x, y, 0), duration);
    }
}

[CustomMRubyClass]
public class Board : MonoBehaviour
{
    [DoNotToLua]
    public BoardObject[] Templates;

    Dictionary<int, BoardObject> objects = new Dictionary<int, BoardObject>();

    private void Awake()
    {
        foreach(var t in Templates)
        {
            t.gameObject.SetActive(false);
        }
    }

    public BoardObject Create(string templateName, int id)
    {
        var template = Templates.First(t => t.name == templateName);
        var obj = GameObject.Instantiate(template.gameObject);
        obj.transform.SetParent(transform, false);

        var bobj = obj.GetComponent<BoardObject>();
        bobj.ObjectID = id;
        objects.Add(id, bobj);

        obj.SetActive(true);

        return bobj;
    }

    public BoardObject Find(int id)
    {
        return objects[id];
    }

}
