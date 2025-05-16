using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ootpisp
{
    public struct PointD
    {
        public double X { get; set; }
        public double Y { get; set; }

        public PointD(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public struct RectangleD
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public RectangleD(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public double Left => X;
        public double Right => X + Width;
        public double Top => Y;
        public double Bottom => Y + Height;
    }

    public abstract class DisplayObject
    {
        protected PointD position;
        protected PointD velocity;
        protected PointD acceleration;
        protected Color fillColor;
        protected double size;
        protected double mass;
        protected double borderThickness;
        protected Pen borderPen;
        protected bool isAccelerating = false;
        private static readonly Random rand = new Random();
        protected bool isMoving = true;

        [JsonIgnore]
        public virtual RectangleD Bounds
        {
            get
            {
                return new RectangleD(
                    position.X - size,
                    position.Y - size,
                    size * 2,
                    size * 2
                );
            }
        }

        [JsonProperty("PositionX")]
        public double PositionX => position.X;
        [JsonProperty("PositionY")]
        public double PositionY => position.Y;
        [JsonProperty("VelocityX")]
        public double VelocityX => velocity.X;
        [JsonProperty("VelocityY")]
        public double VelocityY => velocity.Y;
        [JsonProperty("AccelerationX")]
        public double AccelerationX => acceleration.X;
        [JsonProperty("AccelerationY")]
        public double AccelerationY => acceleration.Y;
        [JsonProperty("ColorR")]
        public int ColorR => fillColor.R;
        [JsonProperty("ColorG")]
        public int ColorG => fillColor.G;
        [JsonProperty("ColorB")]
        public int ColorB => fillColor.B;
        [JsonProperty("BorderThickness")]
        public double BorderThickness => borderThickness;
        [JsonProperty("Size")]
        public double Size => size;
        [JsonProperty("IsMoving")]
        public bool IsMoving => isMoving;
        [JsonProperty("IsAccelerating")]
        public bool IsAccelerating => isAccelerating;
        [JsonProperty("Type")]
        public string Type => GetType().Name;
        [JsonProperty("Mass")]
        public double Mass => mass;

        public DisplayObject(double x, double y)
        {
            position = new PointD(x, y);
            // Случайная скорость от 1 до 5
            double speed = rand.NextDouble() * 7 + 1;
            // Случайный угол направления в радианах
            double angle = rand.NextDouble() * 2 * Math.PI;
            velocity = new PointD(
                speed * Math.Cos(angle),
                speed * Math.Sin(angle)
            );
            acceleration = new PointD(0, 0);
            // Случайный цвет
            fillColor = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
            // Случайная толщина рамки от 1 до 4
            borderThickness = rand.NextDouble() * 3 + 1;
            borderPen = new Pen(Color.Black, (float)borderThickness);
            size = rand.NextDouble() * 30 + 10;
            mass = 2;
        }

        [JsonConstructor]
        public DisplayObject(double positionX, double positionY, double velocityX, double velocityY,
        double accelerationX, double accelerationY, int colorR, int colorG, int colorB,
        double borderThickness, double size, bool isAccelerating, bool isMoving, double mass)
        {
            position = new PointD(positionX, positionY);
            velocity = new PointD(velocityX, velocityY);
            acceleration = new PointD(accelerationX, accelerationY);
            fillColor = Color.FromArgb(colorR, colorG, colorB);
            this.borderThickness = borderThickness;
            borderPen = new Pen(Color.Black, (float)borderThickness);
            this.size = size;
            this.isAccelerating = isAccelerating;
            this.isMoving = isMoving;
            this.mass = mass;
        }

        public void ToggleAcceleration()
        {
            double accelMagnitude = rand.NextDouble() * 0.5 - 0.1;
            double accelAngle = rand.NextDouble() * 2 * Math.PI;
            acceleration = new PointD(
                accelMagnitude * Math.Cos(accelAngle),
                accelMagnitude * Math.Sin(accelAngle)
            );
            isAccelerating = true;
        }

        public void DisableAcceleration()
        {
            acceleration = new PointD(0, 0);
            isAccelerating = false;
        }

        public virtual void Move(Rectangle bounds, DisplayObject[] others)
        {
            if (!isMoving) return;
            if (isAccelerating)
            {
                velocity.X += acceleration.X;
                velocity.Y += acceleration.Y;
            }
            position.X += velocity.X;
            position.Y += velocity.Y;

            // Отскок от стен
            RectangleD objBounds = Bounds;
            if (objBounds.Left < bounds.Left)
            {
                position.X = bounds.Left + size;
                velocity.X = -velocity.X;
            }
            if (objBounds.Right > bounds.Right)
            {
                position.X = bounds.Right - size;
                velocity.X = -velocity.X;
            }
            if (objBounds.Top < bounds.Top)
            {
                position.Y = bounds.Top + size;
                velocity.Y = -velocity.Y;
            }
            if (objBounds.Bottom > bounds.Bottom)
            {
                position.Y = bounds.Bottom - size;
                velocity.Y = -velocity.Y;
            }

            foreach (var other in others)
            {
                if (other == this) continue;

                double dx = position.X - other.position.X;
                double dy = position.Y - other.position.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                double minDistance = size + other.size;

                if (distance < minDistance && distance > 0) // Столкновение произошло
                {
                    double nx = dx / distance; // Нормализованный вектор направления
                    double ny = dy / distance;

                    double relativeVelocityX = velocity.X - other.velocity.X;
                    double relativeVelocityY = velocity.Y - other.velocity.Y;

                    double normalSpeed = relativeVelocityX * nx + relativeVelocityY * ny;
                    if (normalSpeed >= 0) return;

                    double impulse = (2 * normalSpeed) / (mass + other.mass);

                    velocity.X -= impulse * other.mass * nx;
                    velocity.Y -= impulse * other.mass * ny;
                    other.velocity.X += impulse * mass * nx;
                    other.velocity.Y += impulse * mass * ny;

                    // Сдвигаем, чтобы не залипли
                    double overlap = minDistance - distance;
                    double correction = overlap / 2;
                    position.X += nx * correction;
                    position.Y += ny * correction;
                    other.position.X -= nx * correction;
                    other.position.Y -= ny * correction;
                }
            }
        }

        public abstract void Draw(Graphics g);
    }

    public class Circle : DisplayObject
    {
        public Circle(double x, double y) : base(x, y) { }

        [JsonConstructor]
        public Circle(double positionX, double positionY, double velocityX, double velocityY,
        double accelerationX, double accelerationY, int colorR, int colorG, int colorB,
        double borderThickness, double size, bool isAccelerating, bool isMoving, double mass)
        : base(positionX, positionY, velocityX, velocityY, accelerationX, accelerationY,
               colorR, colorG, colorB, borderThickness, size, isAccelerating, isMoving, mass)
        { }

        public override void Draw(Graphics g)
        {
            g.FillEllipse(new SolidBrush(fillColor),
                (float)(position.X - size), (float)(position.Y - size), (float)(size * 2), (float)(size * 2));
            g.DrawEllipse(borderPen,
                (float)(position.X - size), (float)(position.Y - size), (float)(size * 2), (float)(size * 2));
        }
    }

    public class Triangle : DisplayObject
    {
        public Triangle(double x, double y) : base(x, y) { }
        [JsonConstructor]
        public Triangle(double positionX, double positionY, double velocityX, double velocityY,
        double accelerationX, double accelerationY, int colorR, int colorG, int colorB,
        double borderThickness, double size, bool isAccelerating, bool isMoving, double mass)
        : base(positionX, positionY, velocityX, velocityY, accelerationX, accelerationY,
               colorR, colorG, colorB, borderThickness, size, isAccelerating, isMoving, mass)
        { }
        public override void Draw(Graphics g)
        {
            PointF[] points = new PointF[]
            {
                new PointF((float)position.X, (float)(position.Y - size)),
                new PointF((float)(position.X - size), (float)(position.Y + size)),
                new PointF((float)(position.X + size), (float)(position.Y + size))
            };
            g.FillPolygon(new SolidBrush(fillColor), points);
            g.DrawPolygon(borderPen, points);
        }
    }

    public class Square : DisplayObject
    {
        public Square(double x, double y) : base(x, y) { }
        [JsonConstructor]
        public Square(double positionX, double positionY, double velocityX, double velocityY,
        double accelerationX, double accelerationY, int colorR, int colorG, int colorB,
        double borderThickness, double size, bool isAccelerating, bool isMoving, double mass)
        : base(positionX, positionY, velocityX, velocityY, accelerationX, accelerationY,
               colorR, colorG, colorB, borderThickness, size, isAccelerating, isMoving, mass)
        { }
        public override void Draw(Graphics g)
        {
            g.FillRectangle(new SolidBrush(fillColor),
                (float)(position.X - size), (float)(position.Y - size), (float)(size * 2), (float)(size * 2));
            g.DrawRectangle(borderPen,
                (float)(position.X - size), (float)(position.Y - size), (float)(size * 2), (float)(size * 2));
        }
    }

    public class Hexagon : DisplayObject
    {
        public Hexagon(double x, double y) : base(x, y) { }
        [JsonConstructor]
        public Hexagon(double positionX, double positionY, double velocityX, double velocityY,
        double accelerationX, double accelerationY, int colorR, int colorG, int colorB,
        double borderThickness, double size, bool isAccelerating, bool isMoving, double mass)
        : base(positionX, positionY, velocityX, velocityY, accelerationX, accelerationY,
               colorR, colorG, colorB, borderThickness, size, isAccelerating, isMoving, mass)
        { }
        public override void Draw(Graphics g)
        {
            PointF[] points = new PointF[6];
            for (int i = 0; i < 6; i++)
            {
                points[i] = new PointF(
                    (float)(position.X + size * Math.Cos(i * Math.PI / 3)),
                    (float)(position.Y + size * Math.Sin(i * Math.PI / 3))
                );
            }
            g.FillPolygon(new SolidBrush(fillColor), points);
            g.DrawPolygon(borderPen, points);
        }
    }
}