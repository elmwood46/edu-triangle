using Godot;
using System;
using System.Linq;

public partial class Main : Node2D
{
    public enum TriangleModeEnum
    {
        UnitCircle,
        Free
    }

    public enum AngleModeEnum
    {
        Degrees,
        Radians
    }

    public const int GRID_SIZE = 10;

    public TriangleModeEnum TriangleMode = TriangleModeEnum.Free;
    public DraggablePoint[] Points {get;set;} = [];
    public Camera2D Camera {get;set;}
    public static Main Instance {get; private set;}
    public static DraggablePoint SelectedPoint;
    public DraggablePoint GetSelectedPoint () => SelectedPoint;
    private static Vector2 _selected_point_prev_positon = default;
    private static readonly Color FLAT_BLUE = new(0x00a2e8);
    private Node2D _gen_rbs = new();
    private Vector2 _centre_pos = new(0, 0);
    private float _circle_radius = 256f;
    public float GetCircleRadius() => _circle_radius;
    public Vector2 GetCentrePos() => _centre_pos;
    public AngleModeEnum AngleMode = AngleModeEnum.Degrees;
    float _arc_radius_scale = 0.2f;
    public Area2D SwapControlArea;

    public override void _Ready()
    {
        DisplayServer.WindowSetTitle("Triangle");
        var control = GetNode<Control>("%Control");
        var margin_container = control.GetNode<MarginContainer>("MarginContainer");
        var view_size = GetViewport().GetVisibleRect().Size;
        void mouse_enter_lambda()
        {
            GD.Print("mouse enter");
            if (SelectedPoint == null) return;
            control.Position = Vector2.Right * (view_size.X - margin_container.Size.X * control.Scale.X);
        }
        void mouse_exit_lambda()
        {
            //control.Position = Vector2.Zero;
        }
        margin_container.Connect("mouse_entered",Callable.From(mouse_enter_lambda));
        margin_container.Connect("mouse_exited",Callable.From(mouse_exit_lambda));

        AddChild(_gen_rbs);
        Instance = this;
        Points = [.. GetNode<Node2D>("DraggablePoints").GetChildren().OfType<DraggablePoint>()];
        Camera = GetNode<Camera2D>("Camera2D");
        Points[0].Title.Text = "A";
        Points[1].Title.Text = "B";
        Points[2].Title.Text = "C";
        _centre_pos = GetViewport().GetVisibleRect().Size/2;
        for (int i=0; i<Points.Length; i++)
        {
            Points[i].GlobalPosition = _centre_pos + new Vector2(
                Mathf.Cos(-Mathf.Pi/2+Mathf.Tau/Points.Length*i)*_circle_radius,
                Mathf.Sin(-Mathf.Pi/2+Mathf.Tau/Points.Length*i)*_circle_radius
            );
        }
    }

