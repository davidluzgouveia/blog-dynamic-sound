using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DynamicSound
{
    using System;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Audio;

    public class DynamicSound : Game
    {
        // Can be either 1 for Mono or 2 for Stereo
        public const int Channels = 2;

        // Between 8000 Hz and 48000 Hz
        public const int SampleRate = 44100;

        // Tweak for latency
        public const int SamplesPerBuffer = 1024;
        
        public DynamicSound()
        {
            GraphicsDeviceManager graphics = new GraphicsDeviceManager(this) { PreferredBackBufferWidth = 800, PreferredBackBufferHeight = 600 };
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // Create DynamicSoundEffectInstance object and start it
            _instance = new DynamicSoundEffectInstance(SampleRate, Channels == 2 ? AudioChannels.Stereo : AudioChannels.Mono);
            _instance.Play();

            // Create buffers
            const int bytesPerSample = 2;
            _xnaBuffer = new byte[Channels * SamplesPerBuffer * bytesPerSample];
            _workingBuffer = new float[Channels, SamplesPerBuffer];

            // Create our wave generators (oscillators)
            _leftOscillator = new Oscillator {Frequency = NoteToFrequency(_noteLeft)};
            _rightOscillator = new Oscillator {Frequency = NoteToFrequency(_noteRight)};

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Load graphic stuff
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("Arial");
            _leftRenderer = new MathFunctionRenderer(GraphicsDevice, 1500) { Width = 380, Height = 200, MathFunction = _leftOscillator.MathFunction, RangeX = 2.0 / 440.0, RangeY = 1.0 };
            _rightRenderer = new MathFunctionRenderer(GraphicsDevice, 1500) { Width = 380, Height = 200, MathFunction = _rightOscillator.MathFunction, RangeX = 2.0 / 440.0, RangeY = 1.0 };
            
            base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            // Handle and react to keyboard input
            HandleInput();

            // Make sure there is always one buffer in reserve
            while(_instance.PendingBufferCount < 3)
            {
                SubmitBuffer();
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Generates and submits exactly one buffer of audio samples
        /// </summary>
        private void SubmitBuffer()
        {
            FillWorkingBuffer();
            ConvertBuffer(_workingBuffer, _xnaBuffer);
            _instance.SubmitBuffer(_xnaBuffer);
        }


        /// <summary>
        /// Fills the working buffer with values from the oscillators
        /// </summary>
        private void FillWorkingBuffer()
        {
            for (int i = 0; i < SamplesPerBuffer; i++)
            {
                // Here is where you sample your wave function
                _workingBuffer[0, i] = (float) _leftOscillator.MathFunction(_time);
                _workingBuffer[1, i] = (float) _rightOscillator.MathFunction(_time);

                // Advance time passed since beggining
                // Since the amount of samples in a second equals the chosen SampleRate
                // Then each sample should advance the time by 1 / SampleRate
                _time += 1.0 / SampleRate;
            }
        }

        /// <summary>
        /// Converts a bi-dimensional (Channel, Samples) floating point buffer to a PCM (byte) buffer with interleaved channels
        /// </summary>
        private static void ConvertBuffer(float[,] from, byte[] to)
        {
            const int bytesPerSample = 2;
            int channels = from.GetLength(0);
            int bufferSize = from.GetLength(1);

            // Make sure the buffer sizes are correct
            System.Diagnostics.Debug.Assert(to.Length == bufferSize * channels * bytesPerSample, "Buffer sizes are mismatched.");

            for (int i = 0; i < bufferSize; i++)
            {
                for (int c = 0; c < channels; c++)
                {
                    // First clamp the value to the [-1.0..1.0] range
                    float floatSample = MathHelper.Clamp(from[c, i], -1.0f, 1.0f);
	
                    // Convert it to the 16 bit [short.MinValue..short.MaxValue] range
                    short shortSample = (short) (floatSample >= 0.0f ? floatSample * short.MaxValue : floatSample * short.MinValue * -1);

                    // Calculate the right index based on the PCM format of interleaved samples per channel [L-R-L-R]
                    int index = i * channels * bytesPerSample + c * bytesPerSample;

                    // Store the 16 bit sample as two consecutive 8 bit values in the buffer with regard to endian-ness
                    if (!BitConverter.IsLittleEndian)
                    {
                        to[index] = (byte)(shortSample >> 8);
                        to[index + 1] = (byte)shortSample;
                    }
                    else
                    {
                        to[index] = (byte)shortSample;
                        to[index + 1] = (byte)(shortSample >> 8);
                    }
                }
            }
        }

        private void HandleInput()
        {
            _currentKeyboardState = Keyboard.GetState();

            bool alt = _currentKeyboardState.IsKeyDown(Keys.LeftAlt);
            bool ctrl = _currentKeyboardState.IsKeyDown(Keys.LeftControl);

            // Volume Up
            if (_currentKeyboardState.IsKeyDown(Keys.Up))
            {
                if (ctrl || !alt)
                {
                    _leftOscillator.Amplitude = Math.Min(1.0, _leftOscillator.Amplitude + 0.05);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _rightOscillator.Amplitude = Math.Min(1.0, _rightOscillator.Amplitude + 0.05);
                    _rightRenderer.Update();                    
                }
            }

            // Volume Down
            if (_currentKeyboardState.IsKeyDown(Keys.Down))
            {
                if (ctrl || !alt)
                {
                    _leftOscillator.Amplitude = Math.Max(0.0, _leftOscillator.Amplitude - 0.05);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _rightOscillator.Amplitude = Math.Max(0.0, _rightOscillator.Amplitude - 0.05);
                    _rightRenderer.Update();
                }
            }


            // Frequency Up
            if (_currentKeyboardState.IsKeyDown(Keys.Right) && _previousKeyboardState.IsKeyUp(Keys.Right))
            {
                if (ctrl || !alt)
                {
                    _noteLeft = Math.Min(12, _noteLeft + 1);
                    _leftOscillator.Frequency = NoteToFrequency(_noteLeft);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _noteRight = Math.Min(12, _noteRight + 1);
                    _rightOscillator.Frequency = NoteToFrequency(_noteRight);
                    _rightRenderer.Update();
                }
            }

            // Frequency Down
            if (_currentKeyboardState.IsKeyDown(Keys.Left) && _previousKeyboardState.IsKeyUp(Keys.Left))
            {
                if(ctrl || !alt)
                {
                    _noteLeft = Math.Max(-12, _noteLeft - 1);
                    _leftOscillator.Frequency = NoteToFrequency(_noteLeft);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _noteRight = Math.Max(-12, _noteRight - 1);
                    _rightOscillator.Frequency = NoteToFrequency(_noteRight);
                    _rightRenderer.Update();
                }
            }

            // Next type
            if (_currentKeyboardState.IsKeyDown(Keys.Enter) && _previousKeyboardState.IsKeyUp(Keys.Enter))
            {
                if (ctrl || !alt)
                {
                    _leftOscillator.NextType();
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _rightOscillator.NextType();
                    _rightRenderer.Update();
                }

            }

            // Start/Pause sound
            if(_currentKeyboardState.IsKeyDown(Keys.Space) && _previousKeyboardState.IsKeyUp(Keys.Space))
            {
                if (_instance.State == SoundState.Playing)
                {
                    _instance.Pause();
                }
                else
                {
                    _instance.Play();
                }
            }

            // Duty Up
            if (_currentKeyboardState.IsKeyDown(Keys.PageUp))
            {
                if (ctrl || !alt)
                {
                    _leftOscillator.DutyCycle = Math.Min(1.0, _leftOscillator.DutyCycle + 0.01);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _rightOscillator.DutyCycle = Math.Min(1.0, _rightOscillator.DutyCycle + 0.01);
                    _rightRenderer.Update();
                }
            }

            // Duty Down
            if (_currentKeyboardState.IsKeyDown(Keys.PageDown))
            {
                if (ctrl || !alt)
                {
                    _leftOscillator.DutyCycle = Math.Max(0.0, _leftOscillator.DutyCycle - 0.01);
                    _leftRenderer.Update();
                }

                if (alt || !ctrl)
                {
                    _rightOscillator.DutyCycle = Math.Max(0.0, _rightOscillator.DutyCycle - 0.01);
                    _rightRenderer.Update();
                }
            }

            // Update highlights in renderers
            if ((alt && !ctrl) || (ctrl && !alt))
            {
                if(alt)
                {
                    _leftRenderer.BackgroundColor = Color.Gray;
                }
                else if(ctrl)
                {
                    _rightRenderer.BackgroundColor = Color.Gray;
                }
            }
            else
            {
                _rightRenderer.BackgroundColor = Color.White;
                _leftRenderer.BackgroundColor = Color.White;
            }

            _previousKeyboardState = _currentKeyboardState;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            GraphicsDevice.Clear(Color.CornflowerBlue);
            _leftRenderer.Draw(_spriteBatch, 10, 100);
            _rightRenderer.Draw(_spriteBatch, 410, 100);

            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, "Creating a Basic Synth in XNA 4.0 - Part II (Sample)\n(AKA how to make annoying sounds with maths and programming)", new Vector2(10, 10), Color.GreenYellow);
            _spriteBatch.DrawString(_font, "Left Channel", new Vector2(10, 70), Color.Yellow);
            _spriteBatch.DrawString(_font, "Right Channel", new Vector2(410, 70), Color.Yellow);
            _spriteBatch.DrawString(_font, "Amplitude: " + _leftOscillator.Amplitude.ToString("0.0") + "\nFrequency: " + _leftOscillator.Frequency.ToString("0.0") + "Hz\n" +(_leftOscillator.Type == Oscillator.WaveType.Pulse ? "Duty Cycle: " + (int)(_leftOscillator.DutyCycle * 100) + "%\n" : "" ) + "Wave Type: " + _leftOscillator.Type, new Vector2(10, 310), Color.White);
            _spriteBatch.DrawString(_font, "Amplitude: " + _rightOscillator.Amplitude.ToString("0.0") + "\nFrequency: " + _rightOscillator.Frequency.ToString("0.0") + "Hz\n" + (_rightOscillator.Type == Oscillator.WaveType.Pulse ? "Duty Cycle: " + (int)(_rightOscillator.DutyCycle * 100) + "%\n" : "") + "Wave Type: " + _rightOscillator.Type, new Vector2(410, 310), Color.White);
            _spriteBatch.DrawString(_font, "Controls:\n+ Ctrl / Alt (Hold): Left Only / Right Only\n+ Up / Down: Change amplitude\n+ Left / Right: Change frequency\n+ Page Up / Page Down: Change Duty Cycle (Pulse Only)\n+ Enter: Change wave type\n+ Space: Play / Pause", new Vector2(10, 415), Color.GreenYellow);
            _spriteBatch.DrawString(_font, "Source at http://www.david-gouveia.com", new Vector2(420, 565), Color.Blue);
            _spriteBatch.End();
        }

        private static double NoteToFrequency(int note)
        {
           return 440.0 * Math.Pow(2, note / 12.0f);
        }

        private SpriteFont _font;

        private byte[] _xnaBuffer;
        private float[,] _workingBuffer;

        private DynamicSoundEffectInstance _instance;
        private double _time;

        private double _volumeLeft = 0.5;
        private double _volumeRight = 0.5;
        private int _noteLeft = 0;
        private int _noteRight = 0;

        private bool _linked = true;

        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;

        private Oscillator _leftOscillator;
        private Oscillator _rightOscillator;

        private MathFunctionRenderer _leftRenderer;
        private MathFunctionRenderer _rightRenderer;

        private SpriteBatch _spriteBatch;
    }
}
