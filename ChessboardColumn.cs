using Godot;

public partial class ChessboardColumn : Control
{
    [Export] public Button[] OrderedClickables;

    [Export] public TextureRect[] OrderedHighlighters;

    [Export] public PieceSlot[] PieceSlots;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}