    public override void _Draw()
    {
        GetNode<Label>("%TriInfoLabel").Text = "Triangle:";
        var l1 = Points[0].GlobalPosition-Points[1].GlobalPosition;
        var l2 = Points[1].GlobalPosition-Points[2].GlobalPosition;
        var l3 = Points[2].GlobalPosition-Points[0].GlobalPosition;
        var l1_len = Mathf.RoundToInt(l1.LengthSquared());
        var l2_len = Mathf.RoundToInt(l2.LengthSquared());
        var l3_len = Mathf.RoundToInt(l3.LengthSquared());
        if (l1_len == l2_len && l2_len == l3_len) GetNode<Label>("%TriInfoLabel").Text += " Equilateral";
        else if (l1_len == l2_len || l2_len == l3_len || l3_len == l1_len) GetNode<Label>("%TriInfoLabel").Text += " Isosceles";
        else GetNode<Label>("%TriInfoLabel").Text += " Scalene";

        var dotl1_2 = Mathf.Abs(l1.Dot(l2));
        var dotl2_3 = Mathf.Abs(l2.Dot(l3));
        var dotl3_1 = Mathf.Abs(l3.Dot(l1));
        if  (dotl1_2 <= 0.001f || dotl2_3 <= 0.001f || dotl3_1 <= 0.001f)
        {
            GetNode<Label>("%TriInfoLabel").Text += ", Right";
        }

        // draw arcs
        for (int i=0; i<Points.Length; i++)
        {
            DraggablePoint P1, P2, P3; 
            P1 = Points[i];
            P2 = Points[(i+1)%Points.Length];
            P3 = Points[(i+2)%Points.Length];

            // draw arcs between 3 points
            if (TriangleMode == TriangleModeEnum.Free)
            {
                if (P1.GlobalPosition == P2.GlobalPosition || P2.GlobalPosition == P3.GlobalPosition || P1.GlobalPosition == P3.GlobalPosition)
                {
                    // triangle is degenerate
                    GetNode<Label>("%TriInfoLabel").Text = "Triangle: Degenerate (overlapping points)";
                    break;
                }

                var P2_1 = P1.GlobalPosition-P2.GlobalPosition;
                var P2_3 = P3.GlobalPosition-P2.GlobalPosition;
                var P2_1_N = P2_1.Normalized();
                var P2_3_N = P2_3.Normalized();
                var P_av = (P2_1_N+P2_3_N).Normalized();

                if (Mathf.Abs(P2_1_N.Dot(P2_3_N)) >= 1-Mathf.Epsilon)
                {
                    GetNode<Label>("%TriInfoLabel").Text = "Triangle: Degenerate (points in parallel)";
                    break;
                }

                var radius = DraggablePoint.BASE_RADIUS*_arc_radius_scale;
                var center = P2.GlobalPosition;
                var start_angle = Mathf.Atan2(P2_1_N.Y, P2_1_N.X);
                var end_angle =  Mathf.Atan2(P2_3_N.Y, P2_3_N.X);

                if (Math.Abs(end_angle - start_angle) > Mathf.Pi)
                {
                    if (end_angle > start_angle)end_angle -= Mathf.Tau;
                    else start_angle -= Mathf.Tau;
                }

                // if (SelectedPoint != null && SelectedPoint == P2 && _selected_point_prev_positon != SelectedPoint.GlobalPosition)
                // {
                //     _selected_point_prev_positon = SelectedPoint.GlobalPosition;
                //     //GD.Print("SelectedPoint: ", SelectedPoint.GlobalPosition);
                //     GD.Print("start_angle: ", start_angle);
                //     GD.Print("end_angle: ", end_angle);
                //     GD.Print("end_angle-start_angle: ", end_angle-start_angle);
                // }
                if (Mathf.Abs(P2_1_N.Dot(P2_3_N)) <= 0.001f)
                {
                    var pts = new Vector2[]
                    {
                        P2.GlobalPosition,
                        P2.GlobalPosition + P2_1_N*radius*1.4142135624f,
                        P2.GlobalPosition + P_av*radius*1.4142135624f*1.4142135624f,
                        P2.GlobalPosition + P2_3_N*radius*1.4142135624f,
                    };
                    DrawColoredPolygon(pts, FLAT_BLUE);
                }
                else DrawArc(center,radius,start_angle,end_angle,360,FLAT_BLUE,width:radius);

                // angle labels
                var prefix = P2 == Points[1] ? "ABC" : (P2 == Points[0] ? "CAB" : "BCA");
                var decstr = AngleMode == AngleModeEnum.Degrees ? "0." : "0.00";
                var angstr = Mathf.Abs(AngleMode == AngleModeEnum.Degrees ? Mathf.RadToDeg(end_angle-start_angle) : end_angle-start_angle).ToString(decstr);
                GetNode<Label>($"%Angle{prefix}").Text = $"∠{prefix}: " + angstr + (AngleMode == AngleModeEnum.Degrees ? "°" : " rad");

                // draw connecting lines
                DrawLine(P2.GlobalPosition, P1.GlobalPosition, Colors.Red, 2);
                DrawLine(P2.GlobalPosition, P3.GlobalPosition, Colors.Red, 2);
            }
            else if (TriangleMode == TriangleModeEnum.UnitCircle && P2 == Points[1])
            {
                var radius = DraggablePoint.BASE_RADIUS*P1.Scale.X;
                var vec = P2.GlobalPosition - _centre_pos;
                var vec_len = vec.Length();
                var vec_ang = Mathf.Atan2(vec.Y, vec.X);
                var viewsize = GetViewport().GetVisibleRect().Size;
                DrawCircle(_centre_pos, _circle_radius, Colors.Black, filled:false, width: 2);
                DrawLine(new Vector2(_centre_pos.X,0),new Vector2(_centre_pos.X,viewsize.Y), Colors.Black, 2);
                DrawLine(new Vector2(0,_centre_pos.Y),new Vector2(viewsize.X,_centre_pos.Y), Colors.Black, 2);

                var end_angle =  Mathf.Atan2(vec.Y, vec.X);
                if (end_angle > 0)end_angle -= Mathf.Tau;
                DrawArc(_centre_pos,radius,0,end_angle,360,Colors.Red,width:radius);

                DrawLine(_centre_pos, P2.GlobalPosition, Colors.Red, 2);

                Vector2 sinx, cosx;
                sinx = -Mathf.Sin(vec_ang)*vec_len*Vector2.Up;
                cosx = Mathf.Cos(vec_ang)*vec_len*Vector2.Right;
                DrawLine(_centre_pos, _centre_pos+cosx, Colors.Blue, 2);
                DrawLine(P2.GlobalPosition, _centre_pos+cosx, Colors.Green, 2);
                DrawCircle(P2.GlobalPosition, 8, Colors.Green);
                DrawCircle(_centre_pos+cosx, 8, Colors.Blue);

                var sinstr = Mathf.Sin(-vec_ang).ToString("0.00");
                var cosstr = Mathf.Cos(vec_ang).ToString("0.00");
                

                var decstr = AngleMode == AngleModeEnum.Degrees ? "0." : "0.00";
                
                // transform vector angle for STRING display
                var display_ang = vec_ang;
                if (display_ang <= 0) display_ang *= -1;
                else display_ang = Mathf.Pi+(Mathf.Pi-display_ang);
                
                var angval = AngleMode == AngleModeEnum.Degrees ? Mathf.RadToDeg(display_ang) : display_ang;

                var angstr = angval.ToString(decstr);
                angstr += AngleMode == AngleModeEnum.Degrees ? "°" : " rad";
                sinstr = $"sin({angstr}) = " + sinstr;
                cosstr = $"cos({angstr}) = " + cosstr;

                float padding = 32f;
                int fontsize = 26;
                var str_width = ThemeDB.FallbackFont.GetStringSize(angstr, fontSize:fontsize).X;
                var cos_str_width = ThemeDB.FallbackFont.GetStringSize(cosstr, fontSize:fontsize).X;
                var ang_str_offset =(Vector2.Right*(radius + str_width/2f)).Rotated(vec_ang);
                DrawString(ThemeDB.FallbackFont, _centre_pos+cosx+Vector2.Down*padding-Vector2.Right*cos_str_width, cosstr, modulate:Colors.Blue, fontSize:fontsize);
                DrawString(ThemeDB.FallbackFont, P2.GlobalPosition-sinx/2f+Vector2.Right*padding/2f, sinstr, modulate:Colors.LightGreen, fontSize:fontsize);
                DrawString(ThemeDB.FallbackFont, _centre_pos+ang_str_offset, angstr, modulate:Colors.Black, fontSize:fontsize);
            }
        }
    }

