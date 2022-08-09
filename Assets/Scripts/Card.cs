using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MRuby;
using UnityEngine.EventSystems;
using System.Linq;
using DG.Tweening;

[CustomMRubyClass]
public class Card : BoardObject, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
{
    public Text NameText;
    public Button Button;
    public Image Image;
    //public Image CardImage;

    public void Redraw(string name, bool selected, bool reversed)
    {
        NameText.text = name;
        Image.color = selected ? new Color(1.0f, 0.7f, 0.7f) : Color.white;

        if (reversed)
        {
            Image.sprite = MainScene.Instance.CardAtlas.GetSprite("card_list_2d_54");
        }
        else
        {
            Image.sprite = GetCardSprite(name);
        }
    }

    public void OnClick()
    {
        var selectable = MainScene.Instance.Game.x("selectable?", ObjectID).AsBool();
        if (selectable)
        {
            MainScene.Instance.Play(new Command("select") { Card = ObjectID });
        }
    }

    public Sprite GetCardSprite(string name)
    {
        var atlas = MainScene.Instance.CardAtlas;
        //spade,dia,heard,club
        //j = 52,53
        //— =54~
        string spriteName;
        if (name != "J") {
            var suit = name[0];
            var num = name.Substring(1);
            int numNum = 0;
            int suitNum = 0;
            switch (num)
            {
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                case "7":
                case "8":
                case "9":
                case "10":
                    numNum = int.Parse(num);
                    break;
                case "J":
                    numNum = 11;
                    break;
                case "Q":
                    numNum = 12;
                    break;
                case "K":
                    numNum = 13;
                    break;
                default:
                    throw new System.Exception();
            }
            switch (suit)
            {
                case 'S':
                    suitNum = 0;
                    break;
                case 'D':
                    suitNum = 1;
                    break;
                case 'H':
                    suitNum = 2;
                    break;
                case 'C':
                    suitNum = 3;
                    break;
                default:
                    throw new System.Exception();
            }
            spriteName = $"card_list_2d_{suitNum * 13 + numNum - 1}";
        }
        else
        {
            spriteName = $"card_list_2d_54";
        }

        return atlas.GetSprite(spriteName);
    }

    private Vector2 screenToLocal(Vector2 screenPosition)
    {
        var rt = (RectTransform)MainScene.Instance.transform;
        //var rt = (RectTransform)this.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPosition, null, out var pos);
        return pos;
    }

    bool dragging;
    Vector3 startLocalPosition;
    Vector2 startPos;
    bool dragTemporaryDisabled;
    bool startClick;

    public void OnBeginDrag(PointerEventData data)
    {
        if( dragTemporaryDisabled || !startClick)
        {
            return;
        }
        var pos = screenToLocal(data.position);
        //Debug.Log($"Begin {pos}");
        dragging = true;
        startClick = false;
    }

    public void OnDrag(PointerEventData data)
    {
        if( !dragging)
        {
            return;
        }
        var pos = screenToLocal(data.position);
        //Debug.Log($"Drag {pos} {data.enterEventCamera}");
        var v = pos - startPos;
        this.transform.localPosition = startLocalPosition + new Vector3(v.x, v.y, 0);
    }

    public void OnEndDrag(PointerEventData data)
    {
        if (!dragging)
        {
            return;
        }
        var pos = screenToLocal(data.position);
        //Debug.Log($"End {pos}");
        dragging = false;

        var ray = RectTransformUtility.ScreenPointToRay(null, data.position);
        var canvas = MainScene.Instance.Canvas;
        List<RaycastResult> list = new();
        EventSystem.current.RaycastAll(data, list);
        var found = list.FirstOrDefault(r => r.gameObject != gameObject && r.gameObject.GetComponent<BoardObject>() != null);
        if (found.isValid)
        {
            Debug.Log($"Drag on {found.gameObject}");
            var targetObj = found.gameObject.GetComponent<BoardObject>();
            if (targetObj != null)
            {

                var movable = MainScene.Instance.Game.x("movable?", ObjectID, targetObj.ObjectID).AsBool();
                if (movable)
                {
                    MainScene.Instance.Play(new Command("move") { Card = ObjectID, MoveTo = targetObj.ObjectID });
                    return;
                }
            }
        }

        dragTemporaryDisabled = true;
        transform.DOLocalMove(startLocalPosition, 0.3f).AsyncWaitForCompletion().ContinueWith(n=>
        {
            Debug.Log("OK");
            dragTemporaryDisabled = false;
        });
    }

    public void OnPointerDown(PointerEventData data)
    {
        if (dragTemporaryDisabled)
        {
            return;
        }
        var pos = screenToLocal(data.position);
        startClick = true;
        startLocalPosition = transform.localPosition;
        startPos = pos;
    }
}
