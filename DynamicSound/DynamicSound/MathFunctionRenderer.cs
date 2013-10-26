using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DynamicSound
{
    public delegate double MathFunctionDelegate(double time);

    public class MathFunctionRenderer
    {
        /// <summary>
        /// Creates a function renderer instance. Higher buffer size translates to more precision in the drawing.
        /// </summary>
        public MathFunctionRenderer(GraphicsDevice graphicsDevice, int bufferSize = 500)
        {
            _bufferSize = Math.Max(2, bufferSize);
            _samples = new float[this._bufferSize];
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        /// <summary>
        /// Width of the graph
        /// </summary>
        public float Width
        {
            set
            {
                if (_width != value)
                {
                    _width = value;
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// Height of the graph
        /// </summary>
        public float Height
        {
            set
            {
                if (_height != value)
                {
                    _height = value;
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// The range of the graph window. For instance, if range equals 4 then the graph will
        /// show values with X between -2 and 2.
        /// </summary>
        public double RangeX
        {
            set
            {
                if (_rangeX != value)
                {
                    _rangeX = value;
                    _dirty = true;
                }
            }
        }

        public double RangeY
        {
            set
            {
                if (_rangeY != value)
                {
                    _rangeY = value;
                    _dirty = true;
                }
            }
        }

        /// <summary>
        /// The function to draw
        /// </summary>
        public MathFunctionDelegate MathFunction
        {
            set
            {
                if (_mathFunction != value)
                {
                    _mathFunction = value;
                    _dirty = true;
                }
            }
        }

        public Color BackgroundColor
        {
            get { return _backgroundColor; }
            set { _backgroundColor = value; }
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set { _borderColor = value; }
        }

        /// <summary>
        /// Call this when you'd like the function to be sampled again
        /// </summary>
        public void Update()
        {
            _dirty = true;
        }

        /// <summary>
        /// Draw the graph at the chosen coordinates
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, float x, float y)
        {
            // If data has changed recalculate samples
            if (_dirty)
            {
                Recalculate();
                _dirty = false;
            }

            spriteBatch.Begin();

            // Draw background and border
            FillRectangle(spriteBatch, x - 2, y - 2, _width + 4, _height + 4, BorderColor);
            FillRectangle(spriteBatch, x, y, _width, _height, BackgroundColor);

            // Draw function
            float horizontalStep = _width / _bufferSize;
            for (int i = 0; i < _bufferSize - 1; i++)
            {
                DrawLine(spriteBatch, x + (i * horizontalStep), y + _samples[i], x + ((i + 1) * horizontalStep), y + _samples[i + 1], Color.Red, 2);
            }

            // Draw referential
            DrawLine(spriteBatch, x, y + (_height / 2), x + _width, y + (_height / 2), Color.Black, 1);
            DrawLine(spriteBatch, x + (_width / 2), y, x + (_width / 2), y + _height, Color.Black, 1);

            spriteBatch.End();
        }

        /// <summary>
        /// Recalculates height samples from function
        /// </summary>
        private void Recalculate()
        {
            // Only recalculate if a function has been assigned
            if (_mathFunction == null)
            {
                return;
            }

            // Calculate how much to advance each step
            double timeStep = 2.0 * _rangeX / _bufferSize;

            // Choose initial time so that the function is centered on the graph
            double time = -_rangeX;

            for (int i = 0; i < _bufferSize; i++)
            {
                // Get value at that point
                double value = _mathFunction(time);

                // Scale it by the Y range in order to bring it down to [-1..1]
                value /= _rangeY;

                // Transform from [-1..1] range to [0..1] range
                value = (value + 1.0f) / 2.0f;

                // Invert because the Y-axis points down when rendering
                value = 1.0 - value;

                // Translate from [0..1] range to height in pixels
                _samples[i] = (float)(value * _height);

                // Advance to next value
                time += timeStep;
            }
        }

        /// <summary>
        /// Draw a line between two points
        /// </summary>
        private void DrawLine(SpriteBatch spriteBatch, float x1, float y1, float x2, float y2, Color color, int thickness)
        {
            Vector2 direction = new Vector2(x2 - x1, y2 - y1);
            float rotation = (float)Math.Atan2(y2 - y1, x2 - x1);
            spriteBatch.Draw(_pixel, new Vector2(x1, y1), new Rectangle(1, 1, 1, thickness), color, rotation, new Vector2(0f, (float)thickness / 2), new Vector2(direction.Length(), 1f), SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Fill a rectangular region with the specified color
        /// </summary>
        private void FillRectangle(SpriteBatch spriteBatch, float x1, float y1, float width, float height, Color color)
        {
            spriteBatch.Draw(_pixel, new Rectangle((int)x1, (int)y1, (int)width, (int)height), color);
        }

        // Fields

        private float _width = 500;

        private float _height = 300;

        private double _rangeX = 1.0;

        private double _rangeY = 1.0;

        private MathFunctionDelegate _mathFunction;

        private readonly Texture2D _pixel;

        private readonly float[] _samples;

        private readonly int _bufferSize;

        private bool _dirty = true;
        private Color _borderColor = Color.Black;
        private Color _backgroundColor = Color.White;
    }
}