    public override void _Process(double delta)
    {



        // force pointsto be in the unit circle
        if (TriangleMode == TriangleModeEnum.UnitCircle)
        {
            if ((Points[1].GlobalPosition - _centre_pos).LengthSquared() != _circle_radius*_circle_radius)
            {
                Points[1].GlobalPosition = _centre_pos + (Points[1].GlobalPosition - _centre_pos).Normalized()*_circle_radius;
            }
        }

        QueueRedraw();

        if (SelectedPoint == null) return;
        //GD.Print("SelectedPoint: ", SelectedPoint.GlobalPosition);
        if (!Input.IsMouseButtonPressed(MouseButton.Left))
        {
            SelectedPoint.Area.SetSelected(false);
            SelectedPoint = null;
        }
        
    }

    public void Reset()
    {
        if (SelectedPoint != null)
        {
            SelectedPoint.Area.SetSelected(false);
            SelectedPoint = null;
        }
        GetTree().ReloadCurrentScene();
    }

    public void CreateCollisionShape()
    {
        var p_collide = new CollisionPolygon2D();
        var p_shape = new Polygon2D();
        Vector2[] points;

        if (TriangleMode == TriangleModeEnum.UnitCircle)
        {
            var vec = Points[1].GlobalPosition - _centre_pos;
            var vec_len = vec.Length();
            var vec_ang = Mathf.Atan2(vec.Y, vec.X);
            points =
            [
                _centre_pos,
                //_centre_pos + new Vector2(0, Mathf.Sin(vec_ang)) * vec_len,
                Points[1].GlobalPosition,
                _centre_pos + new Vector2(Mathf.Cos(vec_ang), 0) * vec_len,
            ];
        }
        else points = [.. Points.Select(p => p.GlobalPosition)];

        p_shape.Polygon = points;
        p_shape.Color = new Color(Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.NextSingle(), Random.Shared.Next(85,90)*.01f);
        var p_center = GetPolygonCenter(p_shape);
        var model_space_polygon = points.Select(p => p - p_center).ToArray();
        p_collide.Polygon = model_space_polygon;
        p_shape.Polygon = model_space_polygon;
        var rb = new RigidBody2D
        {
            PhysicsMaterialOverride = new PhysicsMaterial()
            {
                Friction = 0.1f,
                Bounce = 0.1f,
            }
        };
        rb.AddChild(p_collide);
        rb.AddChild(p_shape);
        _gen_rbs.AddChild(rb);
        rb.GlobalPosition = p_center;

    }

