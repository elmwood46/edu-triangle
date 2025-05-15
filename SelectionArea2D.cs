using Godot;
using System;

public partial class SelectionArea2D : Area2D
{
    [Signal]
    public delegate void SelectionToggledEventHandler(bool isSelected);
    public bool IsSelected {get; private set;} = false;
    private bool _mouse_is_hovering = false;
    public override void _Ready()
    {
        MouseEntered += () => _mouse_is_hovering = true;
        MouseExited += () => {
            _mouse_is_hovering = false;
            var t = new Timer()
            {
                WaitTime = 0.1f,
                OneShot = true,
            };
            t.Timeout += () =>
            {
                if (!_mouse_is_hovering && IsSelected) SetSelected(false);
                t.Stop();
                t.QueueFree();
            };
            AddChild(t);
            t.Start();
        };
    }

    public override void _Input(InputEvent @event)
    {
        // can't select if already have a selected piece
        if (Main.SelectedPoint != null) return;

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left && mouseButton.IsPressed())
            {
                if (_mouse_is_hovering) SetSelected(true);
                else SetSelected(false);
            }
        }
    }

    public void SetSelected(bool select_this_object)
    {
        if (select_this_object)
        {
            RemoveAllSelectedObjectsFromGroup();
            AddToGroup("selected_objects");
        }
        else 
        {
            RemoveFromGroup("selected_objects");
        }
        IsSelected = select_this_object;
        GD.Print("emitting signal ", SignalName.SelectionToggled + " " + select_this_object);
        EmitSignal(SignalName.SelectionToggled, select_this_object);
    }

    private void RemoveAllSelectedObjectsFromGroup()
    {
        if (!IsSelected) return;
        GetTree().CallGroup("selected_objects", "SetSelected", false);
    }
}
