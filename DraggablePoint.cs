using Godot;
using System;

public partial class DraggablePoint : Node2D
{
    private static CompressedTexture2D _blue_texture = GD.Load<CompressedTexture2D>("res://blue_circle.png");
    private static CompressedTexture2D _orange_texture = GD.Load<CompressedTexture2D>("res://orange_circle.png");
    [Export] public Sprite2D PointSprite;
    [Export] public SelectionArea2D Area;
    public const float BASE_RADIUS = 128f;
    public Label Title;
    public bool CanDrag = true;
    public override void _Ready()
    {
        Area.SelectionToggled += Select;
        PointSprite.Modulate = new Color(1, 1, 1, 0.8f);
        PointSprite.Texture = _blue_texture;
        Title = GetNode<Label>("PointTitle");
    }
    public override void _Process(double delta)
    {
        if (CanDrag && Main.SelectedPoint == this)
        {
            var mouse_pos = GetViewport().GetMousePosition();
            GlobalPosition = Main.Instance.Camera.GlobalPosition + mouse_pos.Clamp(Vector2.Zero, Main.Instance.Camera.GetViewportRect().Size);

            if (Input.IsActionPressed("SnapToGrid"))
            {
                GlobalPosition = new Vector2(
                    Mathf.Round(GlobalPosition.X / Main.GRID_SIZE) * Main.GRID_SIZE,
                    Mathf.Round(GlobalPosition.Y / Main.GRID_SIZE) * Main.GRID_SIZE
                );
            }

            if (Main.Instance.TriangleMode == Main.TriangleModeEnum.UnitCircle)
            {
                var _centre_pos = Main.Instance.GetCentrePos();
                var _circle_radius = Main.Instance.GetCircleRadius();
                if ((Main.Instance.Points[1].GlobalPosition - _centre_pos).LengthSquared() != _circle_radius*_circle_radius)
                {
                    Main.Instance.Points[1].GlobalPosition = _centre_pos + (Main.Instance.Points[1].GlobalPosition - _centre_pos).Normalized()*_circle_radius;
                }
                if (Input.IsActionPressed("SnapToGrid"))
                {
                    var vec = (Main.Instance.Points[1].GlobalPosition - _centre_pos).Normalized();
                    var ang = Mathf.Atan2(vec.Y, vec.X);
                    var ang_rounded = Mathf.Round(ang / (Mathf.Pi / 12f)) * (Mathf.Pi / 12f);
                    Main.Instance.Points[1].GlobalPosition = _centre_pos + new Vector2(Mathf.Cos(ang_rounded), Mathf.Sin(ang_rounded)) * _circle_radius;
                }
            }
        }
        if (Main.SelectedPoint == this)
        {
            PointSprite.Texture = _orange_texture;
        }
        else
        {
            PointSprite.Texture = _blue_texture;
        }
    }

    public void Select(bool is_selected)
    {        
        if (is_selected)
        {
            Main.SelectedPoint = this;
            PointSprite.Texture = _orange_texture;
        }
        else PointSprite.Texture = _blue_texture;
    }
}