    private static Vector2 GetPolygonCenter(Polygon2D poly)
    {
        var w = poly.Polygon.Length;
        var ret = new Vector2(0, 0);
        for (int i = 0; i < w; i++)
        {
            ret.X += poly.Polygon[i].X/w;
            ret.Y += poly.Polygon[i].Y/w;
        }
        return ret;
    }

    public void SetTriangleMode(bool is_unit_circle)
    {
        if (is_unit_circle)
        {
            Points[0].CanDrag = false;
            Points[0].Visible = false;
            Points[2].CanDrag = false;
            Points[2].Visible = false;
            Points[1].Title.Visible = false;

            // reset angle labels
            for (int i=0; i<Points.Length; i++)
            {
                var P2 = Points[(i+1)%Points.Length];
                // reset angle labels
                var prefix = P2 == Points[1] ? "ABC" : (P2 == Points[0] ? "CAB" : "BCA");
                GetNode<Label>($"%Angle{prefix}").Text = $"∠{prefix}: " + "N/A";
            }

            Points[1].GlobalPosition = _centre_pos + new Vector2(
                Mathf.Cos(Mathf.Pi/4)*_circle_radius,
                Mathf.Sin(-Mathf.Pi/4)*_circle_radius
            );
        }
        else
        {
            Points[1].Title.Visible = true;
            for (int i=0; i<Points.Length; i++)
            {
                Points[i].CanDrag = true;
                Points[i].Visible = true;
                Points[i].GlobalPosition = _centre_pos + new Vector2(
                    Mathf.Cos(-Mathf.Pi/2+Mathf.Tau/Points.Length*i)*_circle_radius,
                    Mathf.Sin(-Mathf.Pi/2+Mathf.Tau/Points.Length*i)*_circle_radius
                );
            }
        }
        TriangleMode = is_unit_circle ? TriangleModeEnum.UnitCircle : TriangleModeEnum.Free;
    }

    public void SetAngleMode(int dropdown_index)
    {
        if (dropdown_index == 0) AngleMode = AngleModeEnum.Degrees;
        else AngleMode = AngleModeEnum.Radians;
    }
}
