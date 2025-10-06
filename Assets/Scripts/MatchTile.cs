using UnityEngine;

public class MatchTile : MonoBehaviour
{
    public enum TileType { Normal, Clown }

    public int x, y;
    public HexBoard.ColorType color;
    public TileType type;
    private HexBoard board;
    private SpriteRenderer sr;

    private Vector3 startPos;
    private bool isDragging = false;

    public void Init(int x, int y, HexBoard.ColorType color, HexBoard board, TileType type)
    {
        this.x = x;
        this.y = y;
        this.board = board;
        this.color = color;
        this.type = type; // Set type explicitly from HexBoard
        sr = GetComponent<SpriteRenderer>();

        if (sr != null ) 
        //sr.sortingOrder = 1;

        if (type == TileType.Normal)
        {
            SetColor(color);
        }
    }

    public void SetColor(HexBoard.ColorType newColor)
    {
        this.color = newColor;
        if (sr == null) return;

        if (type == TileType.Normal)
        {
            sr.sprite = board.GetSprite(newColor);
        }
    }

    void OnMouseDown()
    {
        if (type == TileType.Clown) return;

        isDragging = true;
        startPos = transform.position;
    }

    void OnMouseDrag()
    {
        if (!isDragging) return;
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 currentPos = board.GetTilePos(x, y);
        currentPos.z = 0;
        Vector3 delta = mousePos - startPos;
        transform.position = currentPos + delta;
    }

    void OnMouseUp()
    {
        if (!isDragging) return;
        isDragging = false;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        Vector3 dragVector = mousePos - startPos;

        if (dragVector.magnitude < 0.5f)
        {
            transform.position = board.GetTilePos(x, y);
            return;
        }

        board.AttemptSwap(this, dragVector);
    }

    public System.Collections.IEnumerator MoveToPosition(Vector3 targetPos, float duration)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;

        while (timer < duration)
        {
            transform.position = Vector3.Lerp(startPos, targetPos, timer / duration);
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPos;
    }

    public void ResetPosition()
    {
        transform.position = board.GetTilePos(x, y);
    }
}