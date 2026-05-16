using System.Collections.Generic;
using UnityEngine;

public sealed class PuzzlePieceGroup
{
    private readonly List<PuzzlePieceView> members = new();

    public IReadOnlyList<PuzzlePieceView> Members => members;

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
