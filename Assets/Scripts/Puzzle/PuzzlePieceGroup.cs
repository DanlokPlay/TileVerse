using System.Collections.Generic;
using UnityEngine;

public sealed class PuzzlePieceGroup
{
    private readonly List<PuzzlePieceView> members = new();

    public IReadOnlyList<PuzzlePieceView> Members => members;
    public bool IsPlaced { get; private set; }

    public void SetPlaced(bool placed) => IsPlaced = placed;

    public void SetHighlighted(bool highlighted)
    {
        foreach (var member in members) member.SetHighlighted(highlighted);
    }

    public PuzzlePieceGroup(PuzzlePieceView firstPiece)
    {
        AddMember(firstPiece);
    }

    public void BringToFront()
    {
        foreach (var member in members)
        {
            member.RectTransform.SetAsLastSibling();
        }
    }

    public void MoveBy(Vector2 delta)
    {
        foreach (var member in members)
        {
            member.RectTransform.anchoredPosition += delta;
        }
    }

    public void Absorb(PuzzlePieceGroup otherGroup)
    {
        IsPlaced |= otherGroup.IsPlaced;
        foreach (var member in otherGroup.members)
        {
            AddMember(member);
        }

        otherGroup.members.Clear();
    }

    private void AddMember(PuzzlePieceView piece)
    {
        if (members.Contains(piece))
        {
            return;
        }

        members.Add(piece);
        piece.AssignGroup(this);
    }
}
