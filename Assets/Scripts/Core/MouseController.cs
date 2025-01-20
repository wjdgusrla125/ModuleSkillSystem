using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public enum CursorType
{
    Default,
    BlueArrow
}

public class MouseController : MonoSingleton<MouseController>
{
    public delegate void ClickHandler(Vector2 mousePosition);
    
    [Serializable]
    private struct CursorData
    {
        public CursorType type;
        public Texture2D texture;
    }

    [SerializeField] private CursorData[] cursorDatas;

    public event ClickHandler onLeftClicked;
    public event ClickHandler onRightClicked;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            onLeftClicked?.Invoke(Input.mousePosition);
        }
        else if (Input.GetMouseButtonUp(1))
        {
            onRightClicked?.Invoke(Input.mousePosition);
        }
    }

    public void ChangeCursor(CursorType newType)
    {
        if (newType == CursorType.Default)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
        else
        {
            var cursorTexture = cursorDatas.First(x => x.type == newType).texture;
            Cursor.SetCursor(cursorTexture, Vector2.zero, CursorMode.Auto);
        }
    }
}
