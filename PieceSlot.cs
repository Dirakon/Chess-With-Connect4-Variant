using System;
using ChessWithConnect4;
using Godot;

public partial class PieceSlot : ColorRect
{
    private Control? _activatedIcon;
    [Export] private Control Pawn, Bishop, Queen, Knight, Rook, King, Checker;

    public void RenderChecker()
    {
        if (_activatedIcon != null)
            _activatedIcon.Visible = false;
        Checker.Visible = true;
        _activatedIcon = Checker;
    }

    public void RenderChessPiece(ChessPieceType? piece)
    {
        if (_activatedIcon != null)
            _activatedIcon.Visible = false;
        var newPiece = piece switch
        {
            ChessPieceType.Pawn => Pawn,
            ChessPieceType.Bishop => Bishop,
            ChessPieceType.Queen => Queen,
            ChessPieceType.Knight => Knight,
            ChessPieceType.Rook => Rook,
            ChessPieceType.King => King,
            null => null,
            _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, null)
        };
        if (newPiece != null)
            newPiece.Visible = true;

        _activatedIcon = newPiece;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